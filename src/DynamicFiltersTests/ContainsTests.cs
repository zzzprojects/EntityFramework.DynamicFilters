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
    public class ContainsTests
    {
        [TestMethod]
        public void Contains_ConstantIntList()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityASet.ToList();
                Assert.IsTrue((list.Count == 5) && list.All(a => (a.ID == 2) || (a.ID == 4) || (a.ID == 6) || (a.ID == 8) || (a.ID == 10)));
            }
        }

        [TestMethod]
        public void Contains_DynamicIntList()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityBSet.ToList();
                Assert.IsTrue((list.Count == 5) && list.All(b => (b.ID >= 1) && (b.ID <= 5)));
            }
        }

        [TestMethod]
        public void Contains_IntListWithParameterItems()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 5) && list.All(b => (b.ID >= 6) && (b.ID <= 10)));
            }
        }

        [TestMethod]
        public void Contains_ConstantNullableIntList()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityDSet.ToList();
                Assert.IsTrue((list.Count == 5) && list.All(d => (d.IntValue.Value == 2) || (d.IntValue.Value == 4) || (d.IntValue.Value == 6) || (d.IntValue.Value == 8) || (d.IntValue.Value == 10)));
            }
        }

        [TestMethod]
        public void Contains_DynamicNullableIntList()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityESet.ToList();

                //  Note: SQL Server does not return records using "in (null)" syntax...
                Assert.IsTrue((list.Count == 4) && list.All(e => ((e.IntValue.Value >= 1) && (e.IntValue.Value <= 4))));
            }
        }

        [TestMethod]
        public void Contains_NullableIntListWithParameterItems()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityFSet.ToList();
                Assert.IsTrue((list.Count == 5) && list.All(f => (f.IntValue.Value >= 6) && (f.IntValue.Value <= 10)));
            }
        }

        [TestMethod]
        public void Contains_DynamicStringList()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityGSet.ToList();
                Assert.IsTrue((list.Count == 5) && list.All(g => (g.StrValue == "1") || (g.StrValue == "2") || (g.StrValue == "3") || (g.StrValue == "4") || (g.StrValue == "5")));
            }
        }

        [TestMethod]
        public void Contains_DynamicBoolList()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityHSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(g => (g.BoolValue == false)));
            }
        }

        [TestMethod]
        public void Contains_DynamicGuidList()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityISet.ToList();
                Assert.IsTrue((list.Count == 2) && list.All(i => (i.GuidValue == Guid.Parse("3A298D91-3857-E411-829F-001C428D83FF")) || (i.GuidValue == Guid.Parse("3B298D91-3857-E411-829F-001C428D83FF"))));
            }
        }

        [TestMethod]
        public void Contains_DynamicDateList()
        {
            using (var context = new TestContext())
            {
                //  The Oracle EF driver stores a DateTime as a DATE type and then throws an error saying
                //  "The member with identity 'Precision' does not exist in the metadata collection" if you try to use it!
                //  Only DateTimeOffset works in Oracle (which maps to a TIMESTAMP type)
                if (context.IsOracle)
                    return;

                var list = context.EntityJSet.ToList();
                Assert.IsTrue((list.Count == 3) && list.All(j => (j.DateValue == new DateTime(2015, 1, 1) || (j.DateValue == new DateTime(2015, 1, 2, 12, 34, 56, 790)) || (j.DateValue == new DateTime(2015, 1, 3)))));
            }
        }

        [TestMethod]
        public void Contains_DynamicDateTimeOffsetList()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityKSet.ToList();
                Assert.IsTrue((list.Count == 3) && list.All(j => (j.DateValue == new DateTime(2015, 1, 1) || (j.DateValue == new DateTime(2015, 1, 2, 12, 34, 56, 790)) || (j.DateValue == new DateTime(2015, 1, 3)))));
            }
        }

        /// <summary>
        /// Tests issue #31.  Multiple entities being filtered on the same Contains() filter in the same query.
        /// </summary>
        [TestMethod]
        public void Contains_MultipleEntities()
        {
            using (var context = new TestContext())
            {
                var list = context.TenantEntityASet.Include(e => e.EntityBList).ToList();
                Assert.IsTrue((list.Count == 1) && (list.All(e => e.ID == 1)));

                var entityBList = list.FirstOrDefault().EntityBList;
                Assert.IsTrue((entityBList.Count == 3) && (list.All(b => (b.ID == 1) || (b.ID == 2) || (b.ID == 3))));
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
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int ID { get; set; }

            public int? IntValue { get; set; }
        }

        public class EntityE
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int ID { get; set; }

            public int? IntValue { get; set; }
        }

        public class EntityF
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int ID { get; set; }

            public int? IntValue { get; set; }
        }

        public class EntityG
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int ID { get; set; }

            [MaxLength(100)] //  Must set MaxLength or Oracle will set column datatype to NCLOB which will then fail comparisons against a string/nvarchar!
            public string StrValue { get; set; }
        }

        public class EntityH
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int ID { get; set; }

            public bool BoolValue { get; set; }
        }

        public class EntityI
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int ID { get; set; }

            public Guid GuidValue { get; set; }
        }

        public class EntityJ
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int ID { get; set; }

            public DateTime DateValue { get; set; }
        }

        public class EntityK
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int ID { get; set; }

            public DateTimeOffset DateValue { get; set; }
        }

        public interface ITenant
        {
            Guid TenantID { get; set; }
        }

        public class TenantEntityA : ITenant
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int ID { get; set; }

            public Guid TenantID { get; set; }

            public ICollection<TenantEntityB> EntityBList { get; set; }
        }

        public class TenantEntityB : ITenant
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int ID { get; set; }

            public Guid TenantID { get; set; }

            public TenantEntityA EntityA { get; set; }
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
            public DbSet<EntityH> EntityHSet { get; set; }
            public DbSet<EntityI> EntityISet { get; set; }
            public DbSet<EntityJ> EntityJSet { get; set; }
            public DbSet<EntityK> EntityKSet { get; set; }
            public DbSet<TenantEntityA> TenantEntityASet { get; set; }
            public DbSet<TenantEntityB> TenantEntityBSet { get; set; }

            public static Guid TenantID1 = Guid.NewGuid();
            public static Guid TenantID2 = Guid.NewGuid();

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                //  Constant list filter
                modelBuilder.Filter("EntityAFilter", (EntityA a) => (new List<int> { 2, 4, 6, 8, 10 }).Contains(a.ID));

                //  Dynamic int list filter
                modelBuilder.Filter("EntityBFilter", (EntityB b, List<int> valueList) => valueList.Contains(b.ID), () => new List<int> { 1, 2, 3, 4, 5 });

                //  Constant list that has 2 item values that are parmeterized
                modelBuilder.Filter("EntityCFilter", (EntityC c, int val1, int val2) => (new List<int> { val1, val2, 8, 9, 10 }).Contains(c.ID), () => 6, () => 7);

                //  Constant list filter on nullable int
                //  Note that a null value cannot be passed in a constant list. as EF only supports constants (and not null) in a DbInExpression
                modelBuilder.Filter("EntityDFilter", (EntityD d) => (new List<int?> { 2, 4, 6, 8, 10 }).Contains(d.IntValue.Value));

                //  Dynamic int list filter on nullable int
                //  Note: SQL Server does not return records using "in (null)" syntax...
                modelBuilder.Filter("EntityEFilter", (EntityE e, List<int?> valueList) => valueList.Contains(e.IntValue.Value), () => new List<int?> { 1, 2, 3, 4, null });

                //  Constant nullable list that has 2 item values that are parmeterized
                modelBuilder.Filter("EntityFFilter", (EntityF f, int? val1, int? val2) => (new List<int?> { val1, val2, 8, 9, 10 }).Contains(f.IntValue.Value), () => 6, () => 7);

                modelBuilder.Filter("EntityGFilter", (EntityG g, List<string> valueList) => valueList.Contains(g.StrValue), () => new List<string> { "1", "2", "3", "4", "5" });

                modelBuilder.Filter("EntityHFilter", (EntityH h, List<bool> valueList) => valueList.Contains(h.BoolValue), () => new List<bool> { false });

                modelBuilder.Filter("EntityIFilter", (EntityI i, List<Guid> valueList) => valueList.Contains(i.GuidValue), () => new List<Guid> { Guid.Parse("3A298D91-3857-E411-829F-001C428D83FF"), Guid.Parse("3B298D91-3857-E411-829F-001C428D83FF") });

                modelBuilder.Filter("EntityJFilter", (EntityJ j, List<DateTime> valueList) => valueList.Contains(j.DateValue), () => new List<DateTime> { new DateTime(2015, 1, 1), new DateTime(2015, 1, 2, 12, 34, 56, 790), new DateTime(2015, 1, 3) });

                modelBuilder.Filter("EntityKFilter", (EntityK k, List<DateTimeOffset> valueList) => valueList.Contains(k.DateValue), () => new List<DateTimeOffset> { new DateTimeOffset(new DateTime(2015, 1, 1)), new DateTimeOffset(new DateTime(2015, 1, 2, 12, 34, 56, 790)), new DateTimeOffset(new DateTime(2015, 1, 3)) });

                modelBuilder.Filter("TentantEntityFilter", (ITenant t, List<Guid> tenantIDList) => tenantIDList.Contains(t.TenantID), () => new List<Guid> { TestContext.TenantID1, TestContext.TenantID2 });
            }

            public override void Seed()
            {
                System.Diagnostics.Debug.Print("Seeding db");

                for (int i = 1; i <= 10; i++)
                {
                    EntityASet.Add(new EntityA { ID = i });
                    EntityBSet.Add(new EntityB { ID = i });
                    EntityCSet.Add(new EntityC { ID = i });
                    EntityDSet.Add(new EntityD { IntValue = i });
                    EntityESet.Add(new EntityE { IntValue = i });
                    EntityFSet.Add(new EntityF { IntValue = i });
                    EntityGSet.Add(new EntityG { StrValue = i.ToString() });
                }

                //  Note: SQL Server does not return records using "in (null)" syntax...
                EntityESet.Add(new EntityE());      //  For a null IntValue record

                EntityHSet.Add(new EntityH { BoolValue = true });
                EntityHSet.Add(new EntityH { BoolValue = false });

                EntityISet.Add(new EntityI { GuidValue = Guid.Parse("3A298D91-3857-E411-829F-001C428D83FF") });
                EntityISet.Add(new EntityI { GuidValue = Guid.Parse("3B298D91-3857-E411-829F-001C428D83FF") });
                EntityISet.Add(new EntityI { GuidValue = Guid.NewGuid() });
                EntityISet.Add(new EntityI { GuidValue = Guid.NewGuid() });

                EntityJSet.Add(new EntityJ { DateValue = new DateTime(2015, 1, 1) });
                EntityJSet.Add(new EntityJ { DateValue = new DateTime(2015, 1, 2, 12, 34, 56, 790) });
                EntityJSet.Add(new EntityJ { DateValue = new DateTime(2015, 1, 3) });
                EntityJSet.Add(new EntityJ { DateValue = DateTime.Now });
                EntityJSet.Add(new EntityJ { DateValue = DateTime.Now.AddDays(7) });

                EntityKSet.Add(new EntityK { DateValue = new DateTime(2015, 1, 1) });
                EntityKSet.Add(new EntityK { DateValue = new DateTime(2015, 1, 2, 12, 34, 56, 790) });
                EntityKSet.Add(new EntityK { DateValue = new DateTime(2015, 1, 3) });
                EntityKSet.Add(new EntityK { DateValue = DateTime.Now });
                EntityKSet.Add(new EntityK { DateValue = DateTime.Now.AddDays(7) });

                var tenantID2 = Guid.NewGuid();
                TenantEntityASet.Add(new TenantEntityA
                {
                    ID = 1,
                    TenantID = TestContext.TenantID1,
                    EntityBList = new List<TenantEntityB>()
                    {
                        new TenantEntityB { ID = 1, TenantID = TestContext.TenantID1 },
                        new TenantEntityB { ID = 2, TenantID = TestContext.TenantID1 },
                        new TenantEntityB { ID = 3, TenantID = TestContext.TenantID2 },
                        new TenantEntityB { ID = 4, TenantID = tenantID2 },
                        new TenantEntityB { ID = 5, TenantID = tenantID2 },
                        new TenantEntityB { ID = 6, TenantID = tenantID2 }
                    }
                });
                TenantEntityASet.Add(new TenantEntityA
                {
                    ID = 2,
                    TenantID = tenantID2,
                    EntityBList = new List<TenantEntityB>()
                    {
                        new TenantEntityB { ID = 10, TenantID = TestContext.TenantID1 },
                        new TenantEntityB { ID = 11, TenantID = TestContext.TenantID2 },
                        new TenantEntityB { ID = 12, TenantID = TestContext.TenantID2 },
                        new TenantEntityB { ID = 13, TenantID = tenantID2 },
                        new TenantEntityB { ID = 14, TenantID = tenantID2 },
                        new TenantEntityB { ID = 15, TenantID = tenantID2 }
                    }
                });

                SaveChanges();
            }
        }

        #endregion
    }


}
