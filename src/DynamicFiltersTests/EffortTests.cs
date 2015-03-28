using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using EntityFramework.DynamicFilters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicFiltersTests
{

    [TestClass]
    public class EffortTests
    {
        [AssemblyInitialize()]
        public static void AssemblyInit(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext context)
        {
            Effort.Provider.EffortProviderConfiguration.RegisterProvider();
        }

        [TestMethod]
        public void Effort_NoParameters()
        {
            using (var context = new TestContext(Effort.DbConnectionFactory.CreateTransient()))
            {
                var list = context.EntityASet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 2)));
            }
        }

        [TestMethod]
        public void Effort_SingleParameter()
        {
            using (var context = new TestContext(Effort.DbConnectionFactory.CreateTransient()))
            {
                var list = context.EntityBSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 2)));
            }
        }

        [TestMethod]
        public void Effort_DisableFilter()
        {
            using (var context = new TestContext(Effort.DbConnectionFactory.CreateTransient()))
            {
                context.DisableFilter("EntityBFilter");

                var list = context.EntityBSet.ToList();
                Assert.IsTrue(list.Count == 5);
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

        #endregion

        #region TestContext

        public class TestContext : DbContext
        {
            public DbSet<EntityA> EntityASet { get; set; }
            public DbSet<EntityB> EntityBSet { get; set; }

            public TestContext(DbConnection dbConnection)
                : base(dbConnection, false)
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
                Database.Log = log => System.Diagnostics.Debug.WriteLine(log);
                Database.Initialize(false);
            }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Filter("EntityAFilter", (EntityA a) => a.Name == "Joe");
                modelBuilder.Filter("EntityBFilter", (EntityB b) => b.Name, "Joe");
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
                }

                context.SaveChanges();
            }
        }

        #endregion
    }
}
