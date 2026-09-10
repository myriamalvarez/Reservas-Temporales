namespace Reservas_Temporales.Repositorios
{
    // Contrato mínimo común a todos los repositorios de entidades: los dos únicos métodos
    // que existen, con la misma firma, en TODOS ellos. El resto de las operaciones
    // (listar, actualizar, eliminar/anular, búsquedas específicas...) varían según la
    // entidad y las reglas de negocio de cada una, así que se declaran en la interfaz
    // específica de cada repositorio (IRepositorioPropietario, IRepositorioReserva, etc.),
    // no acá.
    public interface IRepositorio<T>
    {
        Task<T?> ObtenerPorIdAsync(int id);
        Task<int> CrearAsync(T entidad);
    }
}