using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using EntityFramework.DynamicFilters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicFiltersTests
{
    [TestClass]
    public class ChildPropertyFiltersTests
    {

        [TestMethod]
        public void ChildPropertyFilters_WhereAny()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityASet.Where(a => a.SubEntities.Any()).ToList();

                Assert.IsTrue((list.Count == 3) && list.All(a => (a.ID >= 1) && (a.ID <= 3)));
            }
        }
        
        #region Models

        public interface IEntitySoftDelete
        {
            bool IsDeleted { get; set; }
        }

        public class EntityA : IEntitySoftDelete
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }
            public bool IsDeleted { get; set; }

            public ICollection<EntityB> SubEntities { get; set; }
        }

        public class EntityB : IEntitySoftDelete
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }
            public bool IsDeleted { get; set; }
        }
        
        #endregion

        #region TestContext

        public class TestContext : TestContextBase<TestContext>, ITestContext
        {
            public DbSet<EntityA> EntityASet { get; set; }
            public DbSet<EntityB> EntityBSet { get; set; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Filter("IsDeleted", (IEntitySoftDelete d) => d.IsDeleted==false);
            }

            public override void Seed()
            {
                System.Diagnostics.Debug.Print("Seeding db");

                for (int i = 1; i <= 5; i++)
                    EntityASet.Add(new EntityA { ID = i, SubEntities = new List<EntityB>() { new EntityB() { ID = i, IsDeleted = i > 3} } });

                SaveChanges();
            }
        }

        #endregion
    }
}
