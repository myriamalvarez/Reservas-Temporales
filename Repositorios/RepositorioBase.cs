using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Reservas_Temporales.Repositorios
{
    // Clase base de la que heredan todos los repositorios: centraliza la lectura del
    // connection string, para no repetirla en cada uno.
    public abstract class RepositorioBase
    {
        protected readonly string connectionString;

        protected RepositorioBase(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("ReservasTemporales")
                ?? throw new InvalidOperationException(
                    "No se encontró la cadena de conexión 'ReservasTemporales' en appsettings.json.");
        }

        // Evita repetir "new MySqlConnection(connectionString)" en cada método de cada repositorio.
        protected MySqlConnection ObtenerConexion() => new MySqlConnection(connectionString);
    }
}