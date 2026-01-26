using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TimeTracker_Entevisual.Data;
using TimeTracker_Entevisual.Helpers;
using TimeTracker_Entevisual.Models;
using TimeTracker_Entevisual.Models.ViewModels;

namespace TimeTracker_Entevisual.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly TimeTrackerDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public HomeController(TimeTrackerDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            int? pausaTiempoId = null,
            int? pausaActividadId = null,
            string? q = null,
            int? tipoId = null,
            string? orden = "recientes"
        )
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.DebeCambiarPassword == true)
                return RedirectToAction("ChangePasswordObligatorio", "Account");

            var usuarioId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(usuarioId))
                return RedirectToAction("Login", "Account");

            var now = DateTime.Now;
            var inicioMes = new DateTime(now.Year, now.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            // ---- Dropdown tipos ----
            var tipos = await _context.TiposActividad
                .AsNoTracking()
                .OrderBy(t => t.Descripcion)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.Descripcion
                })
                .ToListAsync();

            var vm = new IndexVM
            {
                MesActualTexto = now.ToString("MMMM yyyy"),
                PausaTiempoId = pausaTiempoId,
                PausaActividadId = pausaActividadId,
                TiposActividad = tipos
            };

            // ---- Filtros ----
            vm.Filtros.Query = q;
            vm.Filtros.TipoActividadId = tipoId;
            vm.Filtros.Orden = string.IsNullOrWhiteSpace(orden) ? "recientes" : orden;

            vm.Filtros.Tipos = tipos.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text,
                Selected = (tipoId.HasValue && x.Value == tipoId.Value.ToString())
            }).ToList();

            // ---- Modal pausa ----
            if (pausaTiempoId.HasValue)
            {
                var t = await _context.Tiempos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == pausaTiempoId.Value);

                if (t?.Fin != null)
                {
                    var durSeg = t.DuracionSegundos
                        ?? (long)(t.Fin.Value - t.Inicio).TotalSeconds;

                    ViewBag.PausaDuracionTexto =
                        TimeFormatHelper.FormatoHHMMSS(durSeg);
                }
            }

            // ---- Query base (SIEMPRE por usuario) ----
            var query = _context.Actividades
                .AsNoTracking()
                .Where(a =>
                    !a.Archivado &&
                    !a.Eliminado &&
                    a.UsuarioId == usuarioId
                )
                .Include(a => a.TipoActividad)
                .Include(a => a.Usuario)
                .Include(a => a.Tiempos)
                    .ThenInclude(t => t.MarcasTiempo)
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
                    a.Tiempos.Any(t =>
                        t.MarcasTiempo.Any(m =>
                            m.Descripcion != null &&
                            m.Descripcion.ToLower().Contains(term)
                        )
                    )
                );
            }

            orden = string.IsNullOrWhiteSpace(orden) ? "recientes" : orden;

            query = orden == "az"
                ? query.OrderBy(a => a.Titulo)
                : query.OrderByDescending(a => a.FechaCreacion);

            var actividades = await query.ToListAsync();

            // ---- Cards ----
            foreach (var a in actividades)
            {
                var card = ActividadCardBuilder.Build(a, now, inicioMes, finMes);

                if (card.Estado == EstadoActividadVM.Corriendo)
                    vm.EnEjecucion.Add(card);
                else
                    vm.Actividades.Add(card);
            }

            if (orden == "az")
                vm.Actividades = vm.Actividades.OrderBy(x => x.Titulo).ToList();

            return View(vm);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
