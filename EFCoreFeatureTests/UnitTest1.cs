using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace EFCoreFeatureTests
{
    public static class DbContextExtensions
    {
        public static void ResetValueGenerators(this DbContext context)
        {
            var cache = context.GetService<IValueGeneratorCache>();

            foreach (var keyProperty in context.Model.GetEntityTypes()
                .Select(e => e.FindPrimaryKey().Properties[0])
                .Where(p => p.ClrType == typeof(int)
                            && p.ValueGenerated == ValueGenerated.OnAdd))
            {
                var generator = (ResettableValueGenerator)cache.GetOrAdd(
                    keyProperty,
                    keyProperty.DeclaringEntityType,
                    (p, e) => new ResettableValueGenerator());

                generator.Reset();
            }
        }
    }

    public class ResettableValueGenerator : ValueGenerator<int>
    {
        private int _current;

        public override bool GeneratesTemporaryValues => false;

        public override int Next(EntityEntry entry)
            => Interlocked.Increment(ref _current);

        public void Reset() => _current = 0;
    }
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class AppDbContext : DbContext
    {
        public DbSet<Item> Items { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
    }
    public class Tests
    {
        private static DbContextOptions<AppDbContext> CreateNewContextOptions()
        {
            // Create a fresh service provider, and therefore a fresh 
            // InMemory database instance.
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Create a new options instance telling the context to use an
            // InMemory database and the new service provider.
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            builder.UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseInternalServiceProvider(serviceProvider);

            return builder.Options;
        }

        [Fact]
        public void Test1()
        {
            // create a brand new dbContext
            var dbContext = new AppDbContext(CreateNewContextOptions());

            // add one item
            var item = new Item();
            dbContext.Items.Add(item);
            dbContext.SaveChanges();

            // ID should be 1
            Assert.Equal(1, item.Id);

            // dbContext.ResetValueGenerators(); // blows up in 2.2.6
            dbContext.Database.EnsureDeleted();

            Assert.False(dbContext.Items.Any());

            // This will fail in and 3.0.0
            // Makes no difference if ResetValueGenerators() is called
            var item2 = new Item();
            dbContext.Items.Add(item2); // InvalidOperation - Id with value 1 is already being tracked
            dbContext.SaveChanges();

            // ID should STILL be 1; it's 2 in 2.2.6 without running ResetValueGenerators
            Assert.Equal(1, item2.Id);

        }
    }
}
