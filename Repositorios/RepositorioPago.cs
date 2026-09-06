using MySqlConnector;
using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public class RepositorioPago
    {
        private readonly ConexionBD _conexionBD;

        public RepositorioPago(ConexionBD conexionBD)
        {
            _conexionBD = conexionBD;
        }

        private const string SqlBase =
            "SELECT id, id_reserva, concepto, fecha_pago, importe, anulado, " +
            "creado_por_user_id, anulado_por_user_id FROM pago ";

        // Informe: pagos de una reserva en particular.
        public async Task<List<Pago>> ListarPorReservaAsync(int idReserva)
        {
            var pagos = new List<Pago>();
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = SqlBase + "WHERE id_reserva = @idReserva ORDER BY fecha_pago";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@idReserva", idReserva);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                pagos.Add(Mapear(lector));
            return pagos;
        }

        public async Task<Pago?> ObtenerPorIdAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = SqlBase + "WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? Mapear(lector) : null;
        }

        public async Task<int> CrearAsync(Pago pago)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "INSERT INTO pago (id_reserva, concepto, fecha_pago, importe, anulado, creado_por_user_id) " +
                      "VALUES (@idReserva, @concepto, @fechaPago, @importe, 0, @creadoPor); " +
                      "SELECT LAST_INSERT_ID();";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@idReserva", pago.IdReserva);
            comando.Parameters.AddWithValue("@concepto", pago.Concepto);
            comando.Parameters.AddWithValue("@fechaPago", pago.FechaPago.Date);
            comando.Parameters.AddWithValue("@importe", pago.Importe);
            comando.Parameters.AddWithValue("@creadoPor", pago.CreadoPorUserId);
            await conexion.OpenAsync();
            var id = await comando.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        // La narrativa solo permite editar el concepto: fecha e importe quedan fijos.
        public async Task ActualizarConceptoAsync(int id, string nuevoConcepto)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE pago SET concepto = @concepto WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            comando.Parameters.AddWithValue("@concepto", nuevoConcepto);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        // La eliminación es un cambio de estado: el pago sigue visible, marcado como anulado.
        public async Task AnularAsync(int id, int usuarioId)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE pago SET anulado = 1, anulado_por_user_id = @usuarioId WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            comando.Parameters.AddWithValue("@usuarioId", usuarioId);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        private static Pago Mapear(MySqlDataReader lector) => new()
        {
            Id = lector.GetInt32("id"),
            IdReserva = lector.GetInt32("id_reserva"),
            Concepto = lector.GetString("concepto"),
            FechaPago = lector.GetDateTime("fecha_pago"),
            Importe = lector.GetDecimal("importe"),
            Anulado = lector.GetBoolean("anulado"),
            CreadoPorUserId = lector.GetInt32("creado_por_user_id"),
            AnuladoPorUserId = lector.IsDBNull(lector.GetOrdinal("anulado_por_user_id"))
                ? null : lector.GetInt32("anulado_por_user_id")
        };
    }
}