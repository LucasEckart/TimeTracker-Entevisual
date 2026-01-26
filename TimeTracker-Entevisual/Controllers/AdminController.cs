using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TimeTracker_Entevisual.Data;
using TimeTracker_Entevisual.Models;
using TimeTracker_Entevisual.Models.ViewModels;
using TimeTracker_Entevisual.Helpers; // <-- donde pondremos el builder

namespace TimeTracker_Entevisual.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly TimeTrackerDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public AdminController(TimeTrackerDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ DASHBOARD GLOBAL
        public async Task<IActionResult> Dashboard(string? q = null, int? tipoId = null, string? orden = "recientes")
        {
            var now = DateTime.Now;
            var inicioMes = new DateTime(now.Year, now.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            var tipos = await _context.TiposActividad
                .AsNoTracking()
                .OrderBy(t => t.Descripcion)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Descripcion })
                .ToListAsync();

            var vm = new IndexVM
            {
                MesActualTexto = now.ToString("MMMM yyyy"),
                TiposActividad = tipos,
                EsGlobal = true
            };

            vm.Filtros.Query = q;
            vm.Filtros.TipoActividadId = tipoId;
            vm.Filtros.Orden = string.IsNullOrWhiteSpace(orden) ? "recientes" : orden;
            vm.Filtros.Tipos = tipos.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text,
                Selected = (tipoId.HasValue && x.Value == tipoId.Value.ToString())
            }).ToList();

            var query = _context.Actividades
                .AsNoTracking()
                .Where(a => !a.Archivado && !a.Eliminado)
                .Include(a => a.TipoActividad)
                .Include(a => a.Usuario)
                .Include(a => a.Tiempos).ThenInclude(t => t.MarcasTiempo)
                .AsQueryable();

            if (tipoId.HasValue)
                query = query.Where(a => a.TipoActividadId == tipoId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(a =>
                    (a.Titulo != null && a.Titulo.ToLower().Contains(term)) ||
                    (a.Descripcion != null && a.Descripcion.ToLower().Contains(term)) ||
                    (a.Codigo != null && a.Codigo.ToLower().Contains(term)) ||
                    (a.TipoActividad != null && a.TipoActividad.Descripcion.ToLower().Contains(term)) ||
                    a.Tiempos.Any(t => t.MarcasTiempo.Any(m => m.Descripcion != null && m.Descripcion.ToLower().Contains(term))) ||
                    (a.Usuario != null && (((a.Usuario.Nombre ?? "") + " " + (a.Usuario.Apellido ?? "")).ToLower().Contains(term)))
                );
            }

            orden = string.IsNullOrWhiteSpace(orden) ? "recientes" : orden;
            query = orden == "az"
                ? query.OrderBy(a => a.Titulo)
                : query.OrderByDescending(a => a.FechaCreacion);

            var actividades = await query.ToListAsync();

            foreach (var a in actividades)
            {
                var card = ActividadCardBuilder.Build(a, now, inicioMes, finMes);

                // ✅ Global puede tener varias corriendo
                if (card.Estado == EstadoActividadVM.Corriendo)
                    vm.EnEjecucion.Add(card);
                else
                    vm.Actividades.Add(card);
            }

            if (orden == "az")
                vm.Actividades = vm.Actividades.OrderBy(x => x.Titulo).ToList();

            return View(vm); // Views/Admin/Dashboard.cshtml
        }

        // ✅ ARCHIVO GLOBAL
        public async Task<IActionResult> Archivo(string? q = null, int? tipoId = null, string? orden = "recientes")
        {
            var tipos = await _context.TiposActividad
                .AsNoTracking()
                .OrderBy(t => t.Descripcion)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Descripcion })
                .ToListAsync();

            var vm = new ArchivoIndexVM
            {
                MesTexto = "Actividades archivadas (global)",
                EsGlobal = true
            };

            vm.Filtros.Query = q;
            vm.Filtros.TipoActividadId = tipoId;
            vm.Filtros.Orden = string.IsNullOrWhiteSpace(orden) ? "recientes" : orden;
            vm.Filtros.Tipos = tipos.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text,
                Selected = (tipoId.HasValue && x.Value == tipoId.Value.ToString())
            }).ToList();

            var query = _context.Actividades
                .AsNoTracking()
                .Where(a => a.Archivado && !a.Eliminado)
                .Include(a => a.Usuario)
                .Include(a => a.TipoActividad)
                .Include(a => a.Tiempos).ThenInclude(t => t.MarcasTiempo)
                .AsQueryable();

            if (tipoId.HasValue)
                query = query.Where(a => a.TipoActividadId == tipoId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(a =>
                    (a.Titulo != null && a.Titulo.ToLower().Contains(term)) ||
                    (a.Descripcion != null && a.Descripcion.ToLower().Contains(term)) ||
                    (a.Codigo != null && a.Codigo.ToLower().Contains(term)) ||
                    (a.TipoActividad != null && a.TipoActividad.Descripcion.ToLower().Contains(term)) ||
                    (a.Usuario != null && (((a.Usuario.Nombre ?? "") + " " + (a.Usuario.Apellido ?? "")).ToLower().Contains(term)))
                );
            }

            orden = string.IsNullOrWhiteSpace(orden) ? "recientes" : orden;
            query = orden == "az"
                ? query.OrderBy(a => a.Titulo)
                : query.OrderByDescending(a => a.FechaCreacion);

            var actividades = await query.ToListAsync();

            foreach (var a in actividades)
            {
                var totalSeg = a.Tiempos
                    .Where(t => t.Fin != null)
                    .Sum(t => t.DuracionSegundos ?? (long)(t.Fin!.Value - t.Inicio).TotalSeconds);

                vm.Actividades.Add(new ActividadCardVM
                {
                    ActividadId = a.Id,
                    TipoActividad = a.TipoActividad?.Descripcion ?? "—",
                    Codigo = a.Codigo,
                    Titulo = a.Titulo,
                    Descripcion = a.Descripcion,
                    Estado = EstadoActividadVM.Archivada,
                    UsuarioNombre = a.Usuario != null ? $"{a.Usuario.Nombre} {a.Usuario.Apellido}" : null,
                    AcumuladoMes = TimeFormatHelper.FormatoHHMMSS(totalSeg),
                    UltimoRegistro = $"Archivada • {a.FechaCreacion:dd/MM/yyyy}"
                });
            }

            return View(vm); 
        }
    }
}
