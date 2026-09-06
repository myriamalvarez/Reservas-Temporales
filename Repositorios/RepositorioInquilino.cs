using MySqlConnector;
using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public class RepositorioInquilino
    {
        private readonly ConexionBD _conexionBD;

        public RepositorioInquilino(ConexionBD conexionBD)
        {
            _conexionBD = conexionBD;
        }

        private const string ColumnasBase = "id, nombre, apellido, dni, email, telefono, activo";

        public async Task<List<Inquilino>> ListarAsync(bool soloActivos = true)
        {
            var inquilinos = new List<Inquilino>();
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = $"SELECT {ColumnasBase} FROM inquilino" +
                      (soloActivos ? " WHERE activo = 1" : "") +
                      " ORDER BY apellido, nombre";
            using var comando = new MySqlCommand(sql, conexion);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                inquilinos.Add(Mapear(lector));
            return inquilinos;
        }

        public async Task<Inquilino?> ObtenerPorIdAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = $"SELECT {ColumnasBase} FROM inquilino WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? Mapear(lector) : null;
        }

        public async Task<Inquilino?> ObtenerPorDniAsync(string dni)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = $"SELECT {ColumnasBase} FROM inquilino WHERE dni = @dni";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@dni", dni);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? Mapear(lector) : null;
        }

        public async Task<int> CrearAsync(Inquilino inquilino)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "INSERT INTO inquilino (nombre, apellido, dni, email, telefono, activo) " +
                      "VALUES (@nombre, @apellido, @dni, @email, @telefono, @activo); " +
                      "SELECT LAST_INSERT_ID();";
            using var comando = new MySqlCommand(sql, conexion);
            AgregarParametros(comando, inquilino);
            await conexion.OpenAsync();
            var id = await comando.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task ActualizarAsync(Inquilino inquilino)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE inquilino SET nombre = @nombre, apellido = @apellido, dni = @dni, " +
                      "email = @email, telefono = @telefono, activo = @activo WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", inquilino.Id);
            AgregarParametros(comando, inquilino);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        // Baja lógica: el controller debe validar que quien la invoca es administrador.
        public async Task EliminarAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE inquilino SET activo = 0 WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        private static void AgregarParametros(MySqlCommand comando, Inquilino inquilino)
        {
            comando.Parameters.AddWithValue("@nombre", inquilino.Nombre);
            comando.Parameters.AddWithValue("@apellido", inquilino.Apellido);
            comando.Parameters.AddWithValue("@dni", inquilino.Dni);
            comando.Parameters.AddWithValue("@email", (object?)inquilino.Email ?? DBNull.Value);
            comando.Parameters.AddWithValue("@telefono", (object?)inquilino.Telefono ?? DBNull.Value);
            comando.Parameters.AddWithValue("@activo", inquilino.Activo);
        }

        private static Inquilino Mapear(MySqlDataReader lector) => new()
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