using System.ComponentModel.DataAnnotations;

namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresá un email válido.")] public string Email { get; set; } = "";

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool Recordarme { get; set; } = false;

        public string? ReturnUrl { get; set; }
    }
}
