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
    //  Tests related to null comparisons and Nullable<T>.HasValue handling in lambda filters (issue #14)
    [TestClass]
    public class BitwiseOperatorTests
    {
        [TestMethod]
        public void BitwiseOperator_And()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityASet.ToList();
                Assert.IsTrue((list.Count == 3) && list.All(i => (i.ID == 1) || (i.ID == 3) || (i.ID == 5)));
            }
        }

        [TestMethod]
        public void BitwiseOperator_Or()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityBSet.ToList();
                Assert.IsTrue((list.Count == 3) && list.All(i => (i.ID >= 1) && (i.ID <= 3)));
            }
        }

        [TestMethod]
        public void BitwiseOperator_Xor()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(i => (i.ID == 2)));
            }
        }

        #region Models

        public class EntityBase
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }
        }

        public class EntityA : EntityBase
        {}

        public class EntityB : EntityBase
        { }

        public class EntityC : EntityBase
        { }

        #endregion

        #region TestContext

        public class TestContext : TestContextBase<TestContext>, ITestContext
        {
            public DbSet<EntityA> EntityASet { get; set; }
            public DbSet<EntityB> EntityBSet { get; set; }
            public DbSet<EntityC> EntityCSet { get; set; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Filter("EntityAFilter", (EntityA a) => (a.ID & 1) == 1);
                modelBuilder.Filter("EntityBFilter", (EntityB b) => (b.ID | 1) <= 3);
                modelBuilder.Filter("EntityCFilter", (EntityC c) => (c.ID ^ 1) == 3);
            }

            public override void Seed()
            {
                for (int i = 1; i <= 5; i++)
                {
                    EntityASet.Add(new EntityA { ID = i });
                    EntityBSet.Add(new EntityB { ID = i });
                    EntityCSet.Add(new EntityC { ID = i });
                }

                SaveChanges();
            }
        }

        #endregion
    }
}