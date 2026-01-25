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

            // ? Usuario logueado
            var usuarioId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(usuarioId))
                return RedirectToAction("Login", "Account");

            var esAdmin = User.IsInRole("Admin");

            var now = DateTime.Now;
            var inicioMes = new DateTime(now.Year, now.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            // ---- Dropdown tipos (modal + filtros) ----
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

            // ---- Filtros UI ----
            vm.Filtros.Query = q;
            vm.Filtros.TipoActividadId = tipoId;
            vm.Filtros.Orden = string.IsNullOrWhiteSpace(orden) ? "recientes" : orden;

            vm.Filtros.Tipos = tipos.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text,
                Selected = (tipoId.HasValue && x.Value == tipoId.Value.ToString())
            }).ToList();

            // ---- Para mostrar "Período registrado" en modal pausa ----
            if (pausaTiempoId.HasValue)
            {
                var t = await _context.Tiempos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == pausaTiempoId.Value);

                if (t?.Fin != null)
                {
                    var durSeg = t.DuracionSegundos ?? (long)(t.Fin.Value - t.Inicio).TotalSeconds;
                    ViewBag.PausaDuracionTexto = FormatoDuracionCorta(durSeg);
                }
                else
                {
                    ViewBag.PausaDuracionTexto = null;
                }
            }

            // ---- Query base ----
            var query = _context.Actividades
                .AsNoTracking()
                .Where(a => !a.Archivado && !a.Eliminado)
                .Include(a => a.TipoActividad)
                .Include(a => a.Usuario)
                .Include(a => a.Tiempos)
                    .ThenInclude(t => t.MarcasTiempo)
                .AsQueryable();

            // ? Si NO es admin, solo ve las propias
            if (!esAdmin)
                query = query.Where(a => a.UsuarioId == usuarioId);

            // ---- Filtro por tipo ----
            if (tipoId.HasValue)
                query = query.Where(a => a.TipoActividadId == tipoId.Value);

            // ---- Búsqueda ----
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();

                query = query.Where(a =>
                    (a.Titulo != null && a.Titulo.ToLower().Contains(term)) ||
                    (a.Descripcion != null && a.Descripcion.ToLower().Contains(term)) ||
                    (a.Codigo != null && a.Codigo.ToLower().Contains(term)) ||
                    (a.TipoActividad != null && a.TipoActividad.Descripcion.ToLower().Contains(term)) ||
                    a.Tiempos.Any(t => t.MarcasTiempo.Any(m => m.Descripcion != null && m.Descripcion.ToLower().Contains(term))) ||

                    // ? NUEVO: buscar por usuario (admin)
                    (a.Usuario != null &&
                        (
                            (a.Usuario.Nombre != null && a.Usuario.Nombre.ToLower().Contains(term)) ||
                            (a.Usuario.Apellido != null && a.Usuario.Apellido.ToLower().Contains(term)) ||
                            ((a.Usuario.Nombre + " " + a.Usuario.Apellido).ToLower().Contains(term))
                        )
                    )
                );

            }

            // ---- Orden ----
            orden = string.IsNullOrWhiteSpace(orden) ? "recientes" : orden;

            query = orden == "az"
                ? query.OrderBy(a => a.Titulo)
                : query.OrderByDescending(a => a.FechaCreacion);

            var actividades = await query.ToListAsync();

            // ---- Cards ----
            foreach (var a in actividades)
            {

                var card = BuildCardVM(a, now, inicioMes, finMes);
                if (card.Estado == EstadoActividadVM.Corriendo)
                    vm.EnEjecucion.Add(card);
                else
                    vm.Actividades.Add(card);

            }

            if (orden == "az")
                vm.Actividades = vm.Actividades.OrderBy(x => x.Titulo).ToList();

            return View(vm);
        }

        private static ActividadCardVM BuildCardVM(Actividad a, DateTime now, DateTime inicioMes, DateTime finMes)
        {
            var abierto = a.Tiempos.FirstOrDefault(t => t.Fin == null);
            var tieneSesiones = a.Tiempos.Any();

            var estado = GetEstado(abierto, tieneSesiones);

            var acumSeg = CalcularAcumuladoMesSegundos(a, now, inicioMes, finMes);

            var ultimaSesionCerrada = a.Tiempos
                .Where(t => t.Fin != null)
                .OrderByDescending(t => t.Fin)
                .FirstOrDefault();


            var ultimaSesionDetalle = BuildUltimaSesionDetalle(ultimaSesionCerrada, now, out var ultimaSesionDurSeg);

            var ultimoRegistro = BuildUltimoRegistro(a, tieneSesiones, ultimaSesionCerrada, ultimaSesionDurSeg, now);

            return new ActividadCardVM
            {
                ActividadId = a.Id,
                TipoActividad = a.TipoActividad?.Descripcion ?? "—",
                Codigo = a.Codigo,
                Titulo = a.Titulo,
                Descripcion = a.Descripcion,
                Estado = estado,
                UsuarioNombre = a.Usuario != null
                    ? $"{a.Usuario.Nombre} {a.Usuario.Apellido}"
                    : null,

                // helpers
                AcumuladoMes = TimeFormatHelper.FormatoHHMMSS(acumSeg),

                TiempoActualInicio = (abierto != null) ? abierto.Inicio : null,
                UltimoRegistro = ultimoRegistro,
                UltimaSesionDetalle = (estado == EstadoActividadVM.Pausada) ? ultimaSesionDetalle : null
            };

        }

        private static EstadoActividadVM GetEstado(Tiempo? abierto, bool tieneSesiones)
        {
            if (abierto != null) return EstadoActividadVM.Corriendo;
            if (tieneSesiones) return EstadoActividadVM.Pausada;
            return EstadoActividadVM.SinIniciar;
        }

        private static long CalcularAcumuladoMesSegundos(Actividad a, DateTime now, DateTime inicioMes, DateTime finMes)
        {
            long acumSeg = 0;

            var tiemposDelMes = a.Tiempos.Where(t => t.Inicio >= inicioMes && t.Inicio < finMes);

            foreach (var t in tiemposDelMes)
            {
                if (t.Fin != null && t.DuracionSegundos != null)
                    acumSeg += Convert.ToInt64(t.DuracionSegundos);
                else if (t.Fin != null)
                    acumSeg += (long)(t.Fin.Value - t.Inicio).TotalSeconds;
                else
                    acumSeg += (long)(now - t.Inicio).TotalSeconds;
            }

            return acumSeg;
        }

        private static string? BuildUltimaSesionDetalle(Tiempo? ultimaSesionCerrada, DateTime now, out long? durSeg)
        {
            durSeg = null;

            if (ultimaSesionCerrada?.Fin == null)
                return null;

            durSeg = ultimaSesionCerrada.DuracionSegundos != null
                ? Convert.ToInt64(ultimaSesionCerrada.DuracionSegundos)
                : (long)(ultimaSesionCerrada.Fin.Value - ultimaSesionCerrada.Inicio).TotalSeconds;

            return $"Última sesión: {FormatoDuracionCorta(durSeg.Value)} • {HaceCuanto(ultimaSesionCerrada.Fin.Value, now)}";
        }

        private static string BuildUltimoRegistro(Actividad a, bool tieneSesiones, Tiempo? ultimaSesionCerrada, long? ultimaSesionDurSeg, DateTime now)
        {
            if (!tieneSesiones)
                return $"Último registro: Creada • {a.FechaCreacion:dd/MM/yyyy}";

            var ultimaMarca = a.Tiempos
                .Where(t => t.Fin != null)
                .SelectMany(t => t.MarcasTiempo)
                .OrderByDescending(m => m.Fecha)
                .FirstOrDefault();

            if (ultimaMarca != null)
                return $"Último registro: Nota — “{ultimaMarca.Descripcion}”";

            if (ultimaSesionCerrada?.Fin != null && ultimaSesionDurSeg.HasValue)
                return $"Último registro: Sesión — {FormatoDuracionCorta(ultimaSesionDurSeg.Value)} • {HaceCuanto(ultimaSesionCerrada.Fin.Value, now)}";

            return "Último registro: Sesión — —";
        }

        private static string FormatoDuracionCorta(long segundos)
        {
            if (segundos < 0) segundos = 0;
            var ts = TimeSpan.FromSeconds(segundos);

            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";

            return $"{ts.Minutes} min";
        }

        private static string HaceCuanto(DateTime fecha, DateTime now)
        {
            var diff = now - fecha;

            if (diff.TotalMinutes < 1) return "Recién";
            if (diff.TotalHours < 1) return $"Hace {Math.Max(1, (int)diff.TotalMinutes)} min";
            if (diff.TotalDays < 1) return $"Hace {(int)diff.TotalHours} h";
            if (diff.TotalDays < 30) return $"Hace {(int)diff.TotalDays} días";

            return fecha.ToString("dd/MM/yyyy");
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
