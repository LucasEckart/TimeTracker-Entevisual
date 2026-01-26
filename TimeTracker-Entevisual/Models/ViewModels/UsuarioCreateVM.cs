using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class UsuarioCreateVM
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        public string Apellido { get; set; } = "";

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un email válido.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "El rol es obligatorio.")]
        public string Rol { get; set; } = "Usuario";

        public List<SelectListItem> RolesDisponibles { get; set; } = new();
    }
}
