using Microsoft.EntityFrameworkCore;

namespace EFCoreFeatureTests
{
    public class AppDbContext : DbContext
    {
        public DbSet<Item> Items { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
    }
}
