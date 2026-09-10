namespace Reservas_Temporales.ViewModels
{
    public class InmuebleConteoReservas
    {
        public int Id { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public string PropietarioNombre { get; set; } = string.Empty;
        public int CantidadReservas { get; set; }
    }

    public class InmuebleSinReservas
    {
        public int Id { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public string PropietarioNombre { get; set; } = string.Empty;
        public DateTime? UltimaReserva { get; set; }
    }
}