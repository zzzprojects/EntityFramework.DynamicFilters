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

    //  Tests related to null comparisons and Nullable<T>.HasValue handling in lambda filters (issue #14)
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
        public void KNOWN_ISSUE_NavigationProperty_FilterIncludedChildProperty()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityCSet.Include(c => c.Child).ToList();

                throw new NotImplementedException();        //  TODO: Figure out correct return conditions once this case actually works
                //Assert.IsTrue((list.Count == 2) && list.All(i => (i.ID == 1) || (i.ID == 2)));
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
            public int? ChildID { get; set; }
            public EntityCChild Child { get; set; }
        }

        public class EntityCChild : EntityBase
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

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Filter("EntityAFilter", (EntityA a) => a.Child.Value <= 2);
                modelBuilder.Filter("EntityBFilter", (EntityBChild b) => b.ChildValue <= 2);
                modelBuilder.Filter("EntityCChildFilter", (EntityCChild c) => c.ChildValue <= 2);
            }

            public override void Seed()
            {
                for (int i = 1; i <= 5; i++)
                {
                    EntityASet.Add(new EntityA { ID = i, Child = new EntityAChild { ID = i, Value = i } });
                    EntityCSet.Add(new EntityC { ID = i, Child = new EntityCChild { ID = i, ChildValue = i } });
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