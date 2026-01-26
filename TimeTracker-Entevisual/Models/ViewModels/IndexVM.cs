using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;



namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class IndexVM
    {
        public string MesActualTexto { get; set; } = "";
        public List<ActividadCardVM> EnEjecucion { get; set; } = new();
        public List<ActividadCardVM> Actividades { get; set; } = new();

        // Para el modal "Nueva actividad"
        public List<SelectListItem> TiposActividad { get; set; } = new();

        // Para abrir el modal de nota después de pausar
        public int? PausaTiempoId { get; set; }
        public int? PausaActividadId { get; set; }

        public int TotalActividades => Actividades.Count + (EnEjecucion is null ? 0 : 1);

        public FiltrosActividadesVM Filtros { get; set; } = new();
        public bool EsGlobal { get; set; }



    }
}
