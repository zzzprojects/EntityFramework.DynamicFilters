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
    /// <summary>
    /// Misc tests for names and such
    /// </summary>
    [TestClass]
    public class NameTests
    {
        [TestMethod]
        public void Name_UnderscoreInNoParamFilter()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityASet.ToList();
                Assert.IsTrue((list.Count == 2) && list.All(a => a.ID == 1 || a.ID == 2));
            }
        }

        [TestMethod]
        public void Name_UnderscoreInParamFilter()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityBSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(b => b.ID == 3));
            }
        }

        #region Models

        public class EntityA
        {
            public int ID { get; set; }

            public int Parent_ID { get; set; }
        }

        public class EntityB
        {
            public int ID { get; set; }

            public int Parent_ID { get; set; }
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

                modelBuilder.Filter("EntityAFilter", (EntityA a) => a.Parent_ID, () => 10);
                modelBuilder.Filter("EntityBFilter", (EntityB b, int parent_ID) => b.Parent_ID == parent_ID, () => 20);
            }
        }

        public class ContentInitializer<T> : DropCreateDatabaseAlways<T>
            where T : TestContext
        {
            protected override void Seed(T context)
            {
                System.Diagnostics.Debug.Print("Seeding db");

                context.EntityASet.Add(new EntityA { ID = 1, Parent_ID = 10 });
                context.EntityASet.Add(new EntityA { ID = 2, Parent_ID = 10 });
                context.EntityASet.Add(new EntityA { ID = 3, Parent_ID = 20 });
                context.EntityASet.Add(new EntityA { ID = 4, Parent_ID = 30 });

                context.EntityBSet.Add(new EntityB { ID = 1, Parent_ID = 10 });
                context.EntityBSet.Add(new EntityB { ID = 2, Parent_ID = 10 });
                context.EntityBSet.Add(new EntityB { ID = 3, Parent_ID = 20 });
                context.EntityBSet.Add(new EntityB { ID = 4, Parent_ID = 30 });

                context.SaveChanges();
            }
        }

        #endregion
    }

}
