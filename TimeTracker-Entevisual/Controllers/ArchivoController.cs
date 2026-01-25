using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TimeTracker_Entevisual.Data;
using TimeTracker_Entevisual.Helpers;
using TimeTracker_Entevisual.Models;
using TimeTracker_Entevisual.Models.ViewModels;

namespace TimeTracker_Entevisual.Controllers
{
    [Authorize]
    public class ArchivoController : Controller
    {
        private readonly TimeTrackerDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public ArchivoController(TimeTrackerDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? q = null, int? tipoId = null, string? orden = "recientes")
        {
            var usuarioId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(usuarioId))
                return RedirectToAction("Login", "Account");

            var esAdmin = User.IsInRole("Admin");

            var tipos = await _context.TiposActividad
                .AsNoTracking()
                .OrderBy(t => t.Descripcion)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.Descripcion
                })
                .ToListAsync();

            var vm = new ArchivoIndexVM
            {
                MesTexto = "Actividades archivadas"
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

            // ✅ Query base (ARCHIVO = Archivadas)
            var query = _context.Actividades
                .AsNoTracking()
                .Where(a => a.Archivado && !a.Eliminado)   // ✅ FIX ACÁ
                .Include(a => a.Usuario)
                .Include(a => a.TipoActividad)
                .Include(a => a.Tiempos)
                    .ThenInclude(t => t.MarcasTiempo)
                .AsQueryable();

            // ✅ Usuario normal: solo las propias
            if (!esAdmin)
                query = query.Where(a => a.UsuarioId == usuarioId);

            if (tipoId.HasValue)
                query = query.Where(a => a.TipoActividadId == tipoId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();

                query = query.Where(a =>
                    (a.Titulo != null && a.Titulo.ToLower().Contains(term)) ||
                    (a.Descripcion != null && a.Descripcion.ToLower().Contains(term)) ||
                    (a.Codigo != null && a.Codigo.ToLower().Contains(term)) ||
                    (a.TipoActividad != null && a.TipoActividad.Descripcion.ToLower().Contains(term))
                // (opcional) si querés también buscar en notas del archivo:
                // || a.Tiempos.Any(t => t.MarcasTiempo.Any(m => m.Descripcion != null && m.Descripcion.ToLower().Contains(term)))
                // || (a.Usuario != null && (((a.Usuario.Nombre ?? "") + " " + (a.Usuario.Apellido ?? "")).ToLower().Contains(term)))
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
                    .Sum(t => t.DuracionSegundos ??
                        (long)(t.Fin!.Value - t.Inicio).TotalSeconds);

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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desarchivar(int actividadId)
        {
            var usuarioId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(usuarioId))
                return RedirectToAction("Login", "Account");

            var esAdmin = User.IsInRole("Admin");

            var actividad = await _context.Actividades
                .FirstOrDefaultAsync(a => a.Id == actividadId);

            if (actividad == null)
                return NotFound();

            // ✅ Usuario normal: solo si es suya
            if (!esAdmin && actividad.UsuarioId != usuarioId)
                return Forbid();

            actividad.Archivado = false;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarDefinitivo(int actividadId)
        {
            var usuarioId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(usuarioId))
                return RedirectToAction("Login", "Account");

            var esAdmin = User.IsInRole("Admin");

            var act = await _context.Actividades.FirstOrDefaultAsync(a => a.Id == actividadId);
            if (act == null) return NotFound();

            // ✅ Usuario normal: solo si es suya
            if (!esAdmin && act.UsuarioId != usuarioId)
                return Forbid();

            act.Eliminado = true;
            act.FechaEliminado = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
