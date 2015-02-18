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
    public class NullTests
    {
        /// <summary>
        /// Tests a filter that contains an expression (a.DeleteTimestamp == null)
        /// which needs to be translated into "is null" in SQL not "= null".
        /// </summary>
        [TestMethod]
        public void NullComparison_EqualsNull()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityASet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.FirstOrDefault().ID == 1));
            }
        }

        /// <summary>
        /// Tests a filter that contains an expression (a.DeleteTimestamp != null)
        /// which needs to be translated into "is not null" in SQL not "<> null".
        /// </summary>
        [TestMethod]
        public void NullComparison_NotEqualsNull()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityBSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.FirstOrDefault().ID == 2));
            }
        }

        /// <summary>
        /// Tests a filter that contains an expression (!a.DeleteTimestamp.HasValue)
        /// which needs to be translated into "is null".
        /// </summary>
        [TestMethod]
        public void NullableType_HasValue_NotHasValue()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityCSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.FirstOrDefault().ID == 1));
            }
        }

        /// <summary>
        /// Tests a filter that contains an expression a.DeleteTimestamp.HasValue
        /// which needs to be translated into "is not null".
        /// </summary>
        [TestMethod]
        public void NullableType_HasValue()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityDSet.ToList();
                Assert.IsTrue((list.Count == 1) && (list.FirstOrDefault().ID == 2));
            }
        }

        #region Models

        public class EntityBase
        {
            public EntityBase()
            {
                CreateTimestamp = DateTime.Now;
            }

            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }

            public DateTime CreateTimestamp { get; set; }
            public DateTime? DeleteTimestamp { get; set; }
        }

        public class EntityA : EntityBase
        {}

        public class EntityB : EntityBase
        {}

        public class EntityC : EntityBase
        { }

        public class EntityD : EntityBase
        { }

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

                //  Filter to test translating "a.DeleteTimestamp == null" into "DeleteTimestamp is null"
                modelBuilder.Filter("EntityAFilter",
                    (EntityA a, DateTime dt) => ((a.CreateTimestamp <= dt) && ((null == a.DeleteTimestamp) || (a.DeleteTimestamp > dt))),
                    () => DateTime.Now);

                //  Filter to test translating "b.DeleteTimestamp != null" into "not (DeleteTimestamp is null)"
                //  or (hopefully) "DeleteTimestamp is not null"
                modelBuilder.Filter("EntityBFilter", (EntityB b) => (b.DeleteTimestamp != null));

                //  Filter to test translating "!a.DeleteTimestamp.HasValue" into "DeleteTimestamp is null"
                modelBuilder.Filter("EntityCFilter",
                    (EntityC c, DateTime dt) => ((c.CreateTimestamp <= dt) && (!c.DeleteTimestamp.HasValue || (c.DeleteTimestamp > dt))),
                    () => DateTime.Now);

                //  Filter to test translating "b.DeleteTimestamp.HasValue" into "not (DeleteTimestamp is null)"
                //  or (hopefully) "DeleteTimestamp is not null".
                //  This filter also required special handling when cerating the filter to properly detect that this
                //  is a property accessor (and thus a lambda/predicate filter) and not a column name filter.
                modelBuilder.Filter("EntityDFilter", (EntityD d) => d.DeleteTimestamp.HasValue);
            }
        }

        public class ContentInitializer<T> : DropCreateDatabaseAlways<T>
            where T : TestContext
        {
            protected override void Seed(T context)
            {
                context.EntityASet.Add(new EntityA { ID = 1 });
                context.EntityASet.Add(new EntityA { ID = 2, DeleteTimestamp = DateTime.Now.AddMinutes(-1) });

                context.EntityBSet.Add(new EntityB { ID = 1 });
                context.EntityBSet.Add(new EntityB { ID = 2, DeleteTimestamp = DateTime.Now.AddMinutes(-1) });

                context.EntityCSet.Add(new EntityC { ID = 1 });
                context.EntityCSet.Add(new EntityC { ID = 2, DeleteTimestamp = DateTime.Now.AddMinutes(-1) });

                context.EntityDSet.Add(new EntityD { ID = 1 });
                context.EntityDSet.Add(new EntityD { ID = 2, DeleteTimestamp = DateTime.Now.AddMinutes(-1) });

                context.SaveChanges();
            }
        }

        #endregion
    }
}