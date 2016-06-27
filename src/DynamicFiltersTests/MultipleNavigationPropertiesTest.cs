using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EntityFramework.DynamicFilters;

namespace DynamicFiltersTests
{
    //  Tests for entities that have multiple filtered navigation properties of same type (issue #17).

    [TestClass]
    public class MultipleNavigationPropertiesTest
    {

        [TestMethod]
        public void MultipleNavigationProperties_IsDeletedFilterEnabled()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityASet.Include(a => a.Nav1).Include(a => a.Nav2).Include(a => a.Nav3).ToList();

                Assert.IsTrue((list.Count == 2), "list does not contain 2 items");

                var entityA1 = list.FirstOrDefault(a => a.Id == 1);
                var entityA2 = list.FirstOrDefault(a => a.Id == 2);

                Assert.IsTrue(entityA1 != null, "EntityA.Id=1 not found");
                Assert.IsTrue(entityA2 != null, "EntityA.Id=2 not found");
                Assert.IsTrue((entityA1.Nav1 != null) && (entityA1.Nav2 == null) && (entityA1.Nav3 == null), "Navigation properties for EntityA.Id=1 not filtered correctly");
                Assert.IsTrue((entityA2.Nav1 != null) && (entityA2.Nav2 == null) && (entityA2.Nav3 != null), "Navigation properties for EntityA.Id=2 not filtered correctly");
            }
        }

        [TestMethod]
        public void MultipleNavigationProperties_IsDeletedFilterDisabled()
        {
            using (var context = new TestContext())
            {
                context.DisableFilter("IsDeleted");

                var list = context.EntityASet.Include(a => a.Nav1).Include(a => a.Nav2).Include(a => a.Nav3).ToList();

                Assert.IsTrue((list.Count == 3), "list does not contain 3 items");
                Assert.IsTrue(!list.Any(a => (a.Nav1 == null) || (a.Nav2 == null)), "Nav1 or Nav2 properties not loaded");
            }
        }

        #region Models

        //  2nd ISoftDelete so it doesn't interfere with the filter tests on the other models
        public interface IEntitySoftDelete
        {
            bool IsDeleted { get; set; }
        }

        public class EntityA : IEntitySoftDelete
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public bool IsDeleted { get; set; }

            public int? Nav1ID { get; set; }        //  Must specify FK property for dynamic filter to work
            public EntityB Nav1 { get; set; }

            public int? Nav2ID { get; set; }        //  Must specify FK property for dynamic filter to work
            public EntityB Nav2 { get; set; }

            public int? Nav3ID { get; set; }        //  Must specify FK property for dynamic filter to work
            public EntityA Nav3 { get; set; }
        }

        public class EntityB : IEntitySoftDelete
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
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

                //  Multiple navigation property filter
                modelBuilder.Filter("IsDeleted", (IEntitySoftDelete d) => d.IsDeleted, false);
            }

            public override void Seed()
            {
                System.Diagnostics.Debug.Print("Seeding db");

                var a3 = new EntityA { Id = 3, IsDeleted = true, Nav1 = new EntityB { Id = 30, IsDeleted = false }, Nav2 = new EntityB { Id = 31, IsDeleted = true } };
                var a1 = new EntityA { Id = 1, IsDeleted = false, Nav1 = new EntityB { Id = 10, IsDeleted = false }, Nav2 = new EntityB { Id = 11, IsDeleted = true }, Nav3 = a3 };
                var a2 = new EntityA { Id = 2, IsDeleted = false, Nav1 = new EntityB { Id = 20, IsDeleted = false }, Nav2 = new EntityB { Id = 21, IsDeleted = true }, Nav3 = a1 };
                EntityASet.Add(a1);
                EntityASet.Add(a2);
                EntityASet.Add(a3);
                SaveChanges();
            }
        }

        #endregion
    }
}
