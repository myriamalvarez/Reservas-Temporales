using MySqlConnector;
using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    public class RepositorioInmueble : IRepositorioInmueble
    {
        private readonly ConexionBD _conexionBD;

        public RepositorioInmueble(ConexionBD conexionBD)
        {
            _conexionBD = conexionBD;
        }

        // Trae también nombre del propietario y del tipo, útil para listados (informe: inmuebles + dueño).
        private const string SqlBase =
            "SELECT i.id, i.id_propietario, i.id_tipo, i.direccion, i.cupo, i.coord, i.precio_dia, " +
            "i.porcentaje_sena, i.estado, i.activo, " +
            "p.nombre AS propietario_nombre, p.apellido AS propietario_apellido, " +
            "t.nombre AS tipo_nombre " +
            "FROM inmueble i " +
            "INNER JOIN propietario p ON p.id = i.id_propietario " +
            "INNER JOIN tipo_inmueble t ON t.id = i.id_tipo ";

        public async Task<List<Inmueble>> ListarAsync(
            bool soloActivos = true, EstadoInmueble? estado = null, int? idPropietario = null)
        {
            var inmuebles = new List<Inmueble>();
            using var conexion = _conexionBD.ObtenerConexion();
            var condiciones = new List<string>();
            if (soloActivos) condiciones.Add("i.activo = 1");
            if (estado.HasValue) condiciones.Add("i.estado = @estado");
            if (idPropietario.HasValue) condiciones.Add("i.id_propietario = @idPropietario");

            var sql = SqlBase +
                      (condiciones.Count > 0 ? " WHERE " + string.Join(" AND ", condiciones) : "") +
                      " ORDER BY i.direccion";

            using var comando = new MySqlCommand(sql, conexion);
            if (estado.HasValue)
                comando.Parameters.AddWithValue("@estado", ConversionesEnum.AEstadoInmuebleTexto(estado.Value));
            if (idPropietario.HasValue)
                comando.Parameters.AddWithValue("@idPropietario", idPropietario.Value);

            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                inmuebles.Add(Mapear(lector));
            return inmuebles;
        }

        // Paginado por servidor (requisito del proyecto): trae solo una página de resultados
        // más el total de registros, para que el cliente arme los controles de paginación.
        public async Task<(List<Inmueble> Items, int Total)> ListarPaginadoAsync(
            int pagina, int tamanioPagina,
            bool soloActivos = true, EstadoInmueble? estado = null, int? idPropietario = null)
        {
            if (pagina < 1) pagina = 1;
            if (tamanioPagina < 1) tamanioPagina = 10;

            var condiciones = new List<string>();
            if (soloActivos) condiciones.Add("i.activo = 1");
            if (estado.HasValue) condiciones.Add("i.estado = @estado");
            if (idPropietario.HasValue) condiciones.Add("i.id_propietario = @idPropietario");
            var whereSql = condiciones.Count > 0 ? " WHERE " + string.Join(" AND ", condiciones) : "";

            using var conexion = _conexionBD.ObtenerConexion();
            await conexion.OpenAsync();

            void AgregarFiltros(MySqlCommand comandoAAgregar)
            {
                if (estado.HasValue)
                    comandoAAgregar.Parameters.AddWithValue("@estado", ConversionesEnum.AEstadoInmuebleTexto(estado.Value));
                if (idPropietario.HasValue)
                    comandoAAgregar.Parameters.AddWithValue("@idPropietario", idPropietario.Value);
            }

            var sqlTotal = "SELECT COUNT(*) FROM inmueble i " + whereSql;
            using var comandoTotal = new MySqlCommand(sqlTotal, conexion);
            AgregarFiltros(comandoTotal);
            var total = Convert.ToInt32(await comandoTotal.ExecuteScalarAsync());

            var sqlPagina = SqlBase + whereSql + " ORDER BY i.direccion LIMIT @tamanioPagina OFFSET @offset";
            using var comandoPagina = new MySqlCommand(sqlPagina, conexion);
            AgregarFiltros(comandoPagina);
            comandoPagina.Parameters.AddWithValue("@tamanioPagina", tamanioPagina);
            comandoPagina.Parameters.AddWithValue("@offset", (pagina - 1) * tamanioPagina);

            var items = new List<Inmueble>();
            using var lectorPagina = await comandoPagina.ExecuteReaderAsync();
            while (await lectorPagina.ReadAsync())
                items.Add(Mapear(lectorPagina));

            return (items, total);
        }

        public async Task<Inmueble?> ObtenerPorIdAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = SqlBase + " WHERE i.id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? Mapear(lector) : null;
        }

        // Búsqueda de inmuebles disponibles en un rango de fechas (narrativa: alta de reserva).
        // Excluye inmuebles con reservas activas cuyo rango se solape con el pedido.
        public async Task<List<Inmueble>> BuscarDisponiblesAsync(
            DateTime fechaDesde, DateTime fechaHasta, int? cupoMinimo = null, int? idTipo = null)
        {
            var inmuebles = new List<Inmueble>();
            using var conexion = _conexionBD.ObtenerConexion();

            var condiciones = new List<string>
            {
                "i.activo = 1",
                "i.estado = 'disponible'",
                "NOT EXISTS (SELECT 1 FROM reserva r WHERE r.id_inmueble = i.id AND r.activo = 1 " +
                "AND r.estado <> 'finalizada_anticipada' " +
                "AND r.fecha_desde <= @fechaHasta AND r.fecha_hasta >= @fechaDesde)"
            };
            if (cupoMinimo.HasValue) condiciones.Add("i.cupo >= @cupoMinimo");
            if (idTipo.HasValue) condiciones.Add("i.id_tipo = @idTipo");

            var sql = SqlBase + " WHERE " + string.Join(" AND ", condiciones) + " ORDER BY i.precio_dia";

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@fechaDesde", fechaDesde.Date);
            comando.Parameters.AddWithValue("@fechaHasta", fechaHasta.Date);
            if (cupoMinimo.HasValue) comando.Parameters.AddWithValue("@cupoMinimo", cupoMinimo.Value);
            if (idTipo.HasValue) comando.Parameters.AddWithValue("@idTipo", idTipo.Value);

            await conexion.OpenAsync();
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                inmuebles.Add(Mapear(lector));
            return inmuebles;
        }

        public async Task<int> CrearAsync(Inmueble inmueble)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "INSERT INTO inmueble (id_propietario, id_tipo, direccion, cupo, coord, precio_dia, " +
                      "porcentaje_sena, estado, activo) " +
                      "VALUES (@idPropietario, @idTipo, @direccion, @cupo, @coord, @precioDia, " +
                      "@porcentajeSena, @estado, @activo); " +
                      "SELECT LAST_INSERT_ID();";
            using var comando = new MySqlCommand(sql, conexion);
            AgregarParametros(comando, inmueble);
            await conexion.OpenAsync();
            var id = await comando.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task ActualizarAsync(Inmueble inmueble)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE inmueble SET id_propietario = @idPropietario, id_tipo = @idTipo, " +
                      "direccion = @direccion, cupo = @cupo, coord = @coord, precio_dia = @precioDia, " +
                      "porcentaje_sena = @porcentajeSena, estado = @estado, activo = @activo WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", inmueble.Id);
            AgregarParametros(comando, inmueble);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        // El propietario suspende/reactiva la oferta sin afectar reservas ya creadas.
        public async Task CambiarEstadoAsync(int id, EstadoInmueble estado)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE inmueble SET estado = @estado WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            comando.Parameters.AddWithValue("@estado", ConversionesEnum.AEstadoInmuebleTexto(estado));
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        // Baja lógica: el controller debe validar que quien la invoca es administrador.
        public async Task EliminarAsync(int id)
        {
            using var conexion = _conexionBD.ObtenerConexion();
            var sql = "UPDATE inmueble SET activo = 0 WHERE id = @id";
            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }

        private static void AgregarParametros(MySqlCommand comando, Inmueble inmueble)
        {
            comando.Parameters.AddWithValue("@idPropietario", inmueble.IdPropietario);
            comando.Parameters.AddWithValue("@idTipo", inmueble.IdTipo);
            comando.Parameters.AddWithValue("@direccion", inmueble.Direccion);
            comando.Parameters.AddWithValue("@cupo", inmueble.Cupo);
            comando.Parameters.AddWithValue("@coord", (object?)inmueble.Coord ?? DBNull.Value);
            comando.Parameters.AddWithValue("@precioDia", inmueble.PrecioDia);
            comando.Parameters.AddWithValue("@porcentajeSena", inmueble.PorcentajeSena);
            comando.Parameters.AddWithValue("@estado", ConversionesEnum.AEstadoInmuebleTexto(inmueble.Estado));
            comando.Parameters.AddWithValue("@activo", inmueble.Activo);
        }

        private static Inmueble Mapear(MySqlDataReader lector) => new()
        {
            Id = lector.GetInt32("id"),
            IdPropietario = lector.GetInt32("id_propietario"),
            IdTipo = lector.GetInt32("id_tipo"),
            Direccion = lector.GetString("direccion"),
            Cupo = lector.GetInt32("cupo"),
            Coord = lector.IsDBNull(lector.GetOrdinal("coord")) ? null : lector.GetString("coord"),
            PrecioDia = lector.GetDecimal("precio_dia"),
            PorcentajeSena = lector.GetDecimal("porcentaje_sena"),
            Estado = ConversionesEnum.ATextoEstadoInmueble(lector.GetString("estado")),
            Activo = lector.GetBoolean("activo"),
            Propietario = new Propietario
            {
                Id = lector.GetInt32("id_propietario"),
                Nombre = lector.GetString("propietario_nombre"),
                Apellido = lector.GetString("propietario_apellido")
            },
            Tipo = new TipoInmueble
            {
                Id = lector.GetInt32("id_tipo"),
                Nombre = lector.GetString("tipo_nombre")
            }
        };
    }
}