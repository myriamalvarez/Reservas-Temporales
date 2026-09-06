namespace Reservas_Temporales.Models
{
    public enum RolUsuario
    {
        Administrador,
        Empleado
    }

    public enum EstadoInmueble
    {
        Disponible,
        Suspendido
    }

    public enum EstadoReserva
    {
        Vigente,
        Finalizada,
        FinalizadaAnticipada
    }
}