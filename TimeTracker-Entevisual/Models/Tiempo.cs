namespace TimeTracker_Entevisual.Models
{
    public class Tiempo
    {

        public int Id { get; set; }

        // FK Actividad
        public int ActividadId { get; set; }
        public Actividad Actividad { get; set; } = null!;

        // Inicio y fin de la sesión (Fin null = está corriendo)
        public DateTime Inicio { get; set; }
        public DateTime? Fin { get; set; }

        // Duración del tramo de trabajo medido (se calcula al cerrar)
        public int? DuracionSegundos { get; set; }

        // Marcas (notas opcionales al pausar, o hitos manuales)
        public ICollection<MarcaTiempo> MarcasTiempo { get; set; } = new List<MarcaTiempo>();

        public bool OcultoEnNotas { get; set; } = false;

    }
}
