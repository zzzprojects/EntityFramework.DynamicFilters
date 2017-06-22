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
#if USE_CSPACE

    [TestClass]
    public class NavigationPropertyTests
    {
        [TestMethod]
        public void NavigationProperty_FilterChildProperty()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityASet.ToList();
                Assert.IsTrue((list.Count == 2) && list.All(i => (i.ID == 1) || (i.ID == 2)));
            }
        }

        [TestMethod]
        public void NavigationProperty_FilterIncludedChildCollection()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityBSet.Include(b => b.Children).ToList();
                Assert.IsTrue((list.Count == 1) && (list.First().Children.Count == 2) && list.First().Children.All(i => (i.ID == 1) || (i.ID == 2)));
            }
        }

        [TestMethod]
        public void NavigationProperty_FilterIncludedChildProperty()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityCSet.Include(c => c.Child).ToList();

                if (context.OracleVersion()?.Major < 12)     //  If this is true, we are connected to Oracle 11 (or older)
                {
                    //  Oracle 11 does not support the DbExpression.Element() method that is needed
                    //  when applying filters to child properties.  To better support that, those filters are applied
                    //  using SSpace which then also results in them building an inner join against the child property.
                    //  That is technically not correct - the filter in this case is against the child property
                    //  so it should do an outer join and just leave the child set as null.
                    //  But allowing this test to pass for now because there is no other way to support the filter.
                    //  Oracle 12 DOES support it and works properly
                    Assert.IsTrue(list.Count == 2);
                }
                else
                    Assert.IsTrue(list.Count == 5);

                var itemsWithChild = list.Where(i => i.Child != null).ToList();
                Assert.IsTrue((itemsWithChild.Count == 2) && itemsWithChild.All(i => (i.ID == 1) || (i.ID == 2)));
            }
        }

        /// <summary>
        /// KNOWN_ISSUE!
        /// This test is the same as the EntityC test: NavigationProperty_FilterIncludedChildProperty.
        /// Except that the main model does not define a FK property for the child.
        /// See DynamicFilterQueryVisitor.Visit(DbPropertyExpression expression) for details.
        /// This test is expected to fail unless we can find a way to figure out the FKs so that we can
        /// create the join to the child table and include the filter conditions.
        /// ** This test will pass for Oracle & MySQL because of how filters on child properties
        /// are being applied in SSpace.  But that is not a good solution as it has other side effect
        /// (see NavigationProperty_FilterIncludedChildProperty test above).
        /// </summary>
        [TestMethod]
        public void KNOWN_ISSUE_NavigationProperty_NoFKsFilterIncludedChildProperty()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityDSet.Include(d => d.Child).ToList();

                var itemsWithChild = list.Where(i => i.Child != null).ToList();
                Assert.IsTrue((itemsWithChild.Count == 2) && itemsWithChild.All(i => (i.ID == 1) || (i.ID == 2)));
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
        {
            public int ChildID { get; set; }
            public EntityAChild Child { get; set; }
        }

        public class EntityAChild : EntityBase
        {
            public int Value { get; set; }      //  Naming this Value also tests that it can tell the difference between a nullable type ".Value" or a class property named ".Value"
        }

        public class EntityB : EntityBase
        {
            public int ChildID { get; set; }
            public ICollection<EntityBChild> Children { get; set; }
        }

        public class EntityBChild : EntityBase
        {
            public int ChildValue { get; set; }
        }

        public class EntityC : EntityBase
        {
            public int ChildID { get; set; }
            public EntityCChild Child { get; set; }
        }

        public class EntityCChild : EntityBase
        {
            public int ChildValue { get; set; }
        }

        public class EntityD : EntityBase
        {
            public EntityDChild Child { get; set; }
        }

        public class EntityDChild : EntityBase
        {
            public int ChildValue { get; set; }
        }

        #endregion

        #region TestContext

        public class TestContext : TestContextBase<TestContext>, ITestContext
        {
            public DbSet<EntityA> EntityASet { get; set; }
            public DbSet<EntityAChild> EntityAChildSet { get; set; }
            public DbSet<EntityB> EntityBSet { get; set; }
            public DbSet<EntityBChild> EntityBChildSet { get; set; }
            public DbSet<EntityC> EntityCSet { get; set; }
            public DbSet<EntityCChild> EntityCChildSet { get; set; }
            public DbSet<EntityD> EntityDSet { get; set; }
            public DbSet<EntityDChild> EntityDChildSet { get; set; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Filter("EntityAFilter", (EntityA a) => a.Child.Value <= 2);
                modelBuilder.Filter("EntityBFilter", (EntityBChild b) => b.ChildValue <= 2);
                modelBuilder.Filter("EntityCChildFilter", (EntityCChild c) => c.ChildValue <= 2);
                modelBuilder.Filter("EntityDChildFilter", (EntityDChild c) => c.ChildValue <= 2);
            }

            public override void Seed()
            {
                for (int i = 1; i <= 5; i++)
                {
                    EntityASet.Add(new EntityA { ID = i, Child = new EntityAChild { ID = i, Value = i } });
                    EntityCSet.Add(new EntityC { ID = i, Child = new EntityCChild { ID = i, ChildValue = i } });
                    EntityDSet.Add(new EntityD { ID = i, Child = new EntityDChild { ID = i, ChildValue = i } });
                }

                EntityBSet.Add(new EntityB
                {
                    ID = 1,
                    Children = new List<EntityBChild>()
                    {
                        new EntityBChild { ID = 1, ChildValue = 1 },
                        new EntityBChild { ID = 2, ChildValue = 2 },
                        new EntityBChild { ID = 3, ChildValue = 3 },
                        new EntityBChild { ID = 4, ChildValue = 4 },
                        new EntityBChild { ID = 5, ChildValue = 5 }
                    }
                });

                SaveChanges();
            }
        }

        #endregion
    }
#endif
}