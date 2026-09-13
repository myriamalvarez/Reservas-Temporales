using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class Reserva
    {
        public int Id { get; set; }

        [Display(Name = "Inmueble")]
        public int IdInmueble { get; set; }
        public Inmueble? Inmueble { get; set; }

        [Display(Name = "Inquilino")]
        public int IdInquilino { get; set; }
        public Inquilino? Inquilino { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Desde")]
        public DateTime FechaDesde { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Hasta")]
        public DateTime FechaHasta { get; set; }

        // Se conserva aunque haya terminación anticipada, para recalcular la multa.
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de finalización original")]
        public DateTime FechaHastaOriginal { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de terminación")]
        public DateTime? FechaTerminacion { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El campo {0} no puede ser negativo.")]
        [Display(Name = "Monto diario")]
        public decimal MontoDiario { get; set; }

        [Display(Name = "Multa")]
        public decimal? Multa { get; set; }

        public EstadoReserva Estado { get; set; } = EstadoReserva.Vigente;

        public int CreadoPorUserId { get; set; }
        public Usuario? CreadoPor { get; set; }

        public int? TerminadoPorUserId { get; set; }
        public Usuario? TerminadoPor { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Navegación opcional.
        public List<Pago> Pagos { get; set; } = new();
    }
}