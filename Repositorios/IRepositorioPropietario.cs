using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public interface IRepositorioPropietario : IRepositorio<Propietario>
    {
        Task<List<Propietario>> ListarAsync(bool soloActivos = true);

        Task<(List<Propietario> Items, int Total)> ListarPaginadoAsync(
            int pagina, int tamanioPagina, bool soloActivos = true);

        Task ActualizarAsync(Propietario propietario);

        // Baja lógica: solo un administrador puede invocarla (se valida en el controller).
        Task EliminarAsync(int id);
    }
}