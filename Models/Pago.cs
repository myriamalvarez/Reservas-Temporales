using System;
using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class Pago
    {
        public int Id { get; set; }

        public int IdReserva { get; set; }
        public Reserva? Reserva { get; set; }

        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        [StringLength(150, ErrorMessage = "El campo {0} debe tener máximo {1} caracteres.")]
        [Display(Name = "Concepto")]
        public string Concepto { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de pago")]
        public DateTime FechaPago { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El campo {0} no puede ser negativo.")]
        [Display(Name = "Importe")]
        public decimal Importe { get; set; }

        public bool Anulado { get; set; }

        public int CreadoPorUserId { get; set; }
        public Usuario? CreadoPor { get; set; }

        public int? AnuladoPorUserId { get; set; }
        public Usuario? AnuladoPor { get; set; }
    }
}