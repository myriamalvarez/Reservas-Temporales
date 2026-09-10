using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public interface IRepositorioTipoInmueble : IRepositorio<TipoInmueble>
    {
        Task<List<TipoInmueble>> ListarAsync(bool soloActivos = true);

        Task<(List<TipoInmueble> Items, int Total)> ListarPaginadoAsync(int pagina, int tamanioPagina);

        Task ActualizarAsync(TipoInmueble tipo);

        // Baja lógica: solo un administrador puede invocarla (se valida en el controller).
        Task EliminarAsync(int id);
    }
}