using System;
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
    public class StringFunctionsTests
    {
        [TestMethod]
        public void StringFunction_StartsWith_ConstantValue()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityASet.ToList();
                Assert.IsTrue((list.Count == 2) && list.All(a => (a.ID == 1) || (a.ID == 2)));
            }
        }

        [TestMethod]
        public void StringFunction_StartsWith_ParameterValue()
        {
            using (var context1 = new TestContext())
            {
                try
                {
                    var list = context1.EntityBSet.ToList();
                    Assert.IsTrue((list.Count == 2) && list.All(a => (a.ID == 3) || (a.ID == 4)));
                }
                catch (Exception ex)
                {
                    //  A System.Format exception is the expected result for SQL Server CE.  It does not support
                    //  "like @value+'%'".  See: https://stackoverflow.com/questions/1916248/how-to-use-parameter-with-like-in-sql-server-compact-edition
                    //  And there is no way for us to know that we need to append the % character to the parameter value during
                    //  sql interception (because we don't know that the param is being used on a StartsWith function).
                    if ((ex.InnerException != null) && (ex.InnerException is FormatException) && context1.IsSQLCE())
                        return;

                    throw ex;
                }
            }
        }

        [TestMethod]
        public void StringFunction_StartsWith_ConstantSource()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 2)));
            }
        }

        [TestMethod]
        public void StringFunction_StartsWith_ParameterSource()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityDSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 5)));
            }
        }

        [TestMethod]
        public void StringFunction_Contains1()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityESet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 4)));
            }
        }

        [TestMethod]
        public void StringFunction_Contains2()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityFSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 4)));
            }
        }

        [TestMethod]
        public void StringFunction_Contains3()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityGSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 4)));
            }
        }

        [TestMethod]
        public void StringFunction_Contains4()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityHSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 4)));
            }
        }

        [TestMethod]
        public void StringFunction_EndsWith1()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityISet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 4)));
            }
        }

        [TestMethod]
        public void StringFunction_EndsWith2()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityJSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 4)));
            }
        }

        [TestMethod]
        public void StringFunction_EndsWith3()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityKSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 4)));
            }
        }

        [TestMethod]
        public void StringFunction_EndsWith4()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityLSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.ID == 4)));
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
        public class EntityE : EntityBase { }
        public class EntityF : EntityBase { }
        public class EntityG : EntityBase { }
        public class EntityH : EntityBase { }
        public class EntityI : EntityBase { }
        public class EntityJ : EntityBase { }
        public class EntityK : EntityBase { }
        public class EntityL : EntityBase { }

        #endregion

        #region TestContext

        public class TestContext : TestContextBase<TestContext>, ITestContext
        {
            public DbSet<EntityA> EntityASet { get; set; }
            public DbSet<EntityB> EntityBSet { get; set; }
            public DbSet<EntityC> EntityCSet { get; set; }
            public DbSet<EntityD> EntityDSet { get; set; }
            public DbSet<EntityE> EntityESet { get; set; }
            public DbSet<EntityF> EntityFSet { get; set; }
            public DbSet<EntityG> EntityGSet { get; set; }
            public DbSet<EntityH> EntityHSet { get; set; }
            public DbSet<EntityI> EntityISet { get; set; }
            public DbSet<EntityJ> EntityJSet { get; set; }
            public DbSet<EntityK> EntityKSet { get; set; }
            public DbSet<EntityL> EntityLSet { get; set; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Filter("EntityAFilter", (EntityA a) => a.Name.StartsWith("J"));
                modelBuilder.Filter("EntityBFilter", (EntityB b, string val) => b.Name.StartsWith(val), () => "B");
                modelBuilder.Filter("EntityCFilter", (EntityC c) => "Joeseph".StartsWith(c.Name));
                modelBuilder.Filter("EntityDFilter", (EntityD d, string val) => val.StartsWith(d.Name), () => "Frederick");

                modelBuilder.Filter("EntityEFilter", (EntityE e) => e.Name.Contains("bar"));
                modelBuilder.Filter("EntityFFilter", (EntityF f, string val) => f.Name.Contains(val), () => "bar");
                modelBuilder.Filter("EntityGFilter", (EntityG g) => "barney rubble".Contains(g.Name));
                modelBuilder.Filter("EntityHFilter", (EntityH h, string val) => val.Contains(h.Name), () => "barney rubble");

                modelBuilder.Filter("EntityIFilter", (EntityI i) => i.Name.EndsWith("ney"));
                modelBuilder.Filter("EntityJFilter", (EntityJ j, string val) => j.Name.EndsWith(val), () => "ney");
                modelBuilder.Filter("EntityKFilter", (EntityK k) => "rubble, barney".EndsWith(k.Name));
                modelBuilder.Filter("EntityLFilter", (EntityL l, string val) => val.Contains(l.Name), () => "rubble, barney");
            }

            public override void Seed()
            {
                System.Diagnostics.Debug.Print("Seeding db");

                var names = new string[] { "John", "Joe", "Bob", "Barney", "Fred" };

                for (int i = 0; i < 5; i++)
                {
                    EntityASet.Add(new EntityA { ID = i + 1, Name = names[i] });
                    EntityBSet.Add(new EntityB { ID = i + 1, Name = names[i] });
                    EntityCSet.Add(new EntityC { ID = i + 1, Name = names[i] });
                    EntityDSet.Add(new EntityD { ID = i + 1, Name = names[i] });
                    EntityESet.Add(new EntityE { ID = i + 1, Name = names[i] });
                    EntityFSet.Add(new EntityF { ID = i + 1, Name = names[i] });
                    EntityGSet.Add(new EntityG { ID = i + 1, Name = names[i] });
                    EntityHSet.Add(new EntityH { ID = i + 1, Name = names[i] });
                    EntityISet.Add(new EntityI { ID = i + 1, Name = names[i] });
                    EntityJSet.Add(new EntityJ { ID = i + 1, Name = names[i] });
                    EntityKSet.Add(new EntityK { ID = i + 1, Name = names[i] });
                    EntityLSet.Add(new EntityL { ID = i + 1, Name = names[i] });
                }

                SaveChanges();
            }
        }

        #endregion
    }


}
