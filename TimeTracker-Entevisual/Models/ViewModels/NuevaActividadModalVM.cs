using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class NuevaActividadModalVM
    {
        public int TipoActividadId { get; set; }
        public string? Codigo { get; set; }
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        public string? Notas { get; set; }

        // Para el dropdown
        public List<SelectListItem> TiposActividad { get; set; } = new();
    }
}
