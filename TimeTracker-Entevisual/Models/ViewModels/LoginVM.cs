using System.ComponentModel.DataAnnotations;

namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class LoginVM
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool Recordarme { get; set; } = false;

        public string? ReturnUrl { get; set; }
    }
}
