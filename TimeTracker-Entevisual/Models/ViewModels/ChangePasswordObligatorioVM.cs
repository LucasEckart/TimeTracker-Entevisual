using System.ComponentModel.DataAnnotations;

namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class ChangePasswordObligatorioVM
    {
        [Required, DataType(DataType.Password)]
        public string PasswordActual { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string NuevaPassword { get; set; } = "";

        [Required, DataType(DataType.Password), Compare(nameof(NuevaPassword))]
        public string ConfirmarNuevaPassword { get; set; } = "";
    }
}
