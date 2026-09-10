using Microsoft.AspNetCore.Mvc;
using Reservas_Temporales.Repositorios;
using System.Linq;

namespace Reservas_Temporales.Controllers
{
    public class InformeController : ControladorBase
    {
        private readonly RepositorioInformes _repositorioInformes;

        public InformeController(RepositorioInformes repositorioInformes)
        {
            _repositorioInformes = repositorioInformes;
        }

        // Vista Vue con paginado por servidor.
        public IActionResult MasReservados() => View();

        [HttpGet]
        public async Task<IActionResult> MasReservadosJson(int pagina = 1, int tamanioPagina = 10, int dias = 365)
        {
            var (items, total) = await _repositorioInformes.ListarMasReservadosAsync(pagina, tamanioPagina, dias);
            return Json(new
            {
                items = items.Select(i => new { i.Id, i.Direccion, i.PropietarioNombre, i.CantidadReservas }),
                total,
                pagina,
                tamanioPagina,
                totalPaginas = (int)Math.Ceiling(total / (double)tamanioPagina)
            });
        }

        public IActionResult SinReservas() => View();

        [HttpGet]
        public async Task<IActionResult> SinReservasJson(int pagina = 1, int tamanioPagina = 10, int dias = 30)
        {
            var (items, total) = await _repositorioInformes.ListarSinReservasAsync(pagina, tamanioPagina, dias);
            return Json(new
            {
                items = items.Select(i => new
                {
                    i.Id,
                    i.Direccion,
                    i.PropietarioNombre,
                    UltimaReserva = i.UltimaReserva.HasValue ? i.UltimaReserva.Value.ToString("dd/MM/yyyy") : null
                }),
                total,
                pagina,
                tamanioPagina,
                totalPaginas = (int)Math.Ceiling(total / (double)tamanioPagina)
            });
        }
    }
}