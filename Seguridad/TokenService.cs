using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Reservas_Temporales.Models;

namespace Reservas_Temporales.Seguridad
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerarToken(Usuario usuario)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["TokenAuthentication:SecretKey"]!));
            var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // ClaimTypes.NameIdentifier guarda el Id: lo usa ControladorBase para saber quién está logueado.
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(ClaimTypes.Name, usuario.Email),
                new("FullName", $"{usuario.Nombre} {usuario.Apellido}"),
                new(ClaimTypes.Role, usuario.Rol.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["TokenAuthentication:Issuer"],
                audience: _configuration["TokenAuthentication:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: credenciales);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}