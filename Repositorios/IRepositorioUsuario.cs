using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public interface IRepositorioUsuario : IRepositorio<Usuario>
    {
        Task<List<Usuario>> ListarAsync(bool soloActivos = true);

        Task<(List<Usuario> Items, int Total)> ListarPaginadoAsync(
            int pagina, int tamanioPagina, bool soloActivos = true);

        // Usado por AccountController para el login.
        Task<Usuario?> ObtenerPorEmailAsync(string email);

        Task ActualizarAsync(Usuario usuario);

        Task ActualizarPasswordAsync(int id, string nuevoHashPassword);

        Task ActualizarAvatarAsync(int id, string? avatarPath);

        // Baja lógica: solo un administrador puede invocarla (se valida en el controller).
        Task EliminarAsync(int id);
    }
}