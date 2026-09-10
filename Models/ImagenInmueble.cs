using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class ImagenInmueble
    {
        public int Id { get; set; }

        public int IdInmueble { get; set; }

        [Required, StringLength(255)]
        public string Url { get; set; } = string.Empty;

        public bool EsPortada { get; set; }
    }
}