using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class TipoInmueble
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}