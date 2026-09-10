using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservas_Temporales.Models;
using Reservas_Temporales.Repositorios;

namespace Reservas_Temporales.Controllers
{
    // Los pagos se cargan y anulan desde la pantalla de detalle de la reserva,
    // por eso todas las acciones redirigen de vuelta a Reserva/Details.
    public class PagoController : ControladorBase
    {
        private readonly IRepositorioPago _repositorioPago;

        public PagoController(IRepositorioPago repositorioPago)
        {
            _repositorioPago = repositorioPago;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pago pago)
        {
            pago.CreadoPorUserId = UsuarioActualId
                ?? throw new InvalidOperationException("No hay un usuario en sesión.");

            await _repositorioPago.CrearAsync(pago);
            TempData["Mensaje"] = "Pago registrado correctamente.";
            return RedirectToAction("Details", "Reserva", new { id = pago.IdReserva });
        }

        // Solo el concepto es editable; fecha e importe quedan fijos según la narrativa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarConcepto(int id, int idReserva, string concepto)
        {
            await _repositorioPago.ActualizarConceptoAsync(id, concepto);
            TempData["Mensaje"] = "Concepto actualizado.";
            return RedirectToAction("Details", "Reserva", new { id = idReserva });
        }

        // La eliminación es un cambio de estado: el pago sigue visible, marcado como anulado.
        // Solo un administrador puede eliminar/anular entidades.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Anular(int id, int idReserva)
        {
            var usuarioId = UsuarioActualId ?? throw new InvalidOperationException("No hay un usuario en sesión.");
            await _repositorioPago.AnularAsync(id, usuarioId);
            TempData["Mensaje"] = "Pago anulado.";
            return RedirectToAction("Details", "Reserva", new { id = idReserva });
        }
    }
}