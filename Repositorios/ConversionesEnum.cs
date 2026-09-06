using Reservas_Temporales.Models;

namespace Reservas_Temporales.Repositorios
{
    // Los ENUM de MySQL se guardan en minúscula y "finalizada_anticipada" usa
    // guion bajo, por eso no alcanza con un Enum.Parse directo.
    public static class ConversionesEnum
    {
        public static string ARolTexto(RolUsuario rol) =>
            rol == RolUsuario.Administrador ? "administrador" : "empleado";

        public static RolUsuario ATextoRol(string texto) =>
            texto == "administrador" ? RolUsuario.Administrador : RolUsuario.Empleado;

        public static string AEstadoInmuebleTexto(EstadoInmueble estado) =>
            estado == EstadoInmueble.Disponible ? "disponible" : "suspendido";

        public static EstadoInmueble ATextoEstadoInmueble(string texto) =>
            texto == "disponible" ? EstadoInmueble.Disponible : EstadoInmueble.Suspendido;

        public static string AEstadoReservaTexto(EstadoReserva estado) => estado switch
        {
            EstadoReserva.Vigente => "vigente",
            EstadoReserva.Finalizada => "finalizada",
            EstadoReserva.FinalizadaAnticipada => "finalizada_anticipada",
            _ => throw new ArgumentOutOfRangeException(nameof(estado))
        };

        public static EstadoReserva ATextoEstadoReserva(string texto) => texto switch
        {
            "vigente" => EstadoReserva.Vigente,
            "finalizada" => EstadoReserva.Finalizada,
            "finalizada_anticipada" => EstadoReserva.FinalizadaAnticipada,
            _ => throw new ArgumentOutOfRangeException(nameof(texto))
        };
    }
}