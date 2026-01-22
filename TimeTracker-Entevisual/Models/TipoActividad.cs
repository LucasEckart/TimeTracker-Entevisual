using System.ComponentModel.DataAnnotations;

namespace TimeTracker_Entevisual.Models
{
    public class TipoActividad
    {
        public int Id { get; set; }

        [Required, MaxLength(60)]
        public string Descripcion { get; set; } = null!;

        public ICollection<Actividad> Actividades { get; set; } = new List<Actividad>();
    }
}
