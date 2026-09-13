using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Reservas_Temporales.Models;
using Reservas_Temporales.Repositorios;
using System.Linq;

namespace Reservas_Temporales.Controllers
{
    public class InquilinoController : ControladorBase
    {
        private readonly IRepositorioInquilino _repositorioInquilino;

        public InquilinoController(IRepositorioInquilino repositorioInquilino)
        {
            _repositorioInquilino = repositorioInquilino;
        }

        // Vista Vue con paginado por servidor.
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> ListarJson(int pagina = 1, int tamanioPagina = 10)
        {
            var (items, total) = await _repositorioInquilino.ListarPaginadoAsync(pagina, tamanioPagina);
            return Json(new
            {
                items = items.Select(i => new { i.Id, i.Nombre, i.Apellido, i.Dni, i.Email, i.Telefono }),
                total,
                pagina,
                tamanioPagina,
                totalPaginas = (int)Math.Ceiling(total / (double)tamanioPagina)
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            var inquilino = await _repositorioInquilino.ObtenerPorIdAsync(id);
            if (inquilino == null) return NotFound();
            return View(inquilino);
        }

        public IActionResult Create() => View(new Inquilino());

        // ABM inquilino: se registra al entrevistarlo (DNI, nombre completo, datos de contacto).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Inquilino inquilino)
        {
            if (!ModelState.IsValid) return View(inquilino);

            var existente = await _repositorioInquilino.ObtenerPorDniAsync(inquilino.Dni);
            if (existente != null)
            {
                ModelState.AddModelError(nameof(Inquilino.Dni), "Ya existe un inquilino registrado con ese DNI.");
                return View(inquilino);
            }

            try
            {
                var id = await _repositorioInquilino.CrearAsync(inquilino);
                TempData["Mensaje"] = "Inquilino registrado correctamente.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Cubre la carrera entre el chequeo de arriba y el INSERT: si dos personas
                // cargan el mismo DNI casi al mismo tiempo, el chequeo previo no alcanza.
                ModelState.AddModelError(nameof(Inquilino.Dni), "Ya existe un inquilino registrado con ese DNI.");
                return View(inquilino);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var inquilino = await _repositorioInquilino.ObtenerPorIdAsync(id);
            if (inquilino == null) return NotFound();
            return View(inquilino);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Inquilino inquilino)
        {
            if (id != inquilino.Id) return BadRequest();
            if (!ModelState.IsValid) return View(inquilino);

            try
            {
                await _repositorioInquilino.ActualizarAsync(inquilino);
                TempData["Mensaje"] = "Inquilino actualizado correctamente.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                ModelState.AddModelError(nameof(Inquilino.Dni), "Ya existe otro inquilino con ese DNI.");
                return View(inquilino);
            }
        }

        // Baja lógica: solo un administrador puede eliminar entidades.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _repositorioInquilino.EliminarAsync(id);
            TempData["Mensaje"] = "Inquilino eliminado.";
            return RedirectToAction(nameof(Index));
        }
    }
}