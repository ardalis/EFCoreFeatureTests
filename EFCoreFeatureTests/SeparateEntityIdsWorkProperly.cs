using Microsoft.EntityFrameworkCore;
using System.Linq;
using Xunit;

namespace EFCoreFeatureTests
{
    public class SeparateEntityIdsWorkProperly
    {
        //[Fact]
        //public void Test1()
        //{
        //    var dbContext = new AppDbContext(DbConfig.CreateNewContextOptions());

        //    var item1 = new Item() { Name = "Steve" };

        //    dbContext.Items.Add(item1);
        //    Assert.Equal(1, item1.Id);
        //}

        [Fact]
        public void ShowFailureInPre30EFCore()
        {
            var db1 = GetMyDb();
            var zach1 = db1.Items.Where(p => p.Id == 1).FirstOrDefault();
            Assert.NotNull(zach1); // works

            var db2 = GetMyDb();
            var zach2 = db2.Items.Where(p => p.Id == 1).FirstOrDefault();
            Assert.NotNull(zach2); // fails (because the DB is on 3 and 4 now)
        }

        private static AppDbContext GetMyDb()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // This is the magic line
            optionsBuilder.UseInMemoryDatabase("fixed");
            var db = new AppDbContext(optionsBuilder.Options);
            db.Database.EnsureDeleted();

            db.Items.Add(
                new Item
                {
                    Name = "Zach"
                });
            db.Items.Add(
                new Item
                {
                    Name = "George"
                }
            );

            db.SaveChanges();
            return db;
        }

    }
}
