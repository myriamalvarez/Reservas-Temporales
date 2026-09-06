using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Reservas_Temporales.Models
{
    public class Reserva
    {
        public int Id { get; set; }

        public int IdInmueble { get; set; }
        public Inmueble? Inmueble { get; set; }

        public int IdInquilino { get; set; }
        public Inquilino? Inquilino { get; set; }

        [DataType(DataType.Date)]
        public DateTime FechaDesde { get; set; }

        [DataType(DataType.Date)]
        public DateTime FechaHasta { get; set; }

        // Se conserva aunque haya terminación anticipada, para recalcular la multa.
        [DataType(DataType.Date)]
        public DateTime FechaHastaOriginal { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaTerminacion { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MontoDiario { get; set; }

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