using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservas_Temporales.Models;
using Reservas_Temporales.Repositorios;
using System.Linq;

namespace Reservas_Temporales.Controllers
{
    // Los pagos se cargan y anulan desde la pantalla de detalle de la reserva,
    // por eso esas acciones redirigen de vuelta a Reserva/Details. El listado general
    // (Index) sí es una pantalla propia, para poder encontrar un pago sin conocer antes
    // a qué reserva pertenece.
    public class PagoController : ControladorBase
    {
        private readonly IRepositorioPago _repositorioPago;

        public PagoController(IRepositorioPago repositorioPago)
        {
            _repositorioPago = repositorioPago;
        }

        // Vista Vue con paginado por servidor de todos los pagos.
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> ListarJson(int pagina = 1, int tamanioPagina = 10, bool? anulado = null)
        {
            var (items, total) = await _repositorioPago.ListarTodosPaginadoAsync(pagina, tamanioPagina, anulado);
            return Json(new
            {
                items = items.Select(p => new
                {
                    p.Id,
                    p.IdReserva,
                    p.Concepto,
                    FechaPago = p.FechaPago.ToString("dd/MM/yyyy"),
                    p.Importe,
                    p.Anulado,
                    p.InmuebleDireccion,
                    p.InquilinoNombre
                }),
                total,
                pagina,
                tamanioPagina,
                totalPaginas = (int)Math.Ceiling(total / (double)tamanioPagina)
            });
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