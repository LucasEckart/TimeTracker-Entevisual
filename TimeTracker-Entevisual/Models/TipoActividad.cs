using System.ComponentModel.DataAnnotations;

namespace TimeTracker_Entevisual.Models
{
    public class TipoActividad
    {
        public int Id { get; set; }

        [MaxLength(60)]
        [Required(ErrorMessage = "Campo obligatorio.")]

        public string Descripcion { get; set; } = null!;

        public ICollection<Actividad> Actividades { get; set; } = new List<Actividad>();
    }
}
