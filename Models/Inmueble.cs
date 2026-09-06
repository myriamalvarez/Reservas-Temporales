using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class Inmueble
    {
        public int Id { get; set; }

        public int IdPropietario { get; set; }
        public Propietario? Propietario { get; set; }

        public int IdTipo { get; set; }
        public TipoInmueble? Tipo { get; set; }

        [Required, StringLength(200)]
        public string Direccion { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "El cupo debe ser mayor a 0.")]
        public int Cupo { get; set; }

        [StringLength(100)]
        public string? Coord { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PrecioDia { get; set; }

        [Range(0, 100)]
        public decimal PorcentajeSena { get; set; }

        public EstadoInmueble Estado { get; set; } = EstadoInmueble.Disponible;

        public bool Activo { get; set; } = true;

        // Navegación opcional.
        public List<ImagenInmueble> Imagenes { get; set; } = new();
    }
}