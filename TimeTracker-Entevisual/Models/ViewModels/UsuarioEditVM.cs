using System.ComponentModel.DataAnnotations;

namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class UsuarioEditVM
    {
        public string Id { get; set; } = "";

        [Required]
        public string Nombre { get; set; } = "";

        [Required]
        public string Apellido { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        public bool Activo { get; set; }
    }
}
