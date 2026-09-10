using Reservas_Temporales.Models;

namespace Reservas_Temporales.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalInmuebles { get; set; }
        public int InmueblesDisponibles { get; set; }
        public int ReservasVigentes { get; set; }
        public List<Reserva> ReservasProximasATerminar { get; set; } = new();
    }
}