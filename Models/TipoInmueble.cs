using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class TipoInmueble
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        [StringLength(50, ErrorMessage = "El campo {0} debe tener máximo {1} caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public override string ToString() => Nombre;
    }
}