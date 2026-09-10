using Microsoft.AspNetCore.Mvc;
using Reservas_Temporales.Models;
using Reservas_Temporales.Repositorios;
using System.Linq;

namespace Reservas_Temporales.Controllers
{
    public class TipoInmuebleController : ControladorBase
    {
        private readonly RepositorioTipoInmueble _repositorioTipoInmueble;

        public TipoInmuebleController(RepositorioTipoInmueble repositorioTipoInmueble)
        {
            _repositorioTipoInmueble = repositorioTipoInmueble;
        }

        // Vista Vue con paginado por servidor.
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> ListarJson(int pagina = 1, int tamanioPagina = 10)
        {
            var (items, total) = await _repositorioTipoInmueble.ListarPaginadoAsync(pagina, tamanioPagina);
            return Json(new
            {
                items = items.Select(t => new { t.Id, t.Nombre, t.Activo }),
                total,
                pagina,
                tamanioPagina,
                totalPaginas = (int)Math.Ceiling(total / (double)tamanioPagina)
            });
        }

        public IActionResult Create() => View(new TipoInmueble());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TipoInmueble tipo)
        {
            if (!ModelState.IsValid) return View(tipo);
            await _repositorioTipoInmueble.CrearAsync(tipo);
            TempData["Mensaje"] = "Tipo de inmueble creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tipo = await _repositorioTipoInmueble.ObtenerPorIdAsync(id);
            if (tipo == null) return NotFound();
            return View(tipo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TipoInmueble tipo)
        {
            if (id != tipo.Id) return BadRequest();
            if (!ModelState.IsValid) return View(tipo);
            await _repositorioTipoInmueble.ActualizarAsync(tipo);
            TempData["Mensaje"] = "Tipo de inmueble actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Baja lógica: solo un administrador puede eliminar entidades.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            if (!EsAdministrador) return Forbid();

            await _repositorioTipoInmueble.EliminarAsync(id);
            TempData["Mensaje"] = "Tipo de inmueble eliminado.";
            return RedirectToAction(nameof(Index));
        }
    }
}