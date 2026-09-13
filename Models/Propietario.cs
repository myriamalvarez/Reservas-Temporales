using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class Propietario
    {
        public int Id { get; set; }

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
        [StringLength(20, ErrorMessage = "El campo {0} debe tener máximo {1} caracteres.")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "El campo {0} solo puede contener números.")]
        [Display(Name = "DNI")]
        public string Dni { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "El campo {0} no es un correo electrónico válido.")]
        [StringLength(150, ErrorMessage = "El campo {0} debe tener máximo {1} caracteres.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(30, ErrorMessage = "El campo {0} debe tener máximo {1} caracteres.")]
        [RegularExpression(@"^[0-9\-\+\s\(\)]+$", ErrorMessage = "El campo {0} solo puede contener números.")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        public bool Activo { get; set; } = true;

        // Navegación opcional, útil si necesitamos armar la lista en el mismo query.
        public List<Inmueble> Inmuebles { get; set; } = new();

        public override string ToString() => $"{Nombre} {Apellido}";
    }
}