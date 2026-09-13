using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Reservas_Temporales.Models;
using Reservas_Temporales.ViewModels;

namespace Reservas_Temporales.Repositorios
{
    public class RepositorioPago : RepositorioBase, IRepositorioPago
    {
        public RepositorioPago(IConfiguration configuration) : base(configuration)
        {
        }

        private const string SqlBase =
            "SELECT id, id_reserva, concepto, fecha_pago, importe, anulado, " +
            "creado_por_user_id, anulado_por_user_id FROM pago ";

        // Informe: pagos de una reserva en particular.
        // Trae también quién creó y quién anuló cada pago (auditoría, solo visible para
        // administradores en la vista de detalle, según la narrativa).
        public async Task<List<Pago>> ListarPorReservaAsync(int idReserva)
        {
            var pagos = new List<Pago>();
            using var conexion = ObtenerConexion();
            var sql =
                "SELECT p.id, p.id_reserva, p.concepto, p.fecha_pago, p.importe, p.anulado, " +
                "p.creado_por_user_id, p.anulado_por_user_id, " +
                "uc.nombre_usuario AS creador_usuario, uc.nombre AS creador_nombre, uc.apellido AS creador_apellido, " +
                "ua.nombre_usuario AS anulador_usuario, ua.nombre AS anulador_nombre, ua.apellido AS anulador_apellido " +
                "FROM pago p " +
                "INNER JOIN usuario uc ON uc.id = p.creado_por_user_id " +
                "LEFT JOIN usuario ua ON ua.id = p.anulado_por_user_id " +
                "WHERE p.id_reserva = @idReserva ORDER BY p.fecha_pago";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@idReserva", idReserva);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                pagos.Add(MapearConAuditoria(lector));
            return pagos;
        }

        public async Task<Pago?> ObtenerPorIdAsync(int id)
        {
            using var conexion = ObtenerConexion();
            var sql = SqlBase + "WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? Mapear(lector) : null;
        }

        public async Task<int> CrearAsync(Pago pago)
        {
            using var conexion = ObtenerConexion();
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
            using var conexion = ObtenerConexion();
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
            using var conexion = ObtenerConexion();
            var sql = "UPDATE pago SET anulado = 1, anulado_por_user_id = @usuarioId WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            comando.Parameters.AddWithValue("@usuarioId", usuarioId);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        // Listado general: TODOS los pagos, de cualquier reserva, con contexto (inmueble/inquilino)
        // y filtro opcional por anulado. Es la forma de encontrar un pago sin tener que
        // conocer primero a qué reserva pertenece.
        public async Task<(List<PagoListadoItem> Items, int Total)> ListarTodosPaginadoAsync(
            int pagina, int tamanioPagina, bool? anulado = null)
        {
            if (pagina < 1) pagina = 1;
            if (tamanioPagina < 1) tamanioPagina = 10;

            var condiciones = new List<string>();
            if (anulado.HasValue) condiciones.Add("p.anulado = @anulado");
            var whereSql = condiciones.Count > 0 ? " WHERE " + string.Join(" AND ", condiciones) : "";

            using var conexion = ObtenerConexion();
            await conexion.OpenAsync();

            var sqlTotal = "SELECT COUNT(*) FROM pago p" + whereSql;
            using var comandoTotal = new MySqlCommand(sqlTotal, conexion);
            if (anulado.HasValue) comandoTotal.Parameters.AddWithValue("@anulado", anulado.Value);
            var total = Convert.ToInt32(await comandoTotal.ExecuteScalarAsync());

            var sqlPagina =
                "SELECT p.id, p.id_reserva, p.concepto, p.fecha_pago, p.importe, p.anulado, " +
                "i.direccion AS inmueble_direccion, " +
                "CONCAT(iq.apellido, ', ', iq.nombre) AS inquilino_nombre " +
                "FROM pago p " +
                "INNER JOIN reserva r ON r.id = p.id_reserva " +
                "INNER JOIN inmueble i ON i.id = r.id_inmueble " +
                "INNER JOIN inquilino iq ON iq.id = r.id_inquilino" +
                whereSql +
                " ORDER BY p.fecha_pago DESC LIMIT @tamanioPagina OFFSET @offset";
            using var comandoPagina = new MySqlCommand(sqlPagina, conexion);
            if (anulado.HasValue) comandoPagina.Parameters.AddWithValue("@anulado", anulado.Value);
            comandoPagina.Parameters.AddWithValue("@tamanioPagina", tamanioPagina);
            comandoPagina.Parameters.AddWithValue("@offset", (pagina - 1) * tamanioPagina);

            var items = new List<PagoListadoItem>();
            using var lectorPagina = await comandoPagina.ExecuteReaderAsync();
            while (await lectorPagina.ReadAsync())
            {
                items.Add(new PagoListadoItem
                {
                    Id = lectorPagina.GetInt32("id"),
                    IdReserva = lectorPagina.GetInt32("id_reserva"),
                    Concepto = lectorPagina.GetString("concepto"),
                    FechaPago = lectorPagina.GetDateTime("fecha_pago"),
                    Importe = lectorPagina.GetDecimal("importe"),
                    Anulado = lectorPagina.GetBoolean("anulado"),
                    InmuebleDireccion = lectorPagina.GetString("inmueble_direccion"),
                    InquilinoNombre = lectorPagina.GetString("inquilino_nombre")
                });
            }

            return (items, total);
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

        // Igual que Mapear, pero además arma los objetos Usuario livianos de auditoría
        // (CreadoPor / AnuladoPor) a partir de los JOIN de ListarPorReservaAsync.
        private static Pago MapearConAuditoria(MySqlDataReader lector)
        {
            var pago = Mapear(lector);

            pago.CreadoPor = new Usuario
            {
                Id = pago.CreadoPorUserId,
                NombreUsuario = lector.GetString("creador_usuario"),
                Nombre = lector.GetString("creador_nombre"),
                Apellido = lector.GetString("creador_apellido")
            };

            if (pago.AnuladoPorUserId.HasValue && !lector.IsDBNull(lector.GetOrdinal("anulador_usuario")))
            {
                pago.AnuladoPor = new Usuario
                {
                    Id = pago.AnuladoPorUserId.Value,
                    NombreUsuario = lector.GetString("anulador_usuario"),
                    Nombre = lector.GetString("anulador_nombre"),
                    Apellido = lector.GetString("anulador_apellido")
                };
            }

            return pago;
        }
    }
}