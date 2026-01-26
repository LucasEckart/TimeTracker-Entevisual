using System.ComponentModel.DataAnnotations;

namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class ChangePasswordObligatorioVM
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        [DataType(DataType.Password)]
        public string PasswordActual { get; set; } = "";

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string NuevaPassword { get; set; } = "";

        [Required(ErrorMessage = "Debe confirmar la nueva contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NuevaPassword), ErrorMessage = "La confirmación no coincide con la nueva contraseña.")]
        public string ConfirmarNuevaPassword { get; set; } = "";
    }
}
