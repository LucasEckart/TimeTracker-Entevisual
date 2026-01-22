namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class NotasModalVM
    {
        public int ActividadId { get; set; }
        public string TituloActividad { get; set; } = "";
        public List<NotaItemVM> Items { get; set; } = new();
    }
}
