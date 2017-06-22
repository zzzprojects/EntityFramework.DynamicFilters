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
    //  Tests for entities that have filter on a child property for issue #100
    //  https://github.com/jcachat/EntityFramework.DynamicFilters/issues/100
    //  This was a known issue in v1.4 because child properties were not available when using SSpace.
    //  In Oracle 11, it does not support the EF Element() method so even in v2, it was using SSpace
    //  and throwing an error (saying "Property x not found in Entity Type y".
    //  Oracle 12 supports Element() now so this test ensures that this works correctly for everything other
    //  than Oracle 11.

    [TestClass]
    public class NavigationPropertyTests2
    {

        [TestMethod]
        public void NavigationPropertyTests_Oracle12PropertyFilter()
        {
            using (var context = new TestContext())
            {
                List<T_TABLE_A> list;
                try
                {
                    list = context.TABLE_A.Where(x => x.TABLE_BS.Any()).ToList();
                }
                catch
                {
                    if (context.OracleVersion()?.Major < 12)     //  If this is true, we are connected to Oracle 11 (or older)
                    {
                        //  Exception is expected for Oracle 11 so eat it
                        return;
                    }
                    throw;
                }

                Assert.IsTrue(list.Count == 1);
            }
        }

        #region Models

        public enum EStatus
        {
            Active = 1,
            Inactive = 2,
            Deleted = 3
        }

        [Table("T_STATUS")]
        public class T_STATUS
        {
            [Key]
            public EStatus STATUS_ID { get; set; }

            public DateTime CREATEDON { get; set; }
        }

        [Table("T_TABLE_A")]
        public class T_TABLE_A
        {
            public T_TABLE_A()
            {
                this.TABLE_BS = new HashSet<T_TABLE_B>();
            }

            [Key]
            public int TABLE_A_ID { get; set; }

            [InverseProperty("TABLE_A")]
            public ICollection<T_TABLE_B> TABLE_BS { get; set; }

            public DateTime CREATEDON { get; set; }
        }

        [Table("T_TABLE_B")]
        public class T_TABLE_B
        {
            [Key]
            public int TABLE_B_ID { get; set; }

            public int TABLE_A_ID { get; set; }

            public int TABLE_C_ID { get; set; }

            [ForeignKey("TABLE_A_ID")]
            public T_TABLE_A TABLE_A { get; set; }

            [ForeignKey("TABLE_C_ID")]
            public T_TABLE_C TABLE_C { get; set; }

            public DateTime CREATEDON { get; set; }
        }

        [Table("T_TABLE_C")]
        public class T_TABLE_C
        {
            public T_TABLE_C()
            {
                this.TABLE_BS = new HashSet<T_TABLE_B>();
            }

            [Key]
            public int TABLE_C_ID { get; set; }

            public EStatus STATUS_ID { get; set; }

            [ForeignKey("STATUS_ID")]
            public T_STATUS STATUS { get; set; }

            [InverseProperty("TABLE_C")]
            public ICollection<T_TABLE_B> TABLE_BS { get; set; }

            public DateTime CREATEDON { get; set; }
        }

        #endregion

        #region TestContext

        public class TestContext : TestContextBase<TestContext>, ITestContext
        {
            public virtual DbSet<T_TABLE_A> TABLE_A { get; set; }
            public virtual DbSet<T_TABLE_B> TABLE_B { get; set; }
            public virtual DbSet<T_TABLE_C> TABLE_C { get; set; }
            public virtual DbSet<T_STATUS> STATUS { get; set; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Filter("FilterName", (T_TABLE_B b) => b.TABLE_C.STATUS_ID == EStatus.Active);
            }

            public override void Seed()
            {
                System.Diagnostics.Debug.Print("Seeding db");

                T_STATUS Active = new T_STATUS { STATUS_ID = EStatus.Active, CREATEDON = DateTime.Now };
                T_STATUS Inactive = new T_STATUS { STATUS_ID = EStatus.Inactive, CREATEDON = DateTime.Now };
                T_STATUS Deleted = new T_STATUS { STATUS_ID = EStatus.Deleted, CREATEDON = DateTime.Now };
                STATUS.Add(Active);
                STATUS.Add(Inactive);
                STATUS.Add(Deleted);

                T_TABLE_A FirstA = new T_TABLE_A { CREATEDON = DateTime.Now };
                T_TABLE_A SecondA = new T_TABLE_A { CREATEDON = DateTime.Now };
                TABLE_A.Add(FirstA);
                TABLE_A.Add(SecondA);

                T_TABLE_C FirstC = new T_TABLE_C { STATUS_ID = EStatus.Inactive, CREATEDON = DateTime.Now };
                T_TABLE_C SecondC = new T_TABLE_C { STATUS_ID = EStatus.Active, CREATEDON = DateTime.Now };
                TABLE_C.Add(FirstC);
                TABLE_C.Add(SecondC);

                T_TABLE_B FirstB = new T_TABLE_B { TABLE_A = FirstA, TABLE_C = FirstC, CREATEDON = DateTime.Now };
                T_TABLE_B SecondB = new T_TABLE_B { TABLE_A = SecondA, TABLE_C = SecondC, CREATEDON = DateTime.Now };
                TABLE_B.Add(FirstB);
                TABLE_B.Add(SecondB);

                SaveChanges();
            }
        }

        #endregion
    }
}
