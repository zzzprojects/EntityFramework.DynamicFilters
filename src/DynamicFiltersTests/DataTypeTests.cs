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
    public class DataTypeTests
    {
        [TestMethod]
        public void DataType_EnumEquals()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityASet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.FirstOrDefault().ID == 1));
            }
        }

        [TestMethod]
        public void DataType_EnumDynamicContains()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityBSet.ToList();
                Assert.IsTrue((list.Count == 2) && (list.All(b => (b.ID == 1) || (b.ID == 4))));
            }
        }

        [TestMethod]
        public void DataType_EnumConstantContains()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 2) && (list.All(b => (b.ID == 2) || (b.ID == 3))));
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

        public abstract class EntityWithEnumBase
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }

            public StatusEnum Status { get; set; }
        }

        public class EntityA : EntityWithEnumBase
        { }

        public class EntityB : EntityWithEnumBase
        { }

        public class EntityC : EntityWithEnumBase
        { }

        #endregion

        #region TestContext

        public class TestContext : DbContext
        {
            public DbSet<EntityA> EntityASet { get; set; }
            public DbSet<EntityB> EntityBSet { get; set; }
            public DbSet<EntityC> EntityCSet { get; set; }

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

                modelBuilder.Filter("EntityAFilter", (EntityA a, StatusEnum status) => a.Status == status, () => StatusEnum.Active);

                modelBuilder.Filter("EntityBFilter", (EntityB b, List<StatusEnum> statusList) => statusList.Contains(b.Status), () => new List<StatusEnum> { StatusEnum.Active, StatusEnum.Archived });

                modelBuilder.Filter("EntityCFilter", (EntityC c) => (new List<StatusEnum> { StatusEnum.Inactive, StatusEnum.Deleted }).Contains(c.Status));
            }
        }

        public class ContentInitializer<T> : DropCreateDatabaseAlways<T>
            where T : TestContext
        {
            protected override void Seed(T context)
            {
                System.Diagnostics.Debug.Print("Seeding db");

                context.EntityASet.Add(new EntityA { ID = 1, Status = StatusEnum.Active });
                context.EntityASet.Add(new EntityA { ID = 2, Status = StatusEnum.Inactive });
                context.EntityASet.Add(new EntityA { ID = 3, Status = StatusEnum.Deleted });
                context.EntityASet.Add(new EntityA { ID = 4, Status = StatusEnum.Archived });

                context.EntityBSet.Add(new EntityB { ID = 1, Status = StatusEnum.Active });
                context.EntityBSet.Add(new EntityB { ID = 2, Status = StatusEnum.Inactive });
                context.EntityBSet.Add(new EntityB { ID = 3, Status = StatusEnum.Deleted });
                context.EntityBSet.Add(new EntityB { ID = 4, Status = StatusEnum.Archived });

                context.EntityCSet.Add(new EntityC { ID = 1, Status = StatusEnum.Active });
                context.EntityCSet.Add(new EntityC { ID = 2, Status = StatusEnum.Inactive });
                context.EntityCSet.Add(new EntityC { ID = 3, Status = StatusEnum.Deleted });
                context.EntityCSet.Add(new EntityC { ID = 4, Status = StatusEnum.Archived });

                context.SaveChanges();
            }
        }

        #endregion
    }


}
