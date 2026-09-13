namespace Reservas_Temporales.ViewModels
{
    // Proyección liviana para el listado general de pagos (todas las reservas).
    public class PagoListadoItem
    {
        public int Id { get; set; }
        public int IdReserva { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public DateTime FechaPago { get; set; }
        public decimal Importe { get; set; }
        public bool Anulado { get; set; }
        public string InmuebleDireccion { get; set; } = string.Empty;
        public string InquilinoNombre { get; set; } = string.Empty;
    }
}