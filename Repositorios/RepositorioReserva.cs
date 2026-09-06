using MySqlConnector;
using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public class RepositorioReserva
    {
        private readonly ConexionBD _conexionBD;

        public RepositorioReserva(ConexionBD conexionBD)
        {
            _conexionBD = conexionBD;
        }

        private const string SqlBase =
            "SELECT id, id_inmueble, id_inquilino, fecha_desde, fecha_hasta, fecha_hasta_original, " +
            "fecha_terminacion, monto_diario, multa, estado, creado_por_user_id, terminado_por_user_id, " +
            "activo, fecha_creacion FROM reserva ";

        public async Task<Reserva?> ObtenerPorIdAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = SqlBase + "WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? Mapear(lector) : null;
        }

        // Informe: reservas vigentes (por fecha desde/hasta, no solo por el campo estado).
        public async Task<List<Reserva>> ListarVigentesAsync()
        {
            var reservas = new List<Reserva>();
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = SqlBase + "WHERE activo = 1 AND estado = 'vigente' " +
                      "AND CURDATE() BETWEEN fecha_desde AND fecha_hasta ORDER BY fecha_hasta";
            using var comando = new MySqlCommand(sql, conexion);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                reservas.Add(Mapear(lector));
            return reservas;
        }

        // Informe: reservas que terminan dentro de los próximos X días.
        public async Task<List<Reserva>> ListarQueTerminanEnXDiasAsync(int dias)
        {
            var reservas = new List<Reserva>();
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = SqlBase + "WHERE activo = 1 AND estado = 'vigente' " +
                      "AND fecha_hasta BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL @dias DAY) " +
                      "ORDER BY fecha_hasta";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@dias", dias);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                reservas.Add(Mapear(lector));
            return reservas;
        }

        public async Task<List<Reserva>> ListarPorInmuebleAsync(int idInmueble)
        {
            var reservas = new List<Reserva>();
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = SqlBase + "WHERE id_inmueble = @idInmueble AND activo = 1 ORDER BY fecha_desde DESC";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@idInmueble", idInmueble);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                reservas.Add(Mapear(lector));
            return reservas;
        }

        public async Task<List<Reserva>> ListarPorInquilinoAsync(int idInquilino)
        {
            var reservas = new List<Reserva>();
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = SqlBase + "WHERE id_inquilino = @idInquilino AND activo = 1 ORDER BY fecha_desde DESC";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@idInquilino", idInquilino);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                reservas.Add(Mapear(lector));
            return reservas;
        }

        // Validación obligatoria antes de crear: que el inmueble siga libre en esas fechas.
        // idReservaExcluir se usa al editar/renovar, para no chocar contra la propia reserva.
        public async Task<bool> ExisteSolapamientoAsync(
            int idInmueble, DateTime fechaDesde, DateTime fechaHasta, int? idReservaExcluir = null)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "SELECT COUNT(*) FROM reserva WHERE id_inmueble = @idInmueble AND activo = 1 " +
                      "AND estado <> 'finalizada_anticipada' " +
                      "AND fecha_desde <= @fechaHasta AND fecha_hasta >= @fechaDesde" +
                      (idReservaExcluir.HasValue ? " AND id <> @idReservaExcluir" : "");
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@idInmueble", idInmueble);
            comando.Parameters.AddWithValue("@fechaDesde", fechaDesde.Date);
            comando.Parameters.AddWithValue("@fechaHasta", fechaHasta.Date);
            if (idReservaExcluir.HasValue)
                comando.Parameters.AddWithValue("@idReservaExcluir", idReservaExcluir.Value);
            await conexion.OpenAsync();
            var cantidad = Convert.ToInt32(await comando.ExecuteScalarAsync());
            return cantidad > 0;
        }

        public async Task<int> CrearAsync(Reserva reserva)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "INSERT INTO reserva (id_inmueble, id_inquilino, fecha_desde, fecha_hasta, " +
                      "fecha_hasta_original, monto_diario, estado, creado_por_user_id, activo) " +
                      "VALUES (@idInmueble, @idInquilino, @fechaDesde, @fechaHasta, @fechaHastaOriginal, " +
                      "@montoDiario, @estado, @creadoPor, @activo); SELECT LAST_INSERT_ID();";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@idInmueble", reserva.IdInmueble);
            comando.Parameters.AddWithValue("@idInquilino", reserva.IdInquilino);
            comando.Parameters.AddWithValue("@fechaDesde", reserva.FechaDesde.Date);
            comando.Parameters.AddWithValue("@fechaHasta", reserva.FechaHasta.Date);
            comando.Parameters.AddWithValue("@fechaHastaOriginal", reserva.FechaHasta.Date);
            comando.Parameters.AddWithValue("@montoDiario", reserva.MontoDiario);
            comando.Parameters.AddWithValue("@estado", ConversionesEnum.AEstadoReservaTexto(EstadoReserva.Vigente));
            comando.Parameters.AddWithValue("@creadoPor", reserva.CreadoPorUserId);
            comando.Parameters.AddWithValue("@activo", true);
            await conexion.OpenAsync();
            var id = await comando.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        // Terminación anticipada: registra fecha efectiva y multa, sin tocar fecha_hasta_original.
        public async Task TerminarAnticipadamenteAsync(
            int id, DateTime fechaTerminacion, decimal multa, int usuarioId)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE reserva SET fecha_hasta = @fechaTerminacion, fecha_terminacion = @fechaTerminacion, " +
                      "multa = @multa, estado = 'finalizada_anticipada', terminado_por_user_id = @usuarioId " +
                      "WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            comando.Parameters.AddWithValue("@fechaTerminacion", fechaTerminacion.Date);
            comando.Parameters.AddWithValue("@multa", multa);
            comando.Parameters.AddWithValue("@usuarioId", usuarioId);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        // Marca una reserva como finalizada en término (job/proceso batch, o al consultarla vencida).
        public async Task FinalizarAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE reserva SET estado = 'finalizada' WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        // Baja lógica: el controller debe validar que quien la invoca es administrador.
        public async Task EliminarAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE reserva SET activo = 0 WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        private static Reserva Mapear(MySqlDataReader lector) => new()
        {
            Id = lector.GetInt32("id"),
            IdInmueble = lector.GetInt32("id_inmueble"),
            IdInquilino = lector.GetInt32("id_inquilino"),
            FechaDesde = lector.GetDateTime("fecha_desde"),
            FechaHasta = lector.GetDateTime("fecha_hasta"),
            FechaHastaOriginal = lector.GetDateTime("fecha_hasta_original"),
            FechaTerminacion = lector.IsDBNull(lector.GetOrdinal("fecha_terminacion"))
                ? null : lector.GetDateTime("fecha_terminacion"),
            MontoDiario = lector.GetDecimal("monto_diario"),
            Multa = lector.IsDBNull(lector.GetOrdinal("multa")) ? null : lector.GetDecimal("multa"),
            Estado = ConversionesEnum.ATextoEstadoReserva(lector.GetString("estado")),
            CreadoPorUserId = lector.GetInt32("creado_por_user_id"),
            TerminadoPorUserId = lector.IsDBNull(lector.GetOrdinal("terminado_por_user_id"))
                ? null : lector.GetInt32("terminado_por_user_id"),
            Activo = lector.GetBoolean("activo"),
            FechaCreacion = lector.GetDateTime("fecha_creacion")
        };
    }
}