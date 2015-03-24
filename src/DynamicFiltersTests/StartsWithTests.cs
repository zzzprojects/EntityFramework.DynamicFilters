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
    public class StartsWithTests
    {
        [TestMethod]
        public void StartsWith_ConstantValue()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityASet.ToList();
                Assert.IsTrue((list.Count == 2) && list.All(a => (a.ID == 1) || (a.ID == 2)));
            }
        }

        [TestMethod]
        public void StartsWith_ParameterValue()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityBSet.ToList();
                Assert.IsTrue((list.Count == 2) && list.All(a => (a.ID == 3) || (a.ID == 4)));
            }
        }

        [TestMethod]
        public void StartsWith_ConstantSource()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 2)));
            }
        }

        [TestMethod]
        public void StartsWith_ParameterSource()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityDSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 5)));
            }
        }

        #region Models

        public abstract class EntityBase
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }

            public string Name { get; set; }
        }

        public class EntityA : EntityBase { }
        public class EntityB : EntityBase { }
        public class EntityC : EntityBase { }
        public class EntityD : EntityBase { }

        #endregion

        #region TestContext

        public class TestContext : DbContext
        {
            public DbSet<EntityA> EntityASet { get; set; }
            public DbSet<EntityB> EntityBSet { get; set; }
            public DbSet<EntityC> EntityCSet { get; set; }
            public DbSet<EntityD> EntityDSet { get; set; }

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

                modelBuilder.Filter("EntityAFilter", (EntityA a) => a.Name.StartsWith("J"));
                modelBuilder.Filter("EntityBFilter", (EntityB b, string val) => b.Name.StartsWith(val), () => "B");
                modelBuilder.Filter("EntityCFilter", (EntityC c) => "Joeseph".StartsWith(c.Name));
                modelBuilder.Filter("EntityDFilter", (EntityD d, string val) => val.StartsWith(d.Name), () => "Frederick");
            }
        }

        public class ContentInitializer<T> : DropCreateDatabaseAlways<T>
            where T : TestContext
        {
            protected override void Seed(T context)
            {
                System.Diagnostics.Debug.Print("Seeding db");

                var names = new string[] { "John", "Joe", "Bob", "Barney", "Fred" };

                for (int i = 0; i < 5; i++)
                {
                    context.EntityASet.Add(new EntityA { ID = i + 1, Name = names[i] });
                    context.EntityBSet.Add(new EntityB { ID = i + 1, Name = names[i] });
                    context.EntityCSet.Add(new EntityC { ID = i + 1, Name = names[i] });
                    context.EntityDSet.Add(new EntityD { ID = i + 1, Name = names[i] });
                }

                context.SaveChanges();
            }
        }

        #endregion
    }


}
