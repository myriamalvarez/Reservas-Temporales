using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Reservas_Temporales.Repositorios
{
    public class ConexionBD
    {
        private readonly string _connectionString;

        public ConexionBD(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ReservasTemporales")
                ?? throw new InvalidOperationException(
                    "No se encontró la cadena de conexión 'ReservasTemporales' en appsettings.json.");
        }

        public MySqlConnection ObtenerConexion()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}