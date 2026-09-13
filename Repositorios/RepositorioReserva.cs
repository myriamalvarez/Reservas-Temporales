using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Reservas_Temporales.Models;
using Reservas_Temporales.ViewModels;

namespace Reservas_Temporales.Repositorios
{
    public class RepositorioReserva : RepositorioBase, IRepositorioReserva
    {
        public RepositorioReserva(IConfiguration configuration) : base(configuration)
        {
        }

        private const string SqlBase =
            "SELECT id, id_inmueble, id_inquilino, fecha_desde, fecha_hasta, fecha_hasta_original, " +
            "fecha_terminacion, monto_diario, multa, estado, creado_por_user_id, terminado_por_user_id, " +
            "activo, fecha_creacion FROM reserva ";

        // Para listados: incluye dirección del inmueble y nombre del inquilino, más livianos que el modelo completo.
        private const string SqlListado =
            "SELECT r.id, r.fecha_desde, r.fecha_hasta, r.monto_diario, r.estado, " +
            "i.direccion AS inmueble_direccion, " +
            "CONCAT(iq.apellido, ', ', iq.nombre) AS inquilino_nombre " +
            "FROM reserva r " +
            "INNER JOIN inmueble i ON i.id = r.id_inmueble " +
            "INNER JOIN inquilino iq ON iq.id = r.id_inquilino ";

        // Trae también quién creó y quién terminó la reserva (auditoría, solo visible para
        // administradores en la vista de detalle, según la narrativa).
        public async Task<Reserva?> ObtenerPorIdAsync(int id)
        {
            using var conexion = ObtenerConexion();
            var sql =
                "SELECT r.id, r.id_inmueble, r.id_inquilino, r.fecha_desde, r.fecha_hasta, " +
                "r.fecha_hasta_original, r.fecha_terminacion, r.monto_diario, r.multa, r.estado, " +
                "r.creado_por_user_id, r.terminado_por_user_id, r.activo, r.fecha_creacion, " +
                "uc.nombre_usuario AS creador_usuario, uc.nombre AS creador_nombre, uc.apellido AS creador_apellido, " +
                "ut.nombre_usuario AS terminador_usuario, ut.nombre AS terminador_nombre, ut.apellido AS terminador_apellido " +
                "FROM reserva r " +
                "INNER JOIN usuario uc ON uc.id = r.creado_por_user_id " +
                "LEFT JOIN usuario ut ON ut.id = r.terminado_por_user_id " +
                "WHERE r.id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? MapearConAuditoria(lector) : null;
        }

        // Informe: reservas vigentes (por fecha desde/hasta, no solo por el campo estado).
        public async Task<List<Reserva>> ListarVigentesAsync()
        {
            var reservas = new List<Reserva>();
            using var conexion = ObtenerConexion();
            var sql = SqlBase + "WHERE activo = 1 AND estado = 'vigente' " +
                      "AND CURDATE() BETWEEN fecha_desde AND fecha_hasta ORDER BY fecha_hasta";
            using var comando = new MySqlCommand(sql, conexion);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                reservas.Add(Mapear(lector));
            return reservas;
        }

        // Igual que ListarVigentesAsync pero paginado por servidor, para el listado con Vue.
        public async Task<(List<ReservaListadoItem> Items, int Total)> ListarVigentesPaginadoAsync(
            int pagina, int tamanioPagina)
        {
            if (pagina < 1) pagina = 1;
            if (tamanioPagina < 1) tamanioPagina = 10;

            const string condicion =
                "r.activo = 1 AND r.estado = 'vigente' AND CURDATE() BETWEEN r.fecha_desde AND r.fecha_hasta";

            using var conexion = ObtenerConexion();
            await conexion.OpenAsync();

            var sqlTotal = "SELECT COUNT(*) FROM reserva r WHERE " + condicion;
            using var comandoTotal = new MySqlCommand(sqlTotal, conexion);
            var total = Convert.ToInt32(await comandoTotal.ExecuteScalarAsync());

            var sqlPagina = SqlListado + "WHERE " + condicion +
                             " ORDER BY r.fecha_hasta LIMIT @tamanioPagina OFFSET @offset";
            using var comandoPagina = new MySqlCommand(sqlPagina, conexion);
            comandoPagina.Parameters.AddWithValue("@tamanioPagina", tamanioPagina);
            comandoPagina.Parameters.AddWithValue("@offset", (pagina - 1) * tamanioPagina);

            var items = new List<ReservaListadoItem>();
            using var lectorPagina = await comandoPagina.ExecuteReaderAsync();
            while (await lectorPagina.ReadAsync())
            {
                items.Add(new ReservaListadoItem
                {
                    Id = lectorPagina.GetInt32("id"),
                    FechaDesde = lectorPagina.GetDateTime("fecha_desde"),
                    FechaHasta = lectorPagina.GetDateTime("fecha_hasta"),
                    MontoDiario = lectorPagina.GetDecimal("monto_diario"),
                    Estado = ConversionesEnum.ATextoEstadoReserva(lectorPagina.GetString("estado")),
                    InmuebleDireccion = lectorPagina.GetString("inmueble_direccion"),
                    InquilinoNombre = lectorPagina.GetString("inquilino_nombre")
                });
            }

            return (items, total);
        }

        // Listado general: TODAS las reservas (cualquier estado), a diferencia de
        // ListarVigentesPaginadoAsync. Es la forma de llegar a una reserva ya finalizada
        // o finalizada anticipadamente sin tener que conocer/adivinar su Id.
        public async Task<(List<ReservaListadoItem> Items, int Total)> ListarTodasPaginadoAsync(
            int pagina, int tamanioPagina, EstadoReserva? estado = null)
        {
            if (pagina < 1) pagina = 1;
            if (tamanioPagina < 1) tamanioPagina = 10;

            var condiciones = new List<string> { "r.activo = 1" };
            if (estado.HasValue) condiciones.Add("r.estado = @estado");
            var whereSql = "WHERE " + string.Join(" AND ", condiciones);

            using var conexion = ObtenerConexion();
            await conexion.OpenAsync();

            void AgregarFiltros(MySqlCommand comandoAAgregar)
            {
                if (estado.HasValue)
                    comandoAAgregar.Parameters.AddWithValue("@estado", ConversionesEnum.AEstadoReservaTexto(estado.Value));
            }

            var sqlTotal = "SELECT COUNT(*) FROM reserva r " + whereSql;
            using var comandoTotal = new MySqlCommand(sqlTotal, conexion);
            AgregarFiltros(comandoTotal);
            var total = Convert.ToInt32(await comandoTotal.ExecuteScalarAsync());

            var sqlPagina = SqlListado + whereSql +
                             " ORDER BY r.fecha_desde DESC LIMIT @tamanioPagina OFFSET @offset";
            using var comandoPagina = new MySqlCommand(sqlPagina, conexion);
            AgregarFiltros(comandoPagina);
            comandoPagina.Parameters.AddWithValue("@tamanioPagina", tamanioPagina);
            comandoPagina.Parameters.AddWithValue("@offset", (pagina - 1) * tamanioPagina);

            var items = new List<ReservaListadoItem>();
            using var lectorPagina = await comandoPagina.ExecuteReaderAsync();
            while (await lectorPagina.ReadAsync())
            {
                items.Add(new ReservaListadoItem
                {
                    Id = lectorPagina.GetInt32("id"),
                    FechaDesde = lectorPagina.GetDateTime("fecha_desde"),
                    FechaHasta = lectorPagina.GetDateTime("fecha_hasta"),
                    MontoDiario = lectorPagina.GetDecimal("monto_diario"),
                    Estado = ConversionesEnum.ATextoEstadoReserva(lectorPagina.GetString("estado")),
                    InmuebleDireccion = lectorPagina.GetString("inmueble_direccion"),
                    InquilinoNombre = lectorPagina.GetString("inquilino_nombre")
                });
            }

            return (items, total);
        }

        // Informe: reservas que terminan dentro de los próximos X días.
        public async Task<List<Reserva>> ListarQueTerminanEnXDiasAsync(int dias)
        {
            var reservas = new List<Reserva>();
            using var conexion = ObtenerConexion();
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
            using var conexion = ObtenerConexion();
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
            using var conexion = ObtenerConexion();
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
            using var conexion = ObtenerConexion();
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
            using var conexion = ObtenerConexion();
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
            using var conexion = ObtenerConexion();
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
            using var conexion = ObtenerConexion();
            var sql = "UPDATE reserva SET estado = 'finalizada' WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        // Baja lógica: el controller debe validar que quien la invoca es administrador.
        public async Task EliminarAsync(int id)
        {
            using var conexion = ObtenerConexion();
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

        // Igual que Mapear, pero además arma los objetos Usuario livianos de auditoría
        // (CreadoPor / TerminadoPor) a partir de los JOIN de ObtenerPorIdAsync.
        private static Reserva MapearConAuditoria(MySqlDataReader lector)
        {
            var reserva = Mapear(lector);

            reserva.CreadoPor = new Usuario
            {
                Id = reserva.CreadoPorUserId,
                NombreUsuario = lector.GetString("creador_usuario"),
                Nombre = lector.GetString("creador_nombre"),
                Apellido = lector.GetString("creador_apellido")
            };

            if (reserva.TerminadoPorUserId.HasValue && !lector.IsDBNull(lector.GetOrdinal("terminador_usuario")))
            {
                reserva.TerminadoPor = new Usuario
                {
                    Id = reserva.TerminadoPorUserId.Value,
                    NombreUsuario = lector.GetString("terminador_usuario"),
                    Nombre = lector.GetString("terminador_nombre"),
                    Apellido = lector.GetString("terminador_apellido")
                };
            }

            return reserva;
        }
    }
}