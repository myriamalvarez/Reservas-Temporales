using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Reservas_Temporales.Models;
using Reservas_Temporales.Repositorios;
using System.Linq;

namespace Reservas_Temporales.Controllers
{
    public class ReservaController : ControladorBase
    {
        private readonly IRepositorioReserva _repositorioReserva;
        private readonly IRepositorioPago _repositorioPago;
        private readonly IRepositorioInquilino _repositorioInquilino;

        public ReservaController(
            IRepositorioReserva repositorioReserva,
            IRepositorioPago repositorioPago,
            IRepositorioInquilino repositorioInquilino)
        {
            _repositorioReserva = repositorioReserva;
            _repositorioPago = repositorioPago;
            _repositorioInquilino = repositorioInquilino;
        }

        // Vista Vue con paginado por servidor de reservas vigentes.
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> ListarJson(int pagina = 1, int tamanioPagina = 10)
        {
            var (items, total) = await _repositorioReserva.ListarVigentesPaginadoAsync(pagina, tamanioPagina);
            return Json(new
            {
                items = items.Select(r => new
                {
                    r.Id,
                    FechaDesde = r.FechaDesde.ToString("dd/MM/yyyy"),
                    FechaHasta = r.FechaHasta.ToString("dd/MM/yyyy"),
                    r.MontoDiario,
                    Estado = r.Estado.ToString(),
                    r.InmuebleDireccion,
                    r.InquilinoNombre
                }),
                total,
                pagina,
                tamanioPagina,
                totalPaginas = (int)Math.Ceiling(total / (double)tamanioPagina)
            });
        }

        // Informe: reservas que terminan dentro de los próximos X días (plazo configurable).
        public async Task<IActionResult> ProximasATerminar(int dias = 7)
        {
            var reservas = await _repositorioReserva.ListarQueTerminanEnXDiasAsync(dias);
            ViewBag.Dias = dias;
            return View(reservas);
        }

        public async Task<IActionResult> Details(int id)
        {
            var reserva = await _repositorioReserva.ObtenerPorIdAsync(id);
            if (reserva == null) return NotFound();
            reserva.Pagos = await _repositorioPago.ListarPorReservaAsync(id);
            return View(reserva);
        }

        // idInmueble y fechas llegan como querystring desde el resultado de Inmueble/Buscar.
        // El inquilino se elige en la propia vista (dropdown), por eso no es obligatorio acá.
        public async Task<IActionResult> Create(int idInmueble, DateTime fechaDesde, DateTime fechaHasta)
        {
            await CargarInquilinosAsync();
            return View(new Reserva
            {
                IdInmueble = idInmueble,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reserva reserva)
        {
            if (reserva.FechaHasta <= reserva.FechaDesde)
                ModelState.AddModelError(string.Empty, "La fecha hasta debe ser posterior a la fecha desde.");

            // La narrativa exige volver a verificar la disponibilidad justo antes de confirmar.
            if (await _repositorioReserva.ExisteSolapamientoAsync(reserva.IdInmueble, reserva.FechaDesde, reserva.FechaHasta))
                ModelState.AddModelError(string.Empty, "El inmueble ya no está disponible en esas fechas.");

            if (!ModelState.IsValid)
            {
                await CargarInquilinosAsync(reserva.IdInquilino);
                return View(reserva);
            }

            reserva.CreadoPorUserId = UsuarioActualId
                ?? throw new InvalidOperationException("No hay un usuario en sesión.");

            var id = await _repositorioReserva.CrearAsync(reserva);
            TempData["Mensaje"] = "Reserva creada correctamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Renovación: se crea una reserva nueva con el mismo inquilino e inmueble; la original no se toca.
        public async Task<IActionResult> Renovar(int id)
        {
            var reservaOriginal = await _repositorioReserva.ObtenerPorIdAsync(id);
            if (reservaOriginal == null) return NotFound();

            await CargarInquilinosAsync(reservaOriginal.IdInquilino);
            return View("Create", new Reserva
            {
                IdInmueble = reservaOriginal.IdInmueble,
                IdInquilino = reservaOriginal.IdInquilino,
                MontoDiario = reservaOriginal.MontoDiario,
                FechaDesde = reservaOriginal.FechaHasta.AddDays(1)
            });
        }

        public async Task<IActionResult> TerminarAnticipada(int id)
        {
            var reserva = await _repositorioReserva.ObtenerPorIdAsync(id);
            if (reserva == null) return NotFound();
            return View(reserva);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TerminarAnticipada(int id, DateTime fechaTerminacion)
        {
            var reserva = await _repositorioReserva.ObtenerPorIdAsync(id);
            if (reserva == null) return NotFound();

            if (fechaTerminacion < reserva.FechaDesde || fechaTerminacion >= reserva.FechaHastaOriginal)
            {
                ModelState.AddModelError(string.Empty,
                    "La fecha de terminación debe estar dentro del plazo original de la reserva.");
                return View(reserva);
            }

            var multa = CalcularMulta(reserva, fechaTerminacion);
            var usuarioId = UsuarioActualId ?? throw new InvalidOperationException("No hay un usuario en sesión.");

            await _repositorioReserva.TerminarAnticipadamenteAsync(id, fechaTerminacion, multa, usuarioId);

            // La multa se informa y se carga como pago de la reserva en la misma pantalla.
            await _repositorioPago.CrearAsync(new Pago
            {
                IdReserva = id,
                Concepto = "Multa por terminación anticipada",
                FechaPago = fechaTerminacion,
                Importe = multa,
                CreadoPorUserId = usuarioId
            });

            TempData["Mensaje"] = $"Reserva terminada anticipadamente. Multa calculada: {multa:C}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Baja lógica: solo un administrador puede eliminar entidades.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _repositorioReserva.EliminarAsync(id);
            TempData["Mensaje"] = "Reserva eliminada.";
            return RedirectToAction(nameof(Index));
        }

        // Regla de la narrativa: si se cumplió menos de la mitad del plazo original, la multa
        // es el 50% del alquiler restante; si se cumplió la mitad o más, es el 25%. No hay devoluciones.
        private static decimal CalcularMulta(Reserva reserva, DateTime fechaTerminacion)
        {
            var diasTotales = (reserva.FechaHastaOriginal.Date - reserva.FechaDesde.Date).Days;
            var diasCumplidos = (fechaTerminacion.Date - reserva.FechaDesde.Date).Days;
            var diasRestantes = diasTotales - diasCumplidos;
            var montoRestante = diasRestantes * reserva.MontoDiario;
            var porcentaje = diasCumplidos < diasTotales / 2.0 ? 0.50m : 0.25m;
            return Math.Round(montoRestante * porcentaje, 2);
        }

        private async Task CargarInquilinosAsync(int? idSeleccionado = null)
        {
            var inquilinos = await _repositorioInquilino.ListarAsync();
            ViewBag.Inquilinos = new SelectList(
                inquilinos.Select(i => new { i.Id, NombreCompleto = $"{i.Apellido}, {i.Nombre} (DNI {i.Dni})" }),
                "Id", "NombreCompleto", idSeleccionado);
        }
    }
}