using Microsoft.AspNetCore.Mvc.Rendering;

namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class FiltrosActividadesVM
    {
        public string? Query { get; set; }
        public int? TipoActividadId { get; set; }
        public string Orden { get; set; } = "recientes"; // "recientes" | "az"

        public List<SelectListItem> Tipos { get; set; } = new();
    }
}
