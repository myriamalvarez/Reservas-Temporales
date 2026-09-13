using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using Reservas_Temporales.Models;
using Reservas_Temporales.Repositorios;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Reservas_Temporales.Controllers
{
    public class UsuarioController : ControladorBase
    {
        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long TamanioMaximoBytes = 2 * 1024 * 1024; // 2 MB, alcanza para un avatar
        private const int IteracionesHash = 100_000;
        private const int LargoHashBytes = 32; // 256 bits

        private readonly IRepositorioUsuario _repositorioUsuario;
        private readonly IWebHostEnvironment _entorno;
        private readonly IConfiguration _configuration;

        public UsuarioController(
            IRepositorioUsuario repositorioUsuario, IWebHostEnvironment entorno, IConfiguration configuration)
        {
            _repositorioUsuario = repositorioUsuario;
            _entorno = entorno;
            _configuration = configuration;
        }

        // ------------------------------------------------------------------
        // Login / Logout
        // ------------------------------------------------------------------

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            var usuario = await _repositorioUsuario.ObtenerPorEmailAsync(email);
            if (usuario == null || !usuario.Activo || !VerificarPassword(password, usuario.Password))
            {
                ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
                return View();
            }

            var token = GenerarToken(usuario);

            // El JWT va en una cookie HttpOnly (no accesible desde JS) para que el navegador
            // lo mande solo en cada request; el middleware de JwtBearer lo lee de ahí (ver Program.cs).
            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.Now.AddHours(8)
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token");
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult AccesoDenegado() => View();

        // ------------------------------------------------------------------
        // ABM de usuarios
        // ------------------------------------------------------------------

        // Solo los administradores gestionan la lista completa de usuarios.
        [Authorize(Policy = "Administrador")]
        public IActionResult Index() => View();

        [Authorize(Policy = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> ListarJson(int pagina = 1, int tamanioPagina = 10)
        {
            var (items, total) = await _repositorioUsuario.ListarPaginadoAsync(pagina, tamanioPagina);
            return Json(new
            {
                items = items.Select(u => new
                {
                    u.Id,
                    u.NombreUsuario,
                    u.Nombre,
                    u.Apellido,
                    u.Email,
                    Rol = u.Rol.ToString()
                }),
                total,
                pagina,
                tamanioPagina,
                totalPaginas = (int)Math.Ceiling(total / (double)tamanioPagina)
            });
        }

        // Atajo para que cualquier usuario logueado vea/edite su propio perfil sin conocer su Id.
        [Authorize]
        public IActionResult Perfil() => RedirectToAction(nameof(Details), new { id = UsuarioActualId });

        // Un empleado puede ver su propio perfil; un administrador puede ver cualquiera.
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            if (!EsAdministrador && id != UsuarioActualId) return Forbid();

            var usuario = await _repositorioUsuario.ObtenerPorIdAsync(id);
            if (usuario == null) return NotFound();

            ViewBag.UsuarioActualId = UsuarioActualId;
            return View(usuario);
        }

        [Authorize(Policy = "Administrador")]
        public IActionResult Create() => View(new Usuario());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Create(Usuario usuario, string password)
        {
            // Password no viaja como campo del modelo (se recibe aparte y se hashea),
            // así que se excluye de la validación automática del [Required] del modelo.
            ModelState.Remove(nameof(Usuario.Password));
            if (!ModelState.IsValid) return View(usuario);

            usuario.Password = HashearPassword(password);

            try
            {
                var id = await _repositorioUsuario.CrearAsync(usuario);
                TempData["Mensaje"] = "Usuario creado correctamente.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // La tabla tiene UNIQUE tanto en email como en nombre_usuario; el mensaje del
                // motor indica cuál de los dos chocó (viene en ex.Message, ej: "for key 'uq_usuario_email'").
                var campo = ex.Message.Contains("email", StringComparison.OrdinalIgnoreCase)
                    ? nameof(Usuario.Email)
                    : nameof(Usuario.NombreUsuario);
                ModelState.AddModelError(campo, "Ya existe un usuario con ese email o nombre de usuario.");
                return View(usuario);
            }
        }

        // Un empleado solo puede editar su propio perfil; un administrador puede editar cualquiera.
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            if (!EsAdministrador && id != UsuarioActualId) return Forbid();

            var usuario = await _repositorioUsuario.ObtenerPorIdAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, Usuario usuario)
        {
            if (!EsAdministrador && id != UsuarioActualId) return Forbid();
            if (id != usuario.Id) return BadRequest();

            // El formulario de edición no incluye Password ni Avatar (se cambian aparte),
            // así que se excluyen del [Required] del modelo para no invalidar el POST.
            ModelState.Remove(nameof(Usuario.Password));
            if (!ModelState.IsValid) return View(usuario);

            // Un empleado no puede cambiarse el rol ni desactivarse a sí mismo desde su propio perfil.
            // Tampoco se toca el avatar acá: se maneja con SubirAvatar/EliminarAvatar.
            var actual = await _repositorioUsuario.ObtenerPorIdAsync(id);
            if (actual == null) return NotFound();
            usuario.Avatar = actual.Avatar;
            if (!EsAdministrador)
            {
                usuario.Rol = actual.Rol;
                usuario.Activo = actual.Activo;
            }

            try
            {
                await _repositorioUsuario.ActualizarAsync(usuario);
                TempData["Mensaje"] = "Perfil actualizado correctamente.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                var campo = ex.Message.Contains("email", StringComparison.OrdinalIgnoreCase)
                    ? nameof(Usuario.Email)
                    : nameof(Usuario.NombreUsuario);
                ModelState.AddModelError(campo, "Ya existe otro usuario con ese email o nombre de usuario.");
                return View(usuario);
            }
        }

        // Cambio de contraseña: parte de "manipular su propio perfil" para los empleados.
        // Si el usuario cambia SU PROPIA clave, debe confirmar la actual. Si un administrador
        // resetea la clave de otro usuario, no se le exige (no tiene forma de conocerla).
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CambiarPassword(
            int id, string? passwordActual, string nuevaPassword, string confirmarPassword)
        {
            if (!EsAdministrador && id != UsuarioActualId) return Forbid();

            var usuario = await _repositorioUsuario.ObtenerPorIdAsync(id);
            if (usuario == null) return NotFound();

            var esPropioPerfil = id == UsuarioActualId;
            if (esPropioPerfil && !VerificarPassword(passwordActual ?? string.Empty, usuario.Password))
            {
                TempData["Error"] = "La contraseña actual no es correcta.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < 6)
            {
                TempData["Error"] = "La contraseña nueva debe tener al menos 6 caracteres.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (nuevaPassword != confirmarPassword)
            {
                TempData["Error"] = "Las contraseñas nuevas no coinciden.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await _repositorioUsuario.ActualizarPasswordAsync(id, HashearPassword(nuevaPassword));
            TempData["Mensaje"] = "Contraseña actualizada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Cambio de avatar: la otra parte de "manipular su propio perfil". Un solo archivo por
        // usuario (avatar_{id}.ext); si ya tenía uno con otra extensión, se borra antes de guardar.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SubirAvatar(int id, IFormFile archivo)
        {
            if (!EsAdministrador && id != UsuarioActualId) return Forbid();

            if (archivo == null || archivo.Length == 0)
            {
                TempData["Error"] = "Elegí una imagen para subir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
            {
                TempData["Error"] = "Formato no permitido. Usá JPG, PNG o WEBP.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (archivo.Length > TamanioMaximoBytes)
            {
                TempData["Error"] = "La imagen supera el tamaño máximo permitido (2 MB).";
                return RedirectToAction(nameof(Details), new { id });
            }

            var carpeta = Path.Combine(_entorno.WebRootPath, "uploads", "avatares");
            var nombreArchivo = $"avatar_{id}{extension}";
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            try
            {
                Directory.CreateDirectory(carpeta);

                // Borra cualquier avatar anterior del usuario (puede haber quedado con otra extensión).
                foreach (var archivoExistente in Directory.GetFiles(carpeta, $"avatar_{id}.*"))
                    System.IO.File.Delete(archivoExistente);

                using (var stream = new FileStream(rutaFisica, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }
            }
            catch (IOException)
            {
                TempData["Error"] = "No se pudo guardar el avatar en el servidor. Probá de nuevo.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "No se pudo guardar el avatar: sin permisos de escritura en el servidor.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await _repositorioUsuario.ActualizarAvatarAsync(id, $"/uploads/avatares/{nombreArchivo}");
            TempData["Mensaje"] = "Avatar actualizado.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EliminarAvatar(int id)
        {
            if (!EsAdministrador && id != UsuarioActualId) return Forbid();

            var usuario = await _repositorioUsuario.ObtenerPorIdAsync(id);
            if (usuario?.Avatar != null)
            {
                var rutaFisica = Path.Combine(
                    _entorno.WebRootPath,
                    usuario.Avatar.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(rutaFisica))
                    System.IO.File.Delete(rutaFisica);
            }

            await _repositorioUsuario.ActualizarAvatarAsync(id, null);
            TempData["Mensaje"] = "Avatar eliminado.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Baja lógica: solo un administrador puede eliminar entidades.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _repositorioUsuario.EliminarAsync(id);
            TempData["Mensaje"] = "Usuario eliminado.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------
        // Seguridad: hashing de contraseña y generación del JWT.
        // Antes vivían en clases aparte (Seguridad/PasswordHasher y Seguridad/TokenService);
        // se dejan acá como métodos privados para que todo el flujo de usuario quede en un
        // solo archivo, como en el resto del curso.
        // ------------------------------------------------------------------

        private string HashearPassword(string password)
        {
            var salt = _configuration["Salt"]
                ?? throw new InvalidOperationException("Falta configurar \"Salt\" en appsettings.json.");

            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password: Encoding.UTF8.GetBytes(password),
                salt: Encoding.UTF8.GetBytes(salt),
                iterations: IteracionesHash,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: LargoHashBytes);

            return Convert.ToBase64String(hash);
        }

        private bool VerificarPassword(string password, string hashAlmacenado) =>
            HashearPassword(password) == hashAlmacenado;

        private string GenerarToken(Usuario usuario)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["TokenAuthentication:SecretKey"]!));
            var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // ClaimTypes.NameIdentifier guarda el Id: lo usa ControladorBase para saber quién está logueado.
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(ClaimTypes.Name, usuario.Email),
                new("FullName", $"{usuario.Nombre} {usuario.Apellido}"),
                new(ClaimTypes.Role, usuario.Rol.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["TokenAuthentication:Issuer"],
                audience: _configuration["TokenAuthentication:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: credenciales);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}