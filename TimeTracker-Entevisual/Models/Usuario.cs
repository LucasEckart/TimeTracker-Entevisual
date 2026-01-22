using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TimeTracker_Entevisual.Models
{
    public class Usuario : IdentityUser
    {


        [Required, MaxLength(80)]
        public string Nombre { get; set; } = null!;

        [Required, MaxLength(80)]
        public string Apellido { get; set; } = null!;

        [Required, MaxLength(160)]

        public ICollection<Actividad> Actividades { get; set; } = new List<Actividad>();
    }
}
