using Reservas_Temporales.Models;
using Reservas_Temporales.ViewModels;

namespace Reservas_Temporales.Repositorios
{
    public interface IRepositorioReserva : IRepositorio<Reserva>
    {
        // Informe: reservas vigentes (por fecha desde/hasta, no solo por el campo estado).
        Task<List<Reserva>> ListarVigentesAsync();

        Task<(List<ReservaListadoItem> Items, int Total)> ListarVigentesPaginadoAsync(
            int pagina, int tamanioPagina);

        // Listado general: todas las reservas (cualquier estado), con filtro opcional.
        Task<(List<ReservaListadoItem> Items, int Total)> ListarTodasPaginadoAsync(
            int pagina, int tamanioPagina, EstadoReserva? estado = null);

        // Informe: reservas que terminan dentro de los próximos X días.
        Task<List<Reserva>> ListarQueTerminanEnXDiasAsync(int dias);

        Task<List<Reserva>> ListarPorInmuebleAsync(int idInmueble);

        Task<List<Reserva>> ListarPorInquilinoAsync(int idInquilino);

        // Validación obligatoria antes de crear/renovar: que el inmueble siga libre en esas fechas.
        Task<bool> ExisteSolapamientoAsync(
            int idInmueble, DateTime fechaDesde, DateTime fechaHasta, int? idReservaExcluir = null);

        // Terminación anticipada: registra fecha efectiva y multa, sin tocar fecha_hasta_original.
        Task TerminarAnticipadamenteAsync(int id, DateTime fechaTerminacion, decimal multa, int usuarioId);

        Task FinalizarAsync(int id);

        // Baja lógica: solo un administrador puede invocarla (se valida en el controller).
        Task EliminarAsync(int id);
    }
}