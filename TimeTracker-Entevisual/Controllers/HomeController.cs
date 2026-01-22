using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TimeTracker_Entevisual.Data;
using TimeTracker_Entevisual.Models;
using TimeTracker_Entevisual.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TimeTracker_Entevisual.Controllers
{
    public class HomeController : Controller
    {
        private const string EmailInterno = "interno@timetracker.local";

        private readonly TimeTrackerDbContext _context;
        private readonly UserManager<Usuario> _userManager; // por ahora no se usa (modo interno)

        public HomeController(TimeTrackerDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public async Task<IActionResult> Index(int? pausaTiempoId = null, int? pausaActividadId = null)
        {
            var usuarioId = await GetUsuarioInternoId();

            var now = DateTime.Now;
            var inicioMes = new DateTime(now.Year, now.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            var actividades = await _context.Actividades
                .AsNoTracking()
                .Where(a => a.UsuarioId == usuarioId && !a.Archivado)
                .Include(a => a.TipoActividad)
                .Include(a => a.Tiempos).ThenInclude(t => t.MarcasTiempo)
                .ToListAsync();

            var vm = new IndexVM
            {
                MesActualTexto = now.ToString("MMMM yyyy"),
                PausaTiempoId = pausaTiempoId,
                PausaActividadId = pausaActividadId,
                TiposActividad = await _context.TiposActividad
                    .AsNoTracking()
                    .OrderBy(t => t.Descripcion)
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = t.Descripcion
                    })
                    .ToListAsync()
            };

            // Para mostrar "Período registrado: 2h 00m" en el modal
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

            foreach (var a in actividades)
            {
                var card = BuildCardVM(a, now, inicioMes, finMes);

                if (card.Estado == EstadoActividadVM.Corriendo)
                    vm.EnEjecucion = card;
                else
                    vm.Actividades.Add(card);
            }

            return View(vm);
        }

        private async Task<string> GetUsuarioInternoId()
        {
            return await _context.Users
                .Where(u => u.Email == EmailInterno)
                .Select(u => u.Id)
                .FirstAsync();
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
                AcumuladoMes = FormatoHHMMSS(acumSeg),
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

        private static string FormatoHHMMSS(long segundos)
        {
            if (segundos < 0) segundos = 0;
            var ts = TimeSpan.FromSeconds(segundos);
            var totalHoras = (int)ts.TotalHours;
            return $"{totalHoras:00}:{ts.Minutes:00}:{ts.Seconds:00}";
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
