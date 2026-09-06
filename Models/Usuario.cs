using System;
using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        // Almacenar siempre el hash, nunca la contraseña en texto plano.
        [Required, StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Avatar { get; set; }

        public RolUsuario Rol { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}