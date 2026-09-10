using System.Security.Cryptography;
using System.Text;

namespace Reservas_Temporales.Seguridad
{
    // Hashing simple para el proyecto académico (sin salt). Para un caso real,
    // reemplazar por BCrypt.Net-Next u otro algoritmo pensado para contraseñas.
    public static class PasswordHasher
    {
        public static string Hashear(string password) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

        public static bool Verificar(string password, string hashAlmacenado) =>
            Hashear(password) == hashAlmacenado;
    }
}