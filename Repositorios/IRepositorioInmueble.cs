using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public interface IRepositorioInmueble : IRepositorio<Inmueble>
    {
        Task<List<Inmueble>> ListarAsync(
            bool soloActivos = true, EstadoInmueble? estado = null, int? idPropietario = null);

        Task<(List<Inmueble> Items, int Total)> ListarPaginadoAsync(
            int pagina, int tamanioPagina, bool soloActivos = true,
            EstadoInmueble? estado = null, int? idPropietario = null);

        // Búsqueda de inmuebles disponibles en un rango de fechas (narrativa: alta de reserva).
        Task<List<Inmueble>> BuscarDisponiblesAsync(
            DateTime fechaDesde, DateTime fechaHasta, int? cupoMinimo = null, int? idTipo = null);

        Task ActualizarAsync(Inmueble inmueble);

        // El propietario suspende/reactiva la oferta sin afectar reservas ya creadas.
        Task CambiarEstadoAsync(int id, EstadoInmueble estado);

        // Baja lógica: solo un administrador puede invocarla (se valida en el controller).
        Task EliminarAsync(int id);
    }
}