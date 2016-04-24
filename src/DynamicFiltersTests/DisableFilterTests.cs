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

        [TestMethod]
        public void DisableFilter_DisableNoParamFilter()
        {
            //  Verify with filters enabled
            using (var context = new TestContext())
            {
                var list = context.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 4) && list.All(a => (a.ID < 5)));

                context.DisableFilter("EntityCFilter");
                list = context.EntityCSet.ToList();
                Assert.IsTrue(list.Count == 10);
            }
        }

        [TestMethod]
        public void DisableFilter_FilterDisablingOff_NoParamFilter()
        {
            //  Verify with filters enabled
            using (var context = new TestContext())
            {
                var list = context.EntityDSet.ToList();
                Assert.IsTrue((list.Count == 4) && list.All(a => (a.ID < 5)));
            }
        }

        [TestMethod]
        public void DisableFilter_FilterDisablingOff_SingleParamFilter()
        {
            //  Verify with filters enabled
            using (var context = new TestContext())
            {
                var list = context.EntityESet.ToList();
                Assert.IsTrue((list.Count == 4) && list.All(a => (a.ID < 5)));
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

        public class EntityC
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }
        }

        public class EntityD
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }
        }

        public class EntityE
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }
        }

        #endregion

        #region TestContext

        public class TestContext : TestContextBase<TestContext>, ITestContext
        {
            public DbSet<EntityA> EntityASet { get; set; }
            public DbSet<EntityB> EntityBSet { get; set; }
            public DbSet<EntityC> EntityCSet { get; set; }
            public DbSet<EntityD> EntityDSet { get; set; }
            public DbSet<EntityE> EntityESet { get; set; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                //  Constant list filter
                modelBuilder.Filter("EntityAFilter", (EntityA a, int value) => a.ID > value, () => 5);

                //  Dynamic int list filter
                modelBuilder.Filter("EntityBFilter", (EntityB b, int value) => b.ID < value, () => 5);

                //  No parmeter filter (issue #22)
                modelBuilder.Filter("EntityCFilter", (EntityC c) => c.ID < 5);

                modelBuilder.Filter("EntityDFilter", (EntityD d) => d.ID < 5);
                modelBuilder.PreventDisabledFilterConditions("EntityDFilter");

                modelBuilder.Filter("EntityEFilter", (EntityE e, int value) => e.ID < value, () => 5);
                modelBuilder.PreventDisabledFilterConditions("EntityEFilter");
            }

            public override void Seed()
            {
                System.Diagnostics.Debug.Print("Seeding db");

                for (int i = 1; i <= 10; i++)
                {
                    EntityASet.Add(new EntityA { ID = i });
                    EntityBSet.Add(new EntityB { ID = i });
                    EntityCSet.Add(new EntityC { ID = i });
                    EntityDSet.Add(new EntityD { ID = i });
                    EntityESet.Add(new EntityE { ID = i });
                }

                SaveChanges();
            }

        }

        #endregion
    }

}
