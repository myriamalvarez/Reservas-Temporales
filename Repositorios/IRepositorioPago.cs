using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    // No hereda un "EliminarAsync": los pagos nunca se borran, se anulan (la narrativa
    // exige que sigan visibles marcados como anulados). Tampoco hay "ActualizarAsync"
    // genérico porque solo el concepto es editable, no fecha ni importe.
    public interface IRepositorioPago : IRepositorio<Pago>
    {
        // Informe: pagos de una reserva en particular.
        Task<List<Pago>> ListarPorReservaAsync(int idReserva);

        // La narrativa solo permite editar el concepto: fecha e importe quedan fijos.
        Task ActualizarConceptoAsync(int id, string nuevoConcepto);

        // La eliminación es un cambio de estado: el pago sigue visible, marcado como anulado.
        Task AnularAsync(int id, int usuarioId);
    }
}