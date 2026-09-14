using Reservas_Temporales.Models;

namespace Reservas_Temporales.ViewModels
{
    // Para listados (evita traer el modelo completo con todas sus FKs).
    public class ReservaListadoItem
    {
        public int Id { get; set; }
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public decimal MontoDiario { get; set; }
        public EstadoReserva Estado { get; set; }
        public string InmuebleDireccion { get; set; } = string.Empty;
        public string InquilinoNombre { get; set; } = string.Empty;
    }
}