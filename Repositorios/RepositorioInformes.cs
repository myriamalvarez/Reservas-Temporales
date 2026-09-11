using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Reservas_Temporales.ViewModels;

namespace Reservas_Temporales.Repositorios
{
    // Consultas de agregación para los informes de la narrativa. Separado de RepositorioInmueble
    // porque no son operaciones CRUD sobre una entidad sino reportes que combinan varias tablas.
    public class RepositorioInformes : RepositorioBase
    {
        public RepositorioInformes(IConfiguration configuration) : base(configuration)
        {
        }

        // Informe: inmuebles más reservados en los últimos N días (default 365).
        // Incluye los que tuvieron 0 reservas en el período, para dar el ranking completo.
        public async Task<(List<InmuebleConteoReservas> Items, int Total)> ListarMasReservadosAsync(
            int pagina, int tamanioPagina, int dias = 365)
        {
            if (pagina < 1) pagina = 1;
            if (tamanioPagina < 1) tamanioPagina = 10;

            using var conexion = ObtenerConexion();
            await conexion.OpenAsync();

            var sqlTotal = "SELECT COUNT(*) FROM inmueble WHERE activo = 1";
            using var comandoTotal = new MySqlCommand(sqlTotal, conexion);
            var total = Convert.ToInt32(await comandoTotal.ExecuteScalarAsync());

            var sqlPagina =
                "SELECT i.id, i.direccion, p.nombre AS propietario_nombre, p.apellido AS propietario_apellido, " +
                "COUNT(r.id) AS cantidad_reservas " +
                "FROM inmueble i " +
                "INNER JOIN propietario p ON p.id = i.id_propietario " +
                "LEFT JOIN reserva r ON r.id_inmueble = i.id AND r.activo = 1 " +
                "AND r.fecha_desde >= DATE_SUB(CURDATE(), INTERVAL @dias DAY) " +
                "WHERE i.activo = 1 " +
                "GROUP BY i.id, i.direccion, p.nombre, p.apellido " +
                "ORDER BY cantidad_reservas DESC, i.direccion " +
                "LIMIT @tamanioPagina OFFSET @offset";

            using var comandoPagina = new MySqlCommand(sqlPagina, conexion);
            comandoPagina.Parameters.AddWithValue("@dias", dias);
            comandoPagina.Parameters.AddWithValue("@tamanioPagina", tamanioPagina);
            comandoPagina.Parameters.AddWithValue("@offset", (pagina - 1) * tamanioPagina);

            var items = new List<InmuebleConteoReservas>();
            using var lector = await comandoPagina.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                items.Add(new InmuebleConteoReservas
                {
                    Id = lector.GetInt32("id"),
                    Direccion = lector.GetString("direccion"),
                    PropietarioNombre = $"{lector.GetString("propietario_nombre")} {lector.GetString("propietario_apellido")}",
                    CantidadReservas = lector.GetInt32("cantidad_reservas")
                });
            }

            return (items, total);
        }

        // Informe: inmuebles sin reservas en los últimos N días (incluye los que nunca tuvieron ninguna).
        public async Task<(List<InmuebleSinReservas> Items, int Total)> ListarSinReservasAsync(
            int pagina, int tamanioPagina, int dias = 30)
        {
            if (pagina < 1) pagina = 1;
            if (tamanioPagina < 1) tamanioPagina = 10;

            using var conexion = ObtenerConexion();
            await conexion.OpenAsync();

            const string subconsulta =
                "SELECT i.id, MAX(r.fecha_desde) AS ultima " +
                "FROM inmueble i " +
                "LEFT JOIN reserva r ON r.id_inmueble = i.id AND r.activo = 1 " +
                "WHERE i.activo = 1 " +
                "GROUP BY i.id " +
                "HAVING ultima IS NULL OR ultima < DATE_SUB(CURDATE(), INTERVAL @dias DAY)";

            var sqlTotal = $"SELECT COUNT(*) FROM ({subconsulta}) AS sub";
            using var comandoTotal = new MySqlCommand(sqlTotal, conexion);
            comandoTotal.Parameters.AddWithValue("@dias", dias);
            var total = Convert.ToInt32(await comandoTotal.ExecuteScalarAsync());

            var sqlPagina =
                "SELECT i.id, i.direccion, p.nombre AS propietario_nombre, p.apellido AS propietario_apellido, " +
                "MAX(r.fecha_desde) AS ultima_reserva " +
                "FROM inmueble i " +
                "INNER JOIN propietario p ON p.id = i.id_propietario " +
                "LEFT JOIN reserva r ON r.id_inmueble = i.id AND r.activo = 1 " +
                "WHERE i.activo = 1 " +
                "GROUP BY i.id, i.direccion, p.nombre, p.apellido " +
                "HAVING ultima_reserva IS NULL OR ultima_reserva < DATE_SUB(CURDATE(), INTERVAL @dias DAY) " +
                // MySQL no permite envolver un alias de función de agregación (MAX) en otra
                // expresión dentro de ORDER BY (ni de HAVING en algunos casos). Alcanza con el
                // alias "pelado": los NULL ya se ordenan primero por defecto en un ASC.
                "ORDER BY ultima_reserva ASC " +
                "LIMIT @tamanioPagina OFFSET @offset";

            using var comandoPagina = new MySqlCommand(sqlPagina, conexion);
            comandoPagina.Parameters.AddWithValue("@dias", dias);
            comandoPagina.Parameters.AddWithValue("@tamanioPagina", tamanioPagina);
            comandoPagina.Parameters.AddWithValue("@offset", (pagina - 1) * tamanioPagina);

            var items = new List<InmuebleSinReservas>();
            using var lector = await comandoPagina.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                items.Add(new InmuebleSinReservas
                {
                    Id = lector.GetInt32("id"),
                    Direccion = lector.GetString("direccion"),
                    PropietarioNombre = $"{lector.GetString("propietario_nombre")} {lector.GetString("propietario_apellido")}",
                    UltimaReserva = lector.IsDBNull(lector.GetOrdinal("ultima_reserva"))
                        ? null : lector.GetDateTime("ultima_reserva")
                });
            }

            return (items, total);
        }
    }
}