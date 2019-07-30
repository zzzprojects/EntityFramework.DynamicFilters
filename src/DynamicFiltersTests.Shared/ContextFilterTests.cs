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
    /// Tests for support of various data types
    /// </summary>
    [TestClass]
    public class ContextFilterTests
    {
        [TestMethod]
        public void ContextFilter_NotDeleted()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityASet.ToList();
                Assert.IsTrue((list.Count == 2) && (list.All(a => (a.ID == 1) || (a.ID == 2))));
            }
        }

        [TestMethod]
        public void ContextFilter_Deleted()
        {
            using (var context1 = new TestContext())
            {
                context1.IsDeleted = true;
                var list = context1.EntityASet.ToList();
                Assert.IsTrue((list.Count == 2) && (list.All(a => (a.ID == 3) || (a.ID == 4))));
            }
        }

        [TestMethod]
        public void ContextFilter_StatusAndNotDeleted()
        {
            using (var context1 = new TestContext())
            {
                context1.Status = StatusEnum.Active;
                context1.IsDeleted = false;

                var list = context1.EntityBSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 1))));
            }
        }

        [TestMethod]
        public void ContextFilter_StatusAndDeleted()
        {
            using (var context1 = new TestContext())
            {
                context1.Status = StatusEnum.Deleted;
                context1.IsDeleted = true;

                var list = context1.EntityBSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 7))));
            }
        }

        [TestMethod]
        public void ContextFilter_NoStatusAndDeleted()
        {
            using (var context1 = new TestContext())
            {
                context1.Status = null;
                context1.IsDeleted = true;

                var list = context1.EntityBSet.ToList();
                Assert.IsTrue((list.Count == 4) && (list.All(a => (a.ID >= 5) && (a.ID <= 8))));
            }
        }

        [TestMethod]
        public void ContextFilter_SetScopedParameterValueFunc()
        {
            using (var context = new TestContext())
            {
                context.Status = StatusEnum.Inactive;
                context.Status2 = StatusEnum.Archived;

                var list = context.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 1))));

                context.SetFilterScopedParameterValue("EntityCFilter", (TestContext ctx) => ctx.Status.Value);
                list = context.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 2))));

                context.SetFilterScopedParameterValue("EntityCFilter", "status", (TestContext ctx) => ctx.Status2);
                list = context.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 4))));
            }
        }

        [TestMethod]
        public void ContextFilter_SetGlobalParameterValueFunc()
        {
            using (var context = new TestContext())
            {
                context.Status = StatusEnum.Inactive;
                context.Status2 = StatusEnum.Archived;

                var list = context.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 1))));

                context.SetFilterGlobalParameterValue("EntityCFilter", (TestContext ctx) => ctx.Status.Value);
                list = context.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 2))));

                context.SetFilterGlobalParameterValue("EntityCFilter", "status", (TestContext ctx) => ctx.Status2);
                list = context.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 4))));
            }
        }

        [TestMethod]
        public void ContextFilter_SinglePropEquality()
        {
            using (var context1 = new TestContext())
            {
                context1.Status = StatusEnum.Archived;

                var list = context1.EntityDSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(a => (a.ID == 4))));
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

        public interface ISoftDelete
        {
            bool IsDeleted { get; set; }
        }

        public abstract class EntityBase : ISoftDelete
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }

            public StatusEnum Status { get; set; }

            public bool IsDeleted { get; set; }
        }

        public class EntityA : EntityBase
        { }

        public class EntityB : EntityBase
        { }

        public class EntityC : EntityBase
        { }

        public class EntityD : EntityBase
        { }

        #endregion

        #region TestContext

        public class TestContext : TestContextBase<TestContext>, ITestContext
        {
            public DbSet<EntityA> EntityASet { get; set; }
            public DbSet<EntityB> EntityBSet { get; set; }
            public DbSet<EntityC> EntityCSet { get; set; }
            public DbSet<EntityD> EntityDSet { get; set; }

            public StatusEnum? Status { get; set; }
            public StatusEnum Status2 { get; set; }
            public bool IsDeleted { get; set; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Filter("IsDeleted", (ISoftDelete e, bool isDeleted) => e.IsDeleted == isDeleted, (TestContext ctx) => ctx.IsDeleted);

                modelBuilder.Filter("EntityBFilter", (EntityB b, StatusEnum? status) => !status.HasValue || b.Status == status.Value, (TestContext ctx) => ctx.Status);
                modelBuilder.Filter("EntityCFilter", (EntityC c, StatusEnum status) => c.Status == status, StatusEnum.Active);
                modelBuilder.Filter("EntityDFilter", (EntityD d) => d.Status, (TestContext ctx) => ctx.Status);
            }

            public override void Seed()
            {
                System.Diagnostics.Debug.Print("Seeding db");

                EntityASet.Add(new EntityA { ID = 1, Status = StatusEnum.Active });
                EntityASet.Add(new EntityA { ID = 2, Status = StatusEnum.Inactive });
                EntityASet.Add(new EntityA { ID = 3, Status = StatusEnum.Deleted, IsDeleted = true });
                EntityASet.Add(new EntityA { ID = 4, Status = StatusEnum.Archived, IsDeleted = true });

                EntityBSet.Add(new EntityB { ID = 1, Status = StatusEnum.Active });
                EntityBSet.Add(new EntityB { ID = 2, Status = StatusEnum.Inactive });
                EntityBSet.Add(new EntityB { ID = 3, Status = StatusEnum.Deleted });
                EntityBSet.Add(new EntityB { ID = 4, Status = StatusEnum.Archived });
                EntityBSet.Add(new EntityB { ID = 5, Status = StatusEnum.Active, IsDeleted = true });
                EntityBSet.Add(new EntityB { ID = 6, Status = StatusEnum.Inactive, IsDeleted = true });
                EntityBSet.Add(new EntityB { ID = 7, Status = StatusEnum.Deleted, IsDeleted = true });
                EntityBSet.Add(new EntityB { ID = 8, Status = StatusEnum.Archived, IsDeleted = true });

                EntityCSet.Add(new EntityC { ID = 1, Status = StatusEnum.Active });
                EntityCSet.Add(new EntityC { ID = 2, Status = StatusEnum.Inactive });
                EntityCSet.Add(new EntityC { ID = 3, Status = StatusEnum.Deleted});
                EntityCSet.Add(new EntityC { ID = 4, Status = StatusEnum.Archived });

                EntityDSet.Add(new EntityD { ID = 1, Status = StatusEnum.Active });
                EntityDSet.Add(new EntityD { ID = 2, Status = StatusEnum.Inactive });
                EntityDSet.Add(new EntityD { ID = 3, Status = StatusEnum.Deleted });
                EntityDSet.Add(new EntityD { ID = 4, Status = StatusEnum.Archived });

                SaveChanges();
            }
        }

        #endregion
    }


}
