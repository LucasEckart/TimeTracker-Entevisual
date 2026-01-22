using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTracker_Entevisual.Data;
using TimeTracker_Entevisual.Models;
using TimeTracker_Entevisual.Models.ViewModels;

namespace TimeTracker_Entevisual.Controllers
{
    public class ActividadController : Controller
    {
        private readonly TimeTrackerDbContext _context;

        public ActividadController(TimeTrackerDbContext context)
        {
            _context = context;
        }

        private async Task<string> GetUsuarioInternoId()
        {
            const string emailInterno = "interno@timetracker.local";
            return await _context.Users
                .Where(u => u.Email == emailInterno)
                .Select(u => u.Id)
                .FirstAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int tipoActividadId, string? codigo, string titulo, string? descripcion, string? notas)
        {
            var usuarioId = await GetUsuarioInternoId();

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
                .Where(t => t.Fin == null && t.Actividad.UsuarioId == usuarioId && t.ActividadId != actividadObjetivoId)
                .FirstOrDefaultAsync();

            if (abierto != null)
            {
                abierto.Fin = DateTime.Now;
                abierto.DuracionSegundos = (int)(abierto.Fin.Value - abierto.Inicio).TotalSeconds;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Iniciar(int actividadId)
        {
            var usuarioId = await GetUsuarioInternoId();
            await PausarCualquierOtroAbierto(usuarioId, actividadId);

            // si ya hay uno abierto en esa actividad, no hacemos nada
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
            var tiempo = await _context.Tiempos
                .Where(t => t.ActividadId == actividadId && t.Fin == null)
                .FirstOrDefaultAsync();

            if (tiempo == null)
                return RedirectToAction("Index", "Home");

            tiempo.Fin = DateTime.Now;
            tiempo.DuracionSegundos = (int)(tiempo.Fin.Value - tiempo.Inicio).TotalSeconds;

            await _context.SaveChangesAsync();

            // abre modal de nota en Home/Index
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
            // si querés permitir "sin nota", simplemente no guardamos marca
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
            var act = await _context.Actividades.FirstOrDefaultAsync(a => a.Id == actividadId);
            if (act == null) return RedirectToAction("Index", "Home");

            act.Archivado = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public async Task<IActionResult> NotasModal(int actividadId)
        {
            var actividad = await _context.Actividades
                .AsNoTracking()
                .Include(a => a.Tiempos).ThenInclude(t => t.MarcasTiempo)
                .FirstOrDefaultAsync(a => a.Id == actividadId);

            if (actividad == null) return NotFound();

            // ✅ ACÁ va el punto 3 (filtrar por Fin != null y !OcultoEnNotas)
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
        public async Task<IActionResult> NotaActualizar(int marcaTiempoId, string? descripcion)
        {
            var nota = await _context.MarcasTiempo.FindAsync(marcaTiempoId);
            if (nota == null) return NotFound();

            nota.Descripcion = (descripcion ?? "").Trim();
            await _context.SaveChangesAsync();

            return Ok(new { ok = true, texto = nota.Descripcion });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotaEliminar(int marcaTiempoId)
        {
            var nota = await _context.MarcasTiempo.FindAsync(marcaTiempoId);
            if (nota == null) return NotFound();

            _context.MarcasTiempo.Remove(nota);
            await _context.SaveChangesAsync();

            return Ok(new { ok = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotaUpsert(int tiempoId, string? descripcion)
        {
            var tiempo = await _context.Tiempos
                .Include(t => t.MarcasTiempo)
                .FirstOrDefaultAsync(t => t.Id == tiempoId);

            if (tiempo == null) return NotFound();

            var texto = (descripcion ?? "").Trim();

            // Tomamos "la nota" de la sesión como 1 sola (última o única)
            var nota = tiempo.MarcasTiempo
                .OrderByDescending(m => m.Fecha)
                .FirstOrDefault();

            if (nota == null)
            {
                // si está vacío, y no había nota, no creamos nada
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
                // actualizar (permitimos vacío => queda “sin nota”)
                nota.Descripcion = texto;
                nota.Fecha = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(new { ok = true, texto });
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> NotaEliminarPorTiempo(int tiempoId)
        //{
        //    var nota = await _context.MarcasTiempo
        //        .Where(m => m.TiempoId == tiempoId)
        //        .OrderByDescending(m => m.Fecha)
        //        .FirstOrDefaultAsync();

        //    if (nota == null)
        //        return Ok(new { ok = true }); // ya estaba “sin nota”

        //    _context.MarcasTiempo.Remove(nota);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { ok = true });
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OcultarSesionEnNotas(int tiempoId)
        {
            var tiempo = await _context.Tiempos.FirstOrDefaultAsync(t => t.Id == tiempoId);
            if (tiempo == null) return Ok(new { ok = true });

            tiempo.OcultoEnNotas = true;
            await _context.SaveChangesAsync();

            return Ok(new { ok = true });
        }



    }
}
