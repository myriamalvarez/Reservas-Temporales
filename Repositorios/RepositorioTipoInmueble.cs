using MySqlConnector;
using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public class RepositorioTipoInmueble
    {
        private readonly ConexionBD _conexionBD;

        public RepositorioTipoInmueble(ConexionBD conexionBD)
        {
            _conexionBD = conexionBD;
        }

        public async Task<List<TipoInmueble>> ListarAsync(bool soloActivos = true)
        {
            var tipos = new List<TipoInmueble>();
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "SELECT id, nombre, activo FROM tipo_inmueble" +
                      (soloActivos ? " WHERE activo = 1" : "") +
                      " ORDER BY nombre";
            using var comando = new MySqlCommand(sql, conexion);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                tipos.Add(Mapear(lector));
            return tipos;
        }

        // Paginado por servidor. Es un catálogo chico, pero se mantiene el mismo patrón por consistencia.
        public async Task<(List<TipoInmueble> Items, int Total)> ListarPaginadoAsync(int pagina, int tamanioPagina)
        {
            if (pagina < 1) pagina = 1;
            if (tamanioPagina < 1) tamanioPagina = 10;

            using var conexion = _conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            var sqlTotal = "SELECT COUNT(*) FROM tipo_inmueble";
            using var comandoTotal = new MySqlCommand(sqlTotal, conexion);
            var total = Convert.ToInt32(await comandoTotal.ExecuteScalarAsync());

            var sqlPagina = "SELECT id, nombre, activo FROM tipo_inmueble " +
                             "ORDER BY nombre LIMIT @tamanioPagina OFFSET @offset";
            using var comandoPagina = new MySqlCommand(sqlPagina, conexion);
            comandoPagina.Parameters.AddWithValue("@tamanioPagina", tamanioPagina);
            comandoPagina.Parameters.AddWithValue("@offset", (pagina - 1) * tamanioPagina);

            var items = new List<TipoInmueble>();
            using var lectorPagina = await comandoPagina.ExecuteReaderAsync();
            while (await lectorPagina.ReadAsync())
                items.Add(Mapear(lectorPagina));

            return (items, total);
        }

        public async Task<TipoInmueble?> ObtenerPorIdAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "SELECT id, nombre, activo FROM tipo_inmueble WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? Mapear(lector) : null;
        }

        public async Task<int> CrearAsync(TipoInmueble tipo)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "INSERT INTO tipo_inmueble (nombre, activo) VALUES (@nombre, @activo); " +
                      "SELECT LAST_INSERT_ID();";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@nombre", tipo.Nombre);
            comando.Parameters.AddWithValue("@activo", tipo.Activo);
            await conexion.OpenAsync();
            var id = await comando.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task ActualizarAsync(TipoInmueble tipo)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE tipo_inmueble SET nombre = @nombre, activo = @activo WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", tipo.Id);
            comando.Parameters.AddWithValue("@nombre", tipo.Nombre);
            comando.Parameters.AddWithValue("@activo", tipo.Activo);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        // Baja lógica: el controller debe validar que quien la invoca es administrador.
        public async Task EliminarAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE tipo_inmueble SET activo = 0 WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        private static TipoInmueble Mapear(MySqlDataReader lector) => new()
        {
            Id = lector.GetInt32("id"),
            Nombre = lector.GetString("nombre"),
            Activo = lector.GetBoolean("activo")
        };
    }
}