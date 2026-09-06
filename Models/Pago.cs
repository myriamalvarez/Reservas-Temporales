using System;
using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class Pago
    {
        public int Id { get; set; }

        public int IdReserva { get; set; }
        public Reserva? Reserva { get; set; }

        [Required, StringLength(150)]
        public string Concepto { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime FechaPago { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Importe { get; set; }

        public bool Anulado { get; set; }

        public int CreadoPorUserId { get; set; }
        public Usuario? CreadoPor { get; set; }

        public int? AnuladoPorUserId { get; set; }
        public Usuario? AnuladoPor { get; set; }
    }
}