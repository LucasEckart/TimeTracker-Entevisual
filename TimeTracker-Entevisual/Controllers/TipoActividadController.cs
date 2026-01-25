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
            return View(await _context.TiposActividad.ToListAsync());
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

        // POST: TipoActividad/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipoActividad = await _context.TiposActividad.FindAsync(id);
            if (tipoActividad != null)
            {
                _context.TiposActividad.Remove(tipoActividad);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TipoActividadExists(int id)
        {
            return _context.TiposActividad.Any(e => e.Id == id);
        }
    }
}
