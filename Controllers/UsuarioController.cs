using Microsoft.AspNetCore.Mvc;
using Reservas_Temporales.Models;
using Reservas_Temporales.Repositorios;
using Reservas_Temporales.Seguridad;
using System.Linq;

namespace Reservas_Temporales.Controllers
{
    public class UsuarioController : ControladorBase
    {
        private readonly RepositorioUsuario _repositorioUsuario;

        public UsuarioController(RepositorioUsuario repositorioUsuario)
        {
            _repositorioUsuario = repositorioUsuario;
        }

        // Solo los administradores gestionan a otros usuarios. Vista Vue con paginado por servidor.
        public IActionResult Index()
        {
            if (!EsAdministrador) return Forbid();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ListarJson(int pagina = 1, int tamanioPagina = 10)
        {
            if (!EsAdministrador) return Forbid();

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

        // Un empleado puede ver su propio perfil; un administrador puede ver cualquiera.
        public async Task<IActionResult> Details(int id)
        {
            if (!EsAdministrador && id != UsuarioActualId) return Forbid();

            var usuario = await _repositorioUsuario.ObtenerPorIdAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        public IActionResult Create()
        {
            if (!EsAdministrador) return Forbid();
            return View(new Usuario());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario, string password)
        {
            if (!EsAdministrador) return Forbid();

            // Password no viaja como campo del modelo (se recibe aparte y se hashea),
            // así que se excluye de la validación automática del [Required] del modelo.
            ModelState.Remove(nameof(Usuario.Password));
            if (!ModelState.IsValid) return View(usuario);

            usuario.Password = PasswordHasher.Hashear(password);

            var id = await _repositorioUsuario.CrearAsync(usuario);
            TempData["Mensaje"] = "Usuario creado correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Un empleado solo puede editar su propio perfil; un administrador puede editar cualquiera.
        public async Task<IActionResult> Edit(int id)
        {
            if (!EsAdministrador && id != UsuarioActualId) return Forbid();

            var usuario = await _repositorioUsuario.ObtenerPorIdAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Usuario usuario)
        {
            if (!EsAdministrador && id != UsuarioActualId) return Forbid();
            if (id != usuario.Id) return BadRequest();

            // El formulario de edición no incluye Password (se cambia aparte, con hash);
            // se excluye del [Required] del modelo para no invalidar el POST.
            ModelState.Remove(nameof(Usuario.Password));
            if (!ModelState.IsValid) return View(usuario);

            // Un empleado no puede cambiarse el rol ni desactivarse a sí mismo desde su propio perfil.
            if (!EsAdministrador)
            {
                var actual = await _repositorioUsuario.ObtenerPorIdAsync(id);
                if (actual == null) return NotFound();
                usuario.Rol = actual.Rol;
                usuario.Activo = actual.Activo;
            }

            await _repositorioUsuario.ActualizarAsync(usuario);
            TempData["Mensaje"] = "Perfil actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Cambio de contraseña y avatar: parte de "manipular su propio perfil" para los empleados.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(int id, string nuevaPassword)
        {
            if (!EsAdministrador && id != UsuarioActualId) return Forbid();

            await _repositorioUsuario.ActualizarPasswordAsync(id, PasswordHasher.Hashear(nuevaPassword));
            TempData["Mensaje"] = "Contraseña actualizada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Baja lógica: solo un administrador puede eliminar entidades.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            if (!EsAdministrador) return Forbid();

            await _repositorioUsuario.EliminarAsync(id);
            TempData["Mensaje"] = "Usuario eliminado.";
            return RedirectToAction(nameof(Index));
        }
    }
}