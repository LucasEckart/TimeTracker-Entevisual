using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeTracker_Entevisual.Models;

namespace TimeTracker_Entevisual.Data
{
    public class TimeTrackerDbContext : IdentityDbContext<Usuario>
    {

        public TimeTrackerDbContext(DbContextOptions<TimeTrackerDbContext> options) : base(options) { }

        public DbSet<Actividad> Actividades { get; set; } = null!;
        public DbSet<TipoActividad> TiposActividad { get; set; } = null!;
        public DbSet<Tiempo> Tiempos { get; set; } = null!;
        public DbSet<MarcaTiempo> MarcasTiempo { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configuraciones adicionales si es necesario
        }



    }
}
