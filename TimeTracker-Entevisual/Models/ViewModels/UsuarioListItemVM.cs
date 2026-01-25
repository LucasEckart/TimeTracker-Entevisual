namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class UsuarioListItemVM
    {
        public string Id { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Email { get; set; } = "";

        public bool EsAdmin { get; set; }
        public bool EsModerador { get; set; }

        public bool DebeCambiarPassword { get; set; }

        // usando lockout como “activo/inactivo”
        public bool Activo { get; set; }
    }
}
