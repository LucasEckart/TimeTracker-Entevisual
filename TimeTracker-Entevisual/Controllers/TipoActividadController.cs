using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeTracker_Entevisual.Data;
using TimeTracker_Entevisual.Models;

namespace TimeTracker_Entevisual.Controllers
{
    [Authorize(Roles = "Admin,Moderador")]


    public class TipoActividadController : Controller
    {
        private readonly TimeTrackerDbContext _context;

        public TipoActividadController(TimeTrackerDbContext context)
        {
            _context = context;
        }

        // GET: TipoActividad
        public async Task<IActionResult> Index()
        {
            var tipos = await _context.TiposActividad
                .AsNoTracking()
                .OrderBy(t => t.Descripcion)
                .ToListAsync();

            // tipos que están en uso por alguna actividad (no eliminada)
            var tiposEnUso = await _context.Actividades
                .AsNoTracking()
                .Where(a => !a.Eliminado) // si no tenés Eliminado, sacalo
                .Select(a => a.TipoActividadId)
                .Distinct()
                .ToListAsync();

            ViewBag.TiposEnUso = tiposEnUso.ToHashSet();

            return View(tipos);
        }

        // GET: TipoActividad/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tipoActividad = await _context.TiposActividad
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tipoActividad == null)
            {
                return NotFound();
            }

            return View(tipoActividad);
        }

        // GET: TipoActividad/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TipoActividad/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Descripcion")] TipoActividad tipoActividad)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tipoActividad);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tipoActividad);
        }

        // GET: TipoActividad/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tipoActividad = await _context.TiposActividad.FindAsync(id);
            if (tipoActividad == null)
            {
                return NotFound();
            }
            return View(tipoActividad);
        }

        // POST: TipoActividad/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Descripcion")] TipoActividad tipoActividad)
        {
            if (id != tipoActividad.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tipoActividad);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TipoActividadExists(tipoActividad.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tipoActividad);
        }

        // GET: TipoActividad/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var tipoActividad = await _context.TiposActividad
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (tipoActividad == null) return NotFound();

            var cantidad = await _context.Actividades
                .AsNoTracking()
                .CountAsync(a => a.TipoActividadId == id && !a.Eliminado); // si no tenés Eliminado, sacalo

            ViewBag.EnUso = cantidad > 0;
            ViewBag.CantidadActividades = cantidad;

            return View(tipoActividad);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipo = await _context.TiposActividad.FirstOrDefaultAsync(t => t.Id == id);
            if (tipo == null) return NotFound();

            var enUso = await _context.Actividades
                .AsNoTracking()
                .AnyAsync(a => a.TipoActividadId == id && !a.Eliminado); // si no tenés Eliminado, sacalo

            if (enUso)
            {
                TempData["Error"] = "No se puede eliminar este tipo porque está asociado a una o más actividades.";
                return RedirectToAction(nameof(Index));
            }

            _context.TiposActividad.Remove(tipo);
            await _context.SaveChangesAsync();

            TempData["Ok"] = "Tipo eliminado.";
            return RedirectToAction(nameof(Index));
        }


        private bool TipoActividadExists(int id)
        {
            return _context.TiposActividad.Any(e => e.Id == id);
        }
    }
}
