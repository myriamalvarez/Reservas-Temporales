using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public class RepositorioPropietario : RepositorioBase, IRepositorioPropietario
    {
        public RepositorioPropietario(IConfiguration configuration) : base(configuration)
        {
        }

        private const string ColumnasBase = "id, nombre, apellido, dni, email, telefono, activo";

        public async Task<List<Propietario>> ListarAsync(bool soloActivos = true)
        {
            var propietarios = new List<Propietario>();
            using var conexion = ObtenerConexion();
            var sql = $"SELECT {ColumnasBase} FROM propietario" +
                      (soloActivos ? " WHERE activo = 1" : "") +
                      " ORDER BY apellido, nombre";
            using var comando = new MySqlCommand(sql, conexion);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                propietarios.Add(Mapear(lector));
            return propietarios;
        }

        // Paginado por servidor para el listado con Vue.
        public async Task<(List<Propietario> Items, int Total)> ListarPaginadoAsync(
            int pagina, int tamanioPagina, bool soloActivos = true)
        {
            if (pagina < 1) pagina = 1;
            if (tamanioPagina < 1) tamanioPagina = 10;

            var whereSql = soloActivos ? " WHERE activo = 1" : "";

            using var conexion = ObtenerConexion();
            await conexion.OpenAsync();

            var sqlTotal = "SELECT COUNT(*) FROM propietario" + whereSql;
            using var comandoTotal = new MySqlCommand(sqlTotal, conexion);
            var total = Convert.ToInt32(await comandoTotal.ExecuteScalarAsync());

            var sqlPagina = $"SELECT {ColumnasBase} FROM propietario" + whereSql +
                             " ORDER BY apellido, nombre LIMIT @tamanioPagina OFFSET @offset";
            using var comandoPagina = new MySqlCommand(sqlPagina, conexion);
            comandoPagina.Parameters.AddWithValue("@tamanioPagina", tamanioPagina);
            comandoPagina.Parameters.AddWithValue("@offset", (pagina - 1) * tamanioPagina);

            var items = new List<Propietario>();
            using var lectorPagina = await comandoPagina.ExecuteReaderAsync();
            while (await lectorPagina.ReadAsync())
                items.Add(Mapear(lectorPagina));

            return (items, total);
        }

        public async Task<Propietario?> ObtenerPorIdAsync(int id)
        {
            using var conexion = ObtenerConexion();
            var sql = $"SELECT {ColumnasBase} FROM propietario WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? Mapear(lector) : null;
        }

        public async Task<int> CrearAsync(Propietario propietario)
        {
            using var conexion = ObtenerConexion();
            var sql = "INSERT INTO propietario (nombre, apellido, dni, email, telefono, activo) " +
                      "VALUES (@nombre, @apellido, @dni, @email, @telefono, @activo); " +
                      "SELECT LAST_INSERT_ID();";
            using var comando = new MySqlCommand(sql, conexion);
            AgregarParametros(comando, propietario);
            await conexion.OpenAsync();
            var id = await comando.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task ActualizarAsync(Propietario propietario)
        {
            using var conexion = ObtenerConexion();
            var sql = "UPDATE propietario SET nombre = @nombre, apellido = @apellido, dni = @dni, " +
                      "email = @email, telefono = @telefono, activo = @activo WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", propietario.Id);
            AgregarParametros(comando, propietario);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        // Baja lógica: el controller debe validar que quien la invoca es administrador.
        public async Task EliminarAsync(int id)
        {
            using var conexion = ObtenerConexion();
            var sql = "UPDATE propietario SET activo = 0 WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        private static void AgregarParametros(MySqlCommand comando, Propietario propietario)
        {
            comando.Parameters.AddWithValue("@nombre", propietario.Nombre);
            comando.Parameters.AddWithValue("@apellido", propietario.Apellido);
            comando.Parameters.AddWithValue("@dni", propietario.Dni);
            comando.Parameters.AddWithValue("@email", (object?)propietario.Email ?? DBNull.Value);
            comando.Parameters.AddWithValue("@telefono", (object?)propietario.Telefono ?? DBNull.Value);
            comando.Parameters.AddWithValue("@activo", propietario.Activo);
        }

        private static Propietario Mapear(MySqlDataReader lector) => new()
        {
            Id = lector.GetInt32("id"),
            Nombre = lector.GetString("nombre"),
            Apellido = lector.GetString("apellido"),
            Dni = lector.GetString("dni"),
            Email = lector.IsDBNull(lector.GetOrdinal("email")) ? null : lector.GetString("email"),
            Telefono = lector.IsDBNull(lector.GetOrdinal("telefono")) ? null : lector.GetString("telefono"),
            Activo = lector.GetBoolean("activo")
        };
    }
}