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
    /// Tests for support of property values embedded inside the linq filters (not as parameters).
    /// *** These will be evaluated when the query is compiled and then used as a constant value.
    /// *** They will not be re-evaluated each time the query is executed!
    /// See https://github.com/jcachat/EntityFramework.DynamicFilters/issues/109
    /// </summary>
    [TestClass]
    public class PropertiesInExpressionTests
    {
        [TestMethod]
        public void PropertiesInExpression_LocalIntValue()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityASet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 1))));
            }
        }

        [TestMethod]
        public void PropertiesInExpression_ClassIntFieldValue()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityBSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 1))));
            }
        }

        [TestMethod]
        public void PropertiesInExpression_ClassIntPropertyValue()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 1))));
            }
        }

        [TestMethod]
        public void PropertiesInExpression_StaticIntFieldValue()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityDSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 1))));
            }
        }

        [TestMethod]
        public void PropertiesInExpression_StaticIntPropertyValue()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityESet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 1))));
            }
        }

        [TestMethod]
        public void PropertiesInExpression_ContextIntMethod()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityFSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 1))));
            }
        }

        [TestMethod]
        public void PropertiesInExpression_StaticIntMethod()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityGSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 1))));
            }
        }

        #region Models

        public enum StatusEnum
        {
            Active = 0,
            Inactive = 1,
            Deleted = 2,
            Archived = 3,
        }

        public abstract class EntityBase
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }

            public StatusEnum Status { get; set; }
        }

        public class EntityA : EntityBase
        { }

        public class EntityB : EntityBase
        { }

        public class EntityC : EntityBase
        { }

        public class EntityD : EntityBase
        { }

        public class EntityE : EntityBase
        { }

        public class EntityF : EntityBase
        { }

        public class EntityG : EntityBase
        { }

        public static class StaticFilterParamContainer
        {
            public static class FilterValues
            {
                public static readonly int IDField = 1;
                public static int IDProperty { get; set; } = 1;
                public static int IDMethod() { return 1; }
            }
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
            public DbSet<EntityF> EntityFSet { get; set; }
            public DbSet<EntityG> EntityGSet { get; set; }

            private int FilterIDField = 1;
            private int FilterIDProperty { get; set; } = 1;
            private int FilterMethod() { return 1; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                int id = 1;
                modelBuilder.Filter("EntityAFilter", (EntityA a) => a.ID == id);

                modelBuilder.Filter("EntityBFilter", (EntityB b) => b.ID == FilterIDField);
                modelBuilder.Filter("EntityCFilter", (EntityC c) => c.ID == FilterIDProperty);

                modelBuilder.Filter("EntityDFilter", (EntityD d) => d.ID == StaticFilterParamContainer.FilterValues.IDField);
                modelBuilder.Filter("EntityEFilter", (EntityE e) => e.ID == StaticFilterParamContainer.FilterValues.IDProperty);

                modelBuilder.Filter("EntityFFilter", (EntityF f) => f.ID == FilterMethod());
                modelBuilder.Filter("EntityGFilter", (EntityG g) => g.ID == StaticFilterParamContainer.FilterValues.IDMethod());
            }

            public override void Seed()
            {
                System.Diagnostics.Debug.Print("Seeding db");

                EntityASet.Add(new EntityA { ID = 1, Status = StatusEnum.Active });
                EntityASet.Add(new EntityA { ID = 2, Status = StatusEnum.Inactive });

                EntityBSet.Add(new EntityB { ID = 1, Status = StatusEnum.Active });
                EntityBSet.Add(new EntityB { ID = 2, Status = StatusEnum.Inactive });

                EntityCSet.Add(new EntityC { ID = 1, Status = StatusEnum.Active });
                EntityCSet.Add(new EntityC { ID = 2, Status = StatusEnum.Inactive });

                EntityDSet.Add(new EntityD { ID = 1, Status = StatusEnum.Active });
                EntityDSet.Add(new EntityD { ID = 2, Status = StatusEnum.Inactive });

                EntityESet.Add(new EntityE { ID = 1, Status = StatusEnum.Active });
                EntityESet.Add(new EntityE { ID = 2, Status = StatusEnum.Inactive });

                EntityFSet.Add(new EntityF { ID = 1, Status = StatusEnum.Active });
                EntityFSet.Add(new EntityF { ID = 2, Status = StatusEnum.Inactive });

                EntityGSet.Add(new EntityG { ID = 1, Status = StatusEnum.Active });
                EntityGSet.Add(new EntityG { ID = 2, Status = StatusEnum.Inactive });

                SaveChanges();
            }
        }

        #endregion
    }
}
