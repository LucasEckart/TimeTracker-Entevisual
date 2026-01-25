using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace TimeTracker_Entevisual.Models.ViewModels
{
    public class NotaItemVM
    {
        public int TiempoId { get; set; }
        public DateTime Fin { get; set; }
        public string DuracionTexto { get; set; } = "";
        [StringLength(300)]
        public string? Texto { get; set; }
        public int? MarcaTiempoId { get; set; }

    }
}
