using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    // No hereda un "ActualizarAsync" genérico porque no existe: las imágenes no se editan,
    // se reemplazan (Crear) o se marcan como portada (MarcarComoPortadaAsync).
    public interface IRepositorioImagenInmueble : IRepositorio<ImagenInmueble>
    {
        Task<List<ImagenInmueble>> ListarPorInmuebleAsync(int idInmueble);

        Task MarcarComoPortadaAsync(int id, int idInmueble);

        // Acá sí es un borrado físico (no baja lógica): el archivo también se elimina del disco.
        Task EliminarAsync(int id);
    }
}