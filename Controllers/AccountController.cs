using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservas_Temporales.Repositorios;
using Reservas_Temporales.Seguridad;

namespace Reservas_Temporales.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IRepositorioUsuario _repositorioUsuario;
        private readonly TokenService _tokenService;
        private readonly PasswordHasher _passwordHasher;

        public AccountController(
            IRepositorioUsuario repositorioUsuario, TokenService tokenService, PasswordHasher passwordHasher)
        {
            _repositorioUsuario = repositorioUsuario;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
        }

        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            var usuario = await _repositorioUsuario.ObtenerPorEmailAsync(email);
            if (usuario == null || !usuario.Activo || !_passwordHasher.Verificar(password, usuario.Password))
            {
                ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
                return View();
            }

            var token = _tokenService.GenerarToken(usuario);

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

        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token");
            return RedirectToAction(nameof(Login));
        }

        public IActionResult AccesoDenegado() => View();
    }
}