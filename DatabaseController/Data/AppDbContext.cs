using Microsoft.EntityFrameworkCore;

namespace DatabaseController.Data 
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Models.ModelObject> Objects => Set<Models.ModelObject>();
    }
}