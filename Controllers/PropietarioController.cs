using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservas_Temporales.Models;
using Reservas_Temporales.Repositorios;
using System.Linq;

namespace Reservas_Temporales.Controllers
{
    public class PropietarioController : ControladorBase
    {
        private readonly IRepositorioPropietario _repositorioPropietario;

        public PropietarioController(IRepositorioPropietario repositorioPropietario)
        {
            _repositorioPropietario = repositorioPropietario;
        }

        // Vista Vue con paginado por servidor.
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> ListarJson(int pagina = 1, int tamanioPagina = 10)
        {
            var (items, total) = await _repositorioPropietario.ListarPaginadoAsync(pagina, tamanioPagina);
            return Json(new
            {
                items = items.Select(p => new { p.Id, p.Nombre, p.Apellido, p.Dni, p.Email, p.Telefono }),
                total,
                pagina,
                tamanioPagina,
                totalPaginas = (int)Math.Ceiling(total / (double)tamanioPagina)
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            var propietario = await _repositorioPropietario.ObtenerPorIdAsync(id);
            if (propietario == null) return NotFound();
            return View(propietario);
        }

        public IActionResult Create() => View(new Propietario());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Propietario propietario)
        {
            if (!ModelState.IsValid) return View(propietario);
            var id = await _repositorioPropietario.CrearAsync(propietario);
            TempData["Mensaje"] = "Propietario creado correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var propietario = await _repositorioPropietario.ObtenerPorIdAsync(id);
            if (propietario == null) return NotFound();
            return View(propietario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Propietario propietario)
        {
            if (id != propietario.Id) return BadRequest();
            if (!ModelState.IsValid) return View(propietario);
            await _repositorioPropietario.ActualizarAsync(propietario);
            TempData["Mensaje"] = "Propietario actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Baja lógica: solo un administrador puede eliminar entidades.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _repositorioPropietario.EliminarAsync(id);
            TempData["Mensaje"] = "Propietario eliminado.";
            return RedirectToAction(nameof(Index));
        }
    }
}