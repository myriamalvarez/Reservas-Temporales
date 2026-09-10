using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public interface IRepositorioInquilino : IRepositorio<Inquilino>
    {
        Task<List<Inquilino>> ListarAsync(bool soloActivos = true);

        Task<(List<Inquilino> Items, int Total)> ListarPaginadoAsync(
            int pagina, int tamanioPagina, bool soloActivos = true);

        Task<Inquilino?> ObtenerPorDniAsync(string dni);

        Task ActualizarAsync(Inquilino inquilino);

        // Baja lógica: solo un administrador puede invocarla (se valida en el controller).
        Task EliminarAsync(int id);
    }
}