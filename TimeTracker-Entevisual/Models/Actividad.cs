using System.ComponentModel.DataAnnotations;

namespace TimeTracker_Entevisual.Models
{
    public class Actividad
    {

        public int Id { get; set; }

        // FK Usuario
        public string UsuarioId { get; set; } = null!;
        public Usuario Usuario { get; set; } = null!;


        // FK TipoActividad
        public int TipoActividadId { get; set; }
        public TipoActividad TipoActividad { get; set; } = null!;

        [MaxLength(30)]
        public string? Codigo { get; set; }

        [Required, MaxLength(140)]
        public string Titulo { get; set; } = null!;

        [MaxLength(400)]
        public string? Descripcion { get; set; }

        public string? Notas { get; set; }

        public bool Archivado { get; set; }

        // Importante: fecha de creación de la actividad (no es inicio de tiempo)
        public DateTime FechaCreacion { get; set; }

        // Sesiones de tiempo (cada iniciar/reanudar crea una nueva)
        public ICollection<Tiempo> Tiempos { get; set; } = new List<Tiempo>();
    }
}
