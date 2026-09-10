using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Reservas_Temporales.Models;
using Reservas_Temporales.Repositorios;
using System.Linq;

namespace Reservas_Temporales.Controllers
{
    public class InmuebleController : ControladorBase
    {
        private readonly IRepositorioInmueble _repositorioInmueble;
        private readonly IRepositorioPropietario _repositorioPropietario;
        private readonly IRepositorioTipoInmueble _repositorioTipoInmueble;
        private readonly IRepositorioImagenInmueble _repositorioImagenInmueble;

        public InmuebleController(
            IRepositorioInmueble repositorioInmueble,
            IRepositorioPropietario repositorioPropietario,
            IRepositorioTipoInmueble repositorioTipoInmueble,
            IRepositorioImagenInmueble repositorioImagenInmueble)
        {
            _repositorioInmueble = repositorioInmueble;
            _repositorioPropietario = repositorioPropietario;
            _repositorioTipoInmueble = repositorioTipoInmueble;
            _repositorioImagenInmueble = repositorioImagenInmueble;
        }

        // La vista Index es una página Vue: solo carga los combos de filtros server-side.
        // Los datos de la tabla se piden por AJAX a ListarJson, ya paginados.
        // idPropietario/estado opcionales permiten llegar prefiltrado (por ejemplo, desde Propietario/Details).
        public async Task<IActionResult> Index(int? idPropietario, EstadoInmueble? estado)
        {
            ViewBag.Propietarios = await _repositorioPropietario.ListarAsync();
            ViewBag.IdPropietarioInicial = idPropietario;
            ViewBag.EstadoInicial = estado?.ToString() ?? "";
            return View();
        }

        // Informe: listado general de inmuebles y su dueño, filtrable por disponibilidad y propietario.
        // Paginado por servidor: Vue pide una página a la vez y arma los controles con "total".
        [HttpGet]
        public async Task<IActionResult> ListarJson(
            int pagina = 1, int tamanioPagina = 10, EstadoInmueble? estado = null, int? idPropietario = null)
        {
            var (items, total) = await _repositorioInmueble.ListarPaginadoAsync(
                pagina, tamanioPagina, estado: estado, idPropietario: idPropietario);

            return Json(new
            {
                items = items.Select(i => new
                {
                    i.Id,
                    i.Direccion,
                    i.Cupo,
                    i.PrecioDia,
                    i.PorcentajeSena,
                    Estado = i.Estado.ToString(),
                    Tipo = i.Tipo?.Nombre,
                    Propietario = $"{i.Propietario?.Nombre} {i.Propietario?.Apellido}"
                }),
                total,
                pagina,
                tamanioPagina,
                totalPaginas = (int)Math.Ceiling(total / (double)tamanioPagina)
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            var inmueble = await _repositorioInmueble.ObtenerPorIdAsync(id);
            if (inmueble == null) return NotFound();
            inmueble.Imagenes = await _repositorioImagenInmueble.ListarPorInmuebleAsync(id);
            return View(inmueble);
        }

        public async Task<IActionResult> Create()
        {
            await CargarListasAsync();
            return View(new Inmueble());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Inmueble inmueble)
        {
            if (!ModelState.IsValid)
            {
                await CargarListasAsync(inmueble.IdPropietario, inmueble.IdTipo);
                return View(inmueble);
            }

            var id = await _repositorioInmueble.CrearAsync(inmueble);
            TempData["Mensaje"] = "Inmueble creado correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var inmueble = await _repositorioInmueble.ObtenerPorIdAsync(id);
            if (inmueble == null) return NotFound();
            await CargarListasAsync(inmueble.IdPropietario, inmueble.IdTipo);
            return View(inmueble);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Inmueble inmueble)
        {
            if (id != inmueble.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                await CargarListasAsync(inmueble.IdPropietario, inmueble.IdTipo);
                return View(inmueble);
            }

            await _repositorioInmueble.ActualizarAsync(inmueble);
            TempData["Mensaje"] = "Inmueble actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // El propietario suspende o reactiva la oferta; no afecta reservas ya creadas.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, EstadoInmueble estado)
        {
            await _repositorioInmueble.CambiarEstadoAsync(id, estado);
            TempData["Mensaje"] = estado == EstadoInmueble.Suspendido
                ? "El inmueble fue suspendido de la oferta."
                : "El inmueble vuelve a estar disponible.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Baja lógica: solo un administrador puede eliminar entidades.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _repositorioInmueble.EliminarAsync(id);
            TempData["Mensaje"] = "Inmueble eliminado.";
            return RedirectToAction(nameof(Index));
        }

        // Búsqueda de inmuebles no ocupados en un rango de fechas, previa a crear una reserva.
        public async Task<IActionResult> Buscar()
        {
            ViewBag.Tipos = new SelectList(await _repositorioTipoInmueble.ListarAsync(), "Id", "Nombre");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Buscar(DateTime fechaDesde, DateTime fechaHasta, int? cupoMinimo, int? idTipo)
        {
            if (fechaHasta <= fechaDesde)
                ModelState.AddModelError(string.Empty, "La fecha hasta debe ser posterior a la fecha desde.");

            ViewBag.Tipos = new SelectList(await _repositorioTipoInmueble.ListarAsync(), "Id", "Nombre", idTipo);

            if (!ModelState.IsValid) return View();

            var disponibles = await _repositorioInmueble.BuscarDisponiblesAsync(fechaDesde, fechaHasta, cupoMinimo, idTipo);
            ViewBag.FechaDesde = fechaDesde;
            ViewBag.FechaHasta = fechaHasta;
            return View("ResultadoBusqueda", disponibles);
        }

        private async Task CargarListasAsync(int? idPropietario = null, int? idTipo = null)
        {
            ViewBag.Propietarios = new SelectList(
                await _repositorioPropietario.ListarAsync(), "Id", "Nombre", idPropietario);
            ViewBag.Tipos = new SelectList(
                await _repositorioTipoInmueble.ListarAsync(), "Id", "Nombre", idTipo);
        }
    }
}