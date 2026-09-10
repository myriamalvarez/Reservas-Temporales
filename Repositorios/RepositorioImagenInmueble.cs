using MySqlConnector;
using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public class RepositorioImagenInmueble : IRepositorioImagenInmueble
    {
        private readonly ConexionBD _conexionBD;

        public RepositorioImagenInmueble(ConexionBD conexionBD)
        {
            _conexionBD = conexionBD;
        }

        public async Task<List<ImagenInmueble>> ListarPorInmuebleAsync(int idInmueble)
        {
            var imagenes = new List<ImagenInmueble>();
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "SELECT id, id_inmueble, url, es_portada FROM imagen_inmueble " +
                      "WHERE id_inmueble = @idInmueble ORDER BY es_portada DESC, id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@idInmueble", idInmueble);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                imagenes.Add(Mapear(lector));
            return imagenes;
        }

        public async Task<ImagenInmueble?> ObtenerPorIdAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "SELECT id, id_inmueble, url, es_portada FROM imagen_inmueble WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? Mapear(lector) : null;
        }

        public async Task<int> CrearAsync(ImagenInmueble imagen)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            // Si la nueva imagen es portada, primero se desmarca cualquier otra portada del mismo inmueble.
            if (imagen.EsPortada)
                await DesmarcarPortadaAsync(conexion, imagen.IdInmueble);

            var sql = "INSERT INTO imagen_inmueble (id_inmueble, url, es_portada) " +
                      "VALUES (@idInmueble, @url, @esPortada); SELECT LAST_INSERT_ID();";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@idInmueble", imagen.IdInmueble);
            comando.Parameters.AddWithValue("@url", imagen.Url);
            comando.Parameters.AddWithValue("@esPortada", imagen.EsPortada);
            var id = await comando.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task MarcarComoPortadaAsync(int id, int idInmueble)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            await conexion.OpenAsync();
            await DesmarcarPortadaAsync(conexion, idInmueble);

            var sql = "UPDATE imagen_inmueble SET es_portada = 1 WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await comando.ExecuteNonQueryAsync();
        }

        public async Task EliminarAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "DELETE FROM imagen_inmueble WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        private static async Task DesmarcarPortadaAsync(MySqlConnection conexionAbierta, int idInmueble)
        {
            var sql = "UPDATE imagen_inmueble SET es_portada = 0 WHERE id_inmueble = @idInmueble";
            using var comando = new MySqlCommand(sql, conexionAbierta);
            comando.Parameters.AddWithValue("@idInmueble", idInmueble);
            await comando.ExecuteNonQueryAsync();
        }

        private static ImagenInmueble Mapear(MySqlDataReader lector) => new()
        {
            Id = lector.GetInt32("id"),
            IdInmueble = lector.GetInt32("id_inmueble"),
            Url = lector.GetString("url"),
            EsPortada = lector.GetBoolean("es_portada")
        };
    }
}