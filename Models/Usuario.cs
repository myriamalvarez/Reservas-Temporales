using System;
using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        [StringLength(50, ErrorMessage = "El campo {0} debe tener máximo {1} caracteres.")]
        [Display(Name = "Nombre de usuario")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        [StringLength(100, ErrorMessage = "El campo {0} debe tener máximo {1} caracteres.")]
        [RegularExpression(@"^[a-zA-ZÀ-ÖØ-öø-ÿ\s]+$", ErrorMessage = "El campo {0} solo puede contener letras.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        [StringLength(100, ErrorMessage = "El campo {0} debe tener máximo {1} caracteres.")]
        [RegularExpression(@"^[a-zA-ZÀ-ÖØ-öø-ÿ\s]+$", ErrorMessage = "El campo {0} solo puede contener letras.")]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        [EmailAddress(ErrorMessage = "El campo {0} no es un correo electrónico válido.")]
        [StringLength(150, ErrorMessage = "El campo {0} debe tener máximo {1} caracteres.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // Almacenar siempre el hash, nunca la contraseña en texto plano.
        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        [StringLength(255, ErrorMessage = "El campo {0} debe tener máximo {1} caracteres.")]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Avatar")]
        public string? Avatar { get; set; }

        [Display(Name = "Rol")]
        public RolUsuario Rol { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public override string ToString() => $"{Nombre} {Apellido}";
    }
}