using System.ComponentModel.DataAnnotations;

namespace TimeTracker_Entevisual.Models
{
    public class MarcaTiempo
    {
        public int Id { get; set; }

        // FK Tiempo (sesión)
        public int TiempoId { get; set; }
        public Tiempo Tiempo { get; set; } = null!;

        // Momento en que se registró la marca (normalmente al pausar)
        public DateTime Fecha { get; set; }

        // Texto libre: qué pasó durante la pausa / hito / contexto
        [Required, MaxLength(300)]
        public string Descripcion { get; set; } = null!;

        public bool Oculta { get; set; } = false;

    }
}
