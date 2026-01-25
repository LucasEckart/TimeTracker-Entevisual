using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTracker_Entevisual.Data;
using TimeTracker_Entevisual.Models;
using TimeTracker_Entevisual.Models.ViewModels;

namespace TimeTracker_Entevisual.Controllers
{
    [Authorize]
    public class ActividadController : Controller
    {
        private readonly TimeTrackerDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public ActividadController(TimeTrackerDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string? UsuarioIdActual() => _userManager.GetUserId(User);
        private bool EsAdmin() => User.IsInRole("Admin");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int tipoActividadId, string? codigo, string titulo, string? descripcion, string? notas)
        {
            var usuarioId = UsuarioIdActual();
            if (string.IsNullOrWhiteSpace(usuarioId))
                return RedirectToAction("Login", "Account");

            // si querés: impedir que admin cree actividades (admin solo audita)
            // if (EsAdmin()) return Forbid();

            var act = new Actividad
            {
                UsuarioId = usuarioId,
                TipoActividadId = tipoActividadId,
                Codigo = string.IsNullOrWhiteSpace(codigo) ? null : codigo.Trim(),
                Titulo = titulo.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
                Notas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim(),
                FechaCreacion = DateTime.Now,
                Archivado = false
            };

            _context.Actividades.Add(act);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        // regla: solo 1 timer activo -> al iniciar/reanudar, pausamos cualquier otro abierto del usuario (sin nota)
        private async Task PausarCualquierOtroAbierto(string usuarioId, int actividadObjetivoId)
        {
            var abierto = await _context.Tiempos
                .Include(t => t.Actividad)
                .Where(t => t.Fin == null &&
                            t.Actividad.UsuarioId == usuarioId &&
                            t.ActividadId != actividadObjetivoId)
                .FirstOrDefaultAsync();

            if (abierto != null)
            {
                abierto.Fin = DateTime.Now;
                abierto.DuracionSegundos = (int)(abierto.Fin.Value - abierto.Inicio).TotalSeconds;
            }
        }

        private async Task<Actividad?> GetActividadSegura(int actividadId)
        {
            var act = await _context.Actividades.FirstOrDefaultAsync(a => a.Id == actividadId);
            if (act == null) return null;

            if (EsAdmin()) return act; // admin puede ver todo

            var usuarioId = UsuarioIdActual();
            if (string.IsNullOrWhiteSpace(usuarioId)) return null;

            return act.UsuarioId == usuarioId ? act : null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Iniciar(int actividadId)
        {
            var usuarioId = UsuarioIdActual();
            if (string.IsNullOrWhiteSpace(usuarioId))
                return RedirectToAction("Login", "Account");

            // validar dueño (admin también pasa; si querés bloquear admin, acá sería Forbid)
            var act = await GetActividadSegura(actividadId);
            if (act == null) return Forbid();

            await PausarCualquierOtroAbierto(usuarioId, actividadId);

            var yaAbierto = await _context.Tiempos.AnyAsync(t => t.ActividadId == actividadId && t.Fin == null);
            if (!yaAbierto)
            {
                _context.Tiempos.Add(new Tiempo
                {
                    ActividadId = actividadId,
                    Inicio = DateTime.Now,
                    Fin = null,
                    DuracionSegundos = null
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reanudar(int actividadId) => await Iniciar(actividadId);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pausar(int actividadId)
        {
            var act = await GetActividadSegura(actividadId);
            if (act == null) return Forbid();

            var tiempo = await _context.Tiempos
                .Where(t => t.ActividadId == actividadId && t.Fin == null)
                .FirstOrDefaultAsync();

            if (tiempo == null)
                return RedirectToAction("Index", "Home");

            tiempo.Fin = DateTime.Now;
            tiempo.DuracionSegundos = (int)(tiempo.Fin.Value - tiempo.Inicio).TotalSeconds;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home", new
            {
                pausaTiempoId = tiempo.Id,
                pausaActividadId = actividadId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarNotaPausa(int tiempoId, int actividadId, string? descripcion)
        {
            // validar dueño vía actividad
            var act = await GetActividadSegura(actividadId);
            if (act == null) return Forbid();

            if (!string.IsNullOrWhiteSpace(descripcion))
            {
                _context.MarcasTiempo.Add(new MarcaTiempo
                {
                    TiempoId = tiempoId,
                    Fecha = DateTime.Now,
                    Descripcion = descripcion.Trim()
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archivar(int actividadId)
        {
            var act = await GetActividadSegura(actividadId);
            if (act == null) return Forbid();

            act.Archivado = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> NotasModal(int actividadId)
        {
            // ✅ Para NotasModal necesitamos traer tiempos + marcas y validar dueño
            var actividad = await _context.Actividades
                .AsNoTracking()
                .Include(a => a.Tiempos).ThenInclude(t => t.MarcasTiempo)
                .FirstOrDefaultAsync(a => a.Id == actividadId);

            if (actividad == null) return NotFound();

            // validar dueño si no es admin
            if (!EsAdmin())
            {
                var usuarioId = UsuarioIdActual();
                if (string.IsNullOrWhiteSpace(usuarioId)) return Forbid();
                if (actividad.UsuarioId != usuarioId) return Forbid();
            }

            var items = actividad.Tiempos
                .Where(t => t.Fin != null && !t.OcultoEnNotas)
                .OrderByDescending(t => t.Fin)
                .Select(t =>
                {
                    var durSeg = t.DuracionSegundos ?? (long)(t.Fin!.Value - t.Inicio).TotalSeconds;

                    var nota = t.MarcasTiempo
                        .OrderByDescending(m => m.Fecha)
                        .FirstOrDefault();

                    return new NotaItemVM
                    {
                        TiempoId = t.Id,
                        Fin = t.Fin!.Value,
                        DuracionTexto = FormatoDuracionCorta(durSeg),
                        Texto = string.IsNullOrWhiteSpace(nota?.Descripcion) ? "— sin nota —" : nota.Descripcion
                    };
                })
                .ToList();

            var vm = new NotasModalVM
            {
                ActividadId = actividad.Id,
                TituloActividad = actividad.Titulo,
                Items = items
            };

            return PartialView("_NotasModal", vm);
        }

        private static string FormatoDuracionCorta(long segundos)
        {
            if (segundos < 0) segundos = 0;
            var ts = TimeSpan.FromSeconds(segundos);

            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";

            return $"{ts.Minutes} min";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotaUpsert(int tiempoId, string? descripcion)
        {
            var tiempo = await _context.Tiempos
                .Include(t => t.Actividad)
                .Include(t => t.MarcasTiempo)
                .FirstOrDefaultAsync(t => t.Id == tiempoId);

            if (tiempo == null) return NotFound();

            // validar dueño si no es admin
            if (!EsAdmin())
            {
                var usuarioId = UsuarioIdActual();
                if (string.IsNullOrWhiteSpace(usuarioId)) return Forbid();
                if (tiempo.Actividad.UsuarioId != usuarioId) return Forbid();
            }

            var texto = (descripcion ?? "").Trim();

            var nota = tiempo.MarcasTiempo
                .OrderByDescending(m => m.Fecha)
                .FirstOrDefault();

            if (nota == null)
            {
                if (string.IsNullOrWhiteSpace(texto))
                    return Ok(new { ok = true, texto = "" });

                nota = new MarcaTiempo
                {
                    TiempoId = tiempoId,
                    Fecha = DateTime.Now,
                    Descripcion = texto
                };
                _context.MarcasTiempo.Add(nota);
            }
            else
            {
                nota.Descripcion = texto;
                nota.Fecha = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(new { ok = true, texto });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OcultarSesionEnNotas(int tiempoId)
        {
            var tiempo = await _context.Tiempos
                .Include(t => t.Actividad)
                .FirstOrDefaultAsync(t => t.Id == tiempoId);

            if (tiempo == null) return Ok(new { ok = true });

            // validar dueño si no es admin
            if (!EsAdmin())
            {
                var usuarioId = UsuarioIdActual();
                if (string.IsNullOrWhiteSpace(usuarioId)) return Forbid();
                if (tiempo.Actividad.UsuarioId != usuarioId) return Forbid();
            }

            tiempo.OcultoEnNotas = true;
            await _context.SaveChangesAsync();

            return Ok(new { ok = true });
        }

        // (Opcional) estos dos endpoints no los vi en el JS final que estabas usando,
        // pero si los seguís usando, también deberían validar dueño:
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotaActualizar(int marcaTiempoId, string? descripcion)
        {
            var nota = await _context.MarcasTiempo
                .Include(m => m.Tiempo).ThenInclude(t => t.Actividad)
                .FirstOrDefaultAsync(m => m.Id == marcaTiempoId);

            if (nota == null) return NotFound();

            if (!EsAdmin())
            {
                var usuarioId = UsuarioIdActual();
                if (string.IsNullOrWhiteSpace(usuarioId)) return Forbid();
                if (nota.Tiempo.Actividad.UsuarioId != usuarioId) return Forbid();
            }

            nota.Descripcion = (descripcion ?? "").Trim();
            await _context.SaveChangesAsync();

            return Ok(new { ok = true, texto = nota.Descripcion });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotaEliminar(int marcaTiempoId)
        {
            var nota = await _context.MarcasTiempo
                .Include(m => m.Tiempo).ThenInclude(t => t.Actividad)
                .FirstOrDefaultAsync(m => m.Id == marcaTiempoId);

            if (nota == null) return NotFound();

            if (!EsAdmin())
            {
                var usuarioId = UsuarioIdActual();
                if (string.IsNullOrWhiteSpace(usuarioId)) return Forbid();
                if (nota.Tiempo.Actividad.UsuarioId != usuarioId) return Forbid();
            }

            _context.MarcasTiempo.Remove(nota);
            await _context.SaveChangesAsync();

            return Ok(new { ok = true });
        }
    }
}
