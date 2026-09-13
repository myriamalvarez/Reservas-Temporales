using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class Inmueble
    {
        public int Id { get; set; }

        [Display(Name = "Propietario")]
        public int IdPropietario { get; set; }
        public Propietario? Propietario { get; set; }

        [Display(Name = "Tipo")]
        public int IdTipo { get; set; }
        public TipoInmueble? Tipo { get; set; }

        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        [StringLength(200, ErrorMessage = "El campo {0} debe tener máximo {1} caracteres.")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "El campo {0} debe ser mayor a 0.")]
        [Display(Name = "Cupo")]
        public int Cupo { get; set; }

        [StringLength(100, ErrorMessage = "El campo {0} debe tener máximo {1} caracteres.")]
        [Display(Name = "Coordenadas")]
        public string? Coord { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El campo {0} no puede ser negativo.")]
        [Display(Name = "Precio por día")]
        public decimal PrecioDia { get; set; }

        [Range(0, 100, ErrorMessage = "El campo {0} debe estar entre {1} y {2}.")]
        [Display(Name = "Porcentaje de seña")]
        public decimal PorcentajeSena { get; set; }

        public EstadoInmueble Estado { get; set; } = EstadoInmueble.Disponible;

        public bool Activo { get; set; } = true;

        // Navegación opcional.
        public List<ImagenInmueble> Imagenes { get; set; } = new();

        public override string ToString() => Direccion;
    }
}