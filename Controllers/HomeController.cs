using Microsoft.AspNetCore.Mvc;
using Reservas_Temporales.Models;
using Reservas_Temporales.Repositorios;
using Reservas_Temporales.ViewModels;

namespace Reservas_Temporales.Controllers
{
    public class HomeController : ControladorBase
    {
        private readonly RepositorioInmueble _repositorioInmueble;
        private readonly RepositorioReserva _repositorioReserva;

        public HomeController(RepositorioInmueble repositorioInmueble, RepositorioReserva repositorioReserva)
        {
            _repositorioInmueble = repositorioInmueble;
            _repositorioReserva = repositorioReserva;
        }

        public async Task<IActionResult> Index()
        {
            var inmuebles = await _repositorioInmueble.ListarAsync();
            var vigentes = await _repositorioReserva.ListarVigentesAsync();
            var proximasATerminar = await _repositorioReserva.ListarQueTerminanEnXDiasAsync(7);

            var modelo = new DashboardViewModel
            {
                TotalInmuebles = inmuebles.Count,
                InmueblesDisponibles = inmuebles.Count(i => i.Estado == EstadoInmueble.Disponible),
                ReservasVigentes = vigentes.Count,
                ReservasProximasATerminar = proximasATerminar
            };

            return View(modelo);
        }
    }
}