using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


namespace TimeTracker_Entevisual.Data
{
    public class TimeTrackerDbContextFactory
    {
        public TimeTrackerDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TimeTrackerDbContext>();

            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=Movie_DB;Integrated Security=true;TrustServerCertificate=true"
            );

            return new TimeTrackerDbContext(optionsBuilder.Options);
        }
    }
}
