using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class Propietario
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

        // Navegación opcional, útil si armás la lista en el mismo query.
        public List<Inmueble> Inmuebles { get; set; } = new();
    }
}