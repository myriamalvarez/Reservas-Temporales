using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Reservas_Temporales.Models;

namespace Reservas_Temporales.Controllers
{
    // Centraliza el acceso a los datos del usuario logueado, leídos desde los claims del JWT
    // (ver AccountController y la configuración de JwtBearer en Program.cs).
    public abstract class ControladorBase : Controller
    {
        protected int? UsuarioActualId
        {
            get
            {
                var valor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(valor, out var id) ? id : null;
            }
        }

        protected bool EsAdministrador => User.IsInRole(nameof(RolUsuario.Administrador));
    }
}