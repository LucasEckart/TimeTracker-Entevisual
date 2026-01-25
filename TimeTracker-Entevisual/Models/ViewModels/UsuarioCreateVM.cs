using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class UsuarioCreateVM
    {
        [Required]
        public string Nombre { get; set; } = "";

        [Required]
        public string Apellido { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";
        [Required]
        public string Rol { get; set; } = "Usuario";

        public List<SelectListItem> RolesDisponibles { get; set; } = new();
    }
}
