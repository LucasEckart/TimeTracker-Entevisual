namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class ActividadCardVM
    {
        public int ActividadId { get; set; }
        public DateTime? TiempoActualInicio { get; set; }  // solo si está corriendo

        public string TipoActividad { get; set; } = "";
        public string? Codigo { get; set; }

        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }

        public EstadoActividadVM Estado { get; set; }

        // Totales para mostrar (ya formateados)
        public string AcumuladoMes { get; set; } = "00:00:00";
        public string? TiempoActual { get; set; } // solo si Estado == Corriendo

        // Reglas de textos en cards:
        // - Sin iniciar: "Creada · dd/MM/yyyy"
        // - Pausada:     "Sesión — 40 min · Hace 2 días" o "Nota — “...”"
        public string UltimoRegistro { get; set; } = "";

        // Solo para pausadas cuando hay nota + última sesión:
        // "Última sesión: 40 min · Hace 2 días"
        public string? UltimaSesionDetalle { get; set; }

        // Útil para Sin iniciar
        public string? FechaCreacionTexto { get; set; }
        public string? UsuarioNombre { get; set; }  // "Sol Pérez"

    }
}
