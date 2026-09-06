using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class Inquilino
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Dni { get; set; } = string.Empty;

        [EmailAddress, StringLength(150)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Telefono { get; set; }

        public bool Activo { get; set; } = true;
    }
}