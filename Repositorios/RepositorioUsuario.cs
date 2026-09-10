using MySqlConnector;
using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public class RepositorioUsuario
    {
        private readonly ConexionBD _conexionBD;

        public RepositorioUsuario(ConexionBD conexionBD)
        {
            _conexionBD = conexionBD;
        }

        private const string ColumnasBase =
            "id, nombre_usuario, nombre, apellido, email, password, avatar, rol, activo, fecha_creacion";

        public async Task<List<Usuario>> ListarAsync(bool soloActivos = true)
        {
            var usuarios = new List<Usuario>();
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = $"SELECT {ColumnasBase} FROM usuario" +
                      (soloActivos ? " WHERE activo = 1" : "") +
                      " ORDER BY apellido, nombre";
            using var comando = new MySqlCommand(sql, conexion);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                usuarios.Add(Mapear(lector));
            return usuarios;
        }

        // Paginado por servidor para el listado con Vue.
        public async Task<(List<Usuario> Items, int Total)> ListarPaginadoAsync(
            int pagina, int tamanioPagina, bool soloActivos = true)
        {
            if (pagina < 1) pagina = 1;
            if (tamanioPagina < 1) tamanioPagina = 10;

            var whereSql = soloActivos ? " WHERE activo = 1" : "";

            using var conexion = _conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            var sqlTotal = "SELECT COUNT(*) FROM usuario" + whereSql;
            using var comandoTotal = new MySqlCommand(sqlTotal, conexion);
            var total = Convert.ToInt32(await comandoTotal.ExecuteScalarAsync());

            var sqlPagina = $"SELECT {ColumnasBase} FROM usuario" + whereSql +
                             " ORDER BY apellido, nombre LIMIT @tamanioPagina OFFSET @offset";
            using var comandoPagina = new MySqlCommand(sqlPagina, conexion);
            comandoPagina.Parameters.AddWithValue("@tamanioPagina", tamanioPagina);
            comandoPagina.Parameters.AddWithValue("@offset", (pagina - 1) * tamanioPagina);

            var items = new List<Usuario>();
            using var lectorPagina = await comandoPagina.ExecuteReaderAsync();
            while (await lectorPagina.ReadAsync())
                items.Add(Mapear(lectorPagina));

            return (items, total);
        }

        public async Task<Usuario?> ObtenerPorIdAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = $"SELECT {ColumnasBase} FROM usuario WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? Mapear(lector) : null;
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = $"SELECT {ColumnasBase} FROM usuario WHERE email = @email";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@email", email);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? Mapear(lector) : null;
        }

        public async Task<int> CrearAsync(Usuario usuario)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "INSERT INTO usuario (nombre_usuario, nombre, apellido, email, password, avatar, rol, activo) " +
                      "VALUES (@nombreUsuario, @nombre, @apellido, @email, @password, @avatar, @rol, @activo); " +
                      "SELECT LAST_INSERT_ID();";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@nombreUsuario", usuario.NombreUsuario);
            comando.Parameters.AddWithValue("@nombre", usuario.Nombre);
            comando.Parameters.AddWithValue("@apellido", usuario.Apellido);
            comando.Parameters.AddWithValue("@email", usuario.Email);
            comando.Parameters.AddWithValue("@password", usuario.Password);
            comando.Parameters.AddWithValue("@avatar", (object?)usuario.Avatar ?? DBNull.Value);
            comando.Parameters.AddWithValue("@rol", ConversionesEnum.ARolTexto(usuario.Rol));
            comando.Parameters.AddWithValue("@activo", usuario.Activo);
            await conexion.OpenAsync();
            var id = await comando.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        // No incluye Password a propósito: el cambio de contraseña se hace con ActualizarPasswordAsync.
        public async Task ActualizarAsync(Usuario usuario)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE usuario SET nombre_usuario = @nombreUsuario, nombre = @nombre, apellido = @apellido, " +
                      "email = @email, avatar = @avatar, rol = @rol, activo = @activo WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", usuario.Id);
            comando.Parameters.AddWithValue("@nombreUsuario", usuario.NombreUsuario);
            comando.Parameters.AddWithValue("@nombre", usuario.Nombre);
            comando.Parameters.AddWithValue("@apellido", usuario.Apellido);
            comando.Parameters.AddWithValue("@email", usuario.Email);
            comando.Parameters.AddWithValue("@avatar", (object?)usuario.Avatar ?? DBNull.Value);
            comando.Parameters.AddWithValue("@rol", ConversionesEnum.ARolTexto(usuario.Rol));
            comando.Parameters.AddWithValue("@activo", usuario.Activo);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        public async Task ActualizarPasswordAsync(int id, string nuevoHashPassword)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE usuario SET password = @password WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            comando.Parameters.AddWithValue("@password", nuevoHashPassword);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        // Baja lógica: el controller debe validar que quien la invoca es administrador.
        public async Task EliminarAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE usuario SET activo = 0 WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        private static Usuario Mapear(MySqlDataReader lector) => new()
        {
            Id = lector.GetInt32("id"),
            NombreUsuario = lector.GetString("nombre_usuario"),
            Nombre = lector.GetString("nombre"),
            Apellido = lector.GetString("apellido"),
            Email = lector.GetString("email"),
            Password = lector.GetString("password"),
            Avatar = lector.IsDBNull(lector.GetOrdinal("avatar")) ? null : lector.GetString("avatar"),
            Rol = ConversionesEnum.ATextoRol(lector.GetString("rol")),
            Activo = lector.GetBoolean("activo"),
            FechaCreacion = lector.GetDateTime("fecha_creacion")
        };
    }
}