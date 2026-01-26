namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class ArchivoIndexVM
    {
        public string MesTexto { get; set; } = "";
        public List<ActividadCardVM> Actividades { get; set; } = new();
        public FiltrosActividadesVM Filtros { get; set; } = new();
        public bool EsGlobal { get; set; }


    }
}
