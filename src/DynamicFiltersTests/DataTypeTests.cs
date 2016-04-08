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

        [TestMethod]
        public void DataType_BoolTrue()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityDSet.ToList();
                Assert.IsTrue((list.Count == 2) && (list.All(b => (b.ID == 1) || (b.ID == 2))));
            }
        }

        [TestMethod]
        public void DataType_NotBool()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityESet.ToList();
                Assert.IsTrue((list.Count == 2) && (list.All(b => (b.ID == 1) || (b.ID == 2))));
            }
        }

        [TestMethod]
        public void DataType_BoolEqualsParam()
        {
            using (var context1 = new TestContext())
            {
                var list = context1.EntityFSet.ToList();
                Assert.IsTrue((list.Count == 2) && (list.All(b => (b.ID == 1) || (b.ID == 2))));
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

        public class EntityD
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }

            public bool Flag { get; set; }
        }

        public class EntityE
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }

            public bool Flag { get; set; }
        }

        public class EntityF
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }

            public bool Flag { get; set; }
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

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Filter("EntityAFilter", (EntityA a, StatusEnum status) => a.Status == status, () => StatusEnum.Active);

                modelBuilder.Filter("EntityBFilter", (EntityB b, List<StatusEnum> statusList) => statusList.Contains(b.Status), () => new List<StatusEnum> { StatusEnum.Active, StatusEnum.Archived });

                modelBuilder.Filter("EntityCFilter", (EntityC c) => (new List<StatusEnum> { StatusEnum.Inactive, StatusEnum.Deleted }).Contains(c.Status));

                modelBuilder.Filter("EntityDFilter", (EntityD d) => d.Flag);
                modelBuilder.Filter("EntityEFilter", (EntityE e) => !e.Flag);
                modelBuilder.Filter("EntityFFilter", (EntityF f) => f.Flag, false);
            }

            public override void Seed()
            {
                System.Diagnostics.Debug.Print("Seeding db");

                EntityASet.Add(new EntityA { ID = 1, Status = StatusEnum.Active });
                EntityASet.Add(new EntityA { ID = 2, Status = StatusEnum.Inactive });
                EntityASet.Add(new EntityA { ID = 3, Status = StatusEnum.Deleted });
                EntityASet.Add(new EntityA { ID = 4, Status = StatusEnum.Archived });

                EntityBSet.Add(new EntityB { ID = 1, Status = StatusEnum.Active });
                EntityBSet.Add(new EntityB { ID = 2, Status = StatusEnum.Inactive });
                EntityBSet.Add(new EntityB { ID = 3, Status = StatusEnum.Deleted });
                EntityBSet.Add(new EntityB { ID = 4, Status = StatusEnum.Archived });

                EntityCSet.Add(new EntityC { ID = 1, Status = StatusEnum.Active });
                EntityCSet.Add(new EntityC { ID = 2, Status = StatusEnum.Inactive });
                EntityCSet.Add(new EntityC { ID = 3, Status = StatusEnum.Deleted });
                EntityCSet.Add(new EntityC { ID = 4, Status = StatusEnum.Archived });

                EntityDSet.Add(new EntityD { ID = 1, Flag = true });
                EntityDSet.Add(new EntityD { ID = 2, Flag = true });
                EntityDSet.Add(new EntityD { ID = 3, Flag = false });

                EntityESet.Add(new EntityE { ID = 1, Flag = false });
                EntityESet.Add(new EntityE { ID = 2, Flag = false });
                EntityESet.Add(new EntityE { ID = 3, Flag = true });

                EntityFSet.Add(new EntityF { ID = 1, Flag = false });
                EntityFSet.Add(new EntityF { ID = 2, Flag = false });
                EntityFSet.Add(new EntityF { ID = 3, Flag = true });

                SaveChanges();
            }
        }

        #endregion
    }


}
