using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Reservas_Temporales.Seguridad
{
    // Hashing con sal (PBKDF2), en la misma línea que usa el profesor con KeyDerivation.Pbkdf2,
    // pero con Rfc2898DeriveBytes (incluido en .NET, no requiere el paquete de ASP.NET Identity).
    public class PasswordHasher
    {
        private const int Iteraciones = 100_000;
        private const int LargoHashBytes = 32; // 256 bits

        private readonly string _salt;

        public PasswordHasher(IConfiguration configuration)
        {
            _salt = configuration["Salt"]
                ?? throw new InvalidOperationException("Falta configurar \"Salt\" en appsettings.json.");
        }

        public string Hashear(string password)
        {
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password: Encoding.UTF8.GetBytes(password),
                salt: Encoding.UTF8.GetBytes(_salt),
                iterations: Iteraciones,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: LargoHashBytes);

            return Convert.ToBase64String(hash);
        }

        public bool Verificar(string password, string hashAlmacenado) =>
            Hashear(password) == hashAlmacenado;
    }
}