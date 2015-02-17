using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using EntityFramework.DynamicFilters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicFiltersTests
{
        //  Tests related to Contains() operator in lambda filters (issue #13
    [TestClass]
    public class DisableFilterTests
    {
        [TestMethod]
        public void DisableFilter_DisableEntityAFilter()
        {
            //  Verify with filters enabled
            using (var context = new TestContext())
            {
                var listA = context.EntityASet.ToList();
                var listB = context.EntityBSet.ToList();
                Assert.IsTrue((listA.Count == 5) && listA.All(a => (a.ID > 5)));
                Assert.IsTrue((listB.Count == 4) && listB.All(a => (a.ID < 5)));
            }

            //  Disable EntityA filter and verify all records returned for A but B still filtered
            using (var context = new TestContext())
            {
                context.DisableFilter("EntityAFilter");

                var listA = context.EntityASet.ToList();
                var listB = context.EntityBSet.ToList();
                Assert.IsTrue((listA.Count == 10) && listA.All(a => (a.ID >= 1) && (a.ID <= 10)));
                Assert.IsTrue((listB.Count == 4) && listB.All(a => (a.ID < 5)));

                //  Re-enable and check again
                context.EnableFilter("EntityAFilter");

                listA = context.EntityASet.ToList();
                listB = context.EntityBSet.ToList();
                Assert.IsTrue((listA.Count == 5) && listA.All(a => (a.ID > 5)));
                Assert.IsTrue((listB.Count == 4) && listB.All(a => (a.ID < 5)));
            }
        }

        [TestMethod]
        public void DisableFilter_DisableAllFilters()
        {
            //  Verify with filters enabled
            using (var context = new TestContext())
            {
                var listA = context.EntityASet.ToList();
                var listB = context.EntityBSet.ToList();
                Assert.IsTrue((listA.Count == 5) && listA.All(a => (a.ID > 5)));
                Assert.IsTrue((listB.Count == 4) && listB.All(a => (a.ID < 5)));
            }

            //  Disable all filters and verify all records returned for both
            using (var context = new TestContext())
            {
                context.DisableAllFilters();

                var listA = context.EntityASet.ToList();
                var listB = context.EntityBSet.ToList();
                Assert.IsTrue((listA.Count == 10) && listA.All(a => (a.ID >= 1) && (a.ID <= 10)));
                Assert.IsTrue((listB.Count == 10) && listB.All(a => (a.ID >= 1) && (a.ID <= 10)));

                //  Re-enable and check again
                context.EnableAllFilters();

                listA = context.EntityASet.ToList();
                listB = context.EntityBSet.ToList();
                Assert.IsTrue((listA.Count == 5) && listA.All(a => (a.ID > 5)));
                Assert.IsTrue((listB.Count == 4) && listB.All(a => (a.ID < 5)));
            }
        }
    }

    #region Models

    public class EntityA
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
    }

    public class EntityB
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
    }

    #endregion

    #region TestContext

    public class TestContext : DbContext
    {
        public DbSet<EntityA> EntityASet { get; set; }
        public DbSet<EntityB> EntityBSet { get; set; }

        public TestContext()
            : base("TestContext")
        {
            Database.SetInitializer(new ContentInitializer<TestContext>());
            Database.Log = log => System.Diagnostics.Debug.WriteLine(log);
            Database.Initialize(false);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //  Constant list filter
            modelBuilder.Filter("EntityAFilter", (EntityA a, int value) => a.ID > value, () => 5);

            //  Dynamic int list filter
            modelBuilder.Filter("EntityBFilter", (EntityB b, int value) => b.ID < value, () => 5);
        }
    }

    public class ContentInitializer<T> : DropCreateDatabaseAlways<T>
        where T : TestContext
    {
        protected override void Seed(T context)
        {
            System.Diagnostics.Debug.Print("Seeding db");

            for (int i = 1; i <= 10; i++)
            {
                context.EntityASet.Add(new EntityA { ID = i });
                context.EntityBSet.Add(new EntityB { ID = i });
            }

            context.SaveChanges();
        }
    }

    #endregion
}
