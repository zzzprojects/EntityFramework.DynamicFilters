using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using EntityFramework.DynamicFilters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicFiltersTests
{
    [TestClass]
    public class OptionsTests
    {
        //  TODO: Additional tests
        //  2) Recursion test with a non-navigation (not child collection) property

        [TestMethod]
        public void Options_FilterNotAppliedToChildEntities()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityASet.Include(a => a.Children).ToList();

                Assert.IsTrue((list.Count == 1) && (list.Single().Children.Count == 2));
            }
        }

        [TestMethod]
        public void Options_FilterNotAppliedRecursively1()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityBSet.Include(x => x.Children.Select(y => y.Children)).ToList();

                Assert.IsTrue(list.Count == 2);

                var b = list.First(x => x.ID == 1);
                Assert.IsTrue(b.Children.All(x => x.ID == 1) && b.Children.Single().Children.Count == 2);

                b = list.First(x => x.ID == 2);
                Assert.IsTrue(b.Children.All(x => x.ID == 3) && b.Children.Single().Children.Count == 2);
            }
        }

        [TestMethod]
        public void Options_FilterNotAppliedRecursively2()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityCSet.Include(x => x.Children.Select(y => y.Children)).ToList();

                Assert.IsTrue(list.All(x => (x.ID == 1)) && (list.Single().Children.Count == 2));

                Assert.IsTrue(list.Single().Children.First(x => x.ID == 1).Children.Count == 2);
                Assert.IsTrue(list.Single().Children.First(x => x.ID == 2).Children.Count == 2);
            }
        }

        [TestMethod]
        public void Options_FilterNotAppliedRecursively3()
        {
            //  This entity has 2 sets of children that both implement the ISoftDelete interface
            //  as do their child.  Tests to make sure that BOTH of the properties inside EntityD
            //  are filtered and that neither of their child collections are filtered.
            using (var context = new TestContext())
            {
                var list = context.EntityDSet
                    .Include(x => x.Children1.Select(y => y.Children))
                    .Include(x => x.Children2.Select(y => y.Children))
                    .ToList();

                var d = list.Single();

                Assert.IsTrue(d.Children1.All(x => x.ID == 1));
                Assert.IsTrue(d.Children1.Single().Children.Count == 2);
                Assert.IsTrue(d.Children2.All(x => x.ID == 1));
                Assert.IsTrue(d.Children2.Single().Children.Count == 2);
            }
        }

        [TestMethod]
        public void Options_FilterNotAppliedRecursively4()
        {
            using (var context = new TestContext())
            {
                var list = context.EntityESet.Include(x => x.Child).ToList();

                Assert.IsTrue(list.All(e => ((e.ID == 1) || (e.ID == 2)) && (e.Child != null) && (e.Child.ID == e.ID)));
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

        public interface ISoftDeleteA
        {
            bool IsDeleted { get; set; }
        }

        public class EntityA : EntityBase, ISoftDeleteA
        {
            public ICollection<EntityAChild> Children { get; set; }

            public bool IsDeleted { get; set; }
        }

        public class EntityAChild : EntityBase, ISoftDeleteA
        {
            public int ParentID { get; set; }
            public EntityA Parent { get; set; }

            public bool IsDeleted { get; set; }
        }

        public interface ISoftDeleteB
        {
            bool IsDeleted { get; set; }
        }

        public class EntityB : EntityBase
        {
            public ICollection<EntityBChild1> Children { get; set; }
        }

        public class EntityBChild1 : EntityBase, ISoftDeleteB
        {
            public int ParentID { get; set; }
            public EntityB Parent { get; set; }

            public bool IsDeleted { get; set; }

            public ICollection<EntityBChild2> Children { get; set; }
        }

        public class EntityBChild2 : EntityBase, ISoftDeleteB
        {
            public int ParentID { get; set; }
            public EntityBChild1 Parent { get; set; }

            public bool IsDeleted { get; set; }
        }

        public interface ISoftDeleteC
        {
            bool IsDeleted { get; set; }
        }

        public class EntityC : EntityBase, ISoftDeleteC
        {
            public ICollection<EntityCChild1> Children { get; set; }

            public bool IsDeleted { get; set; }
        }

        public class EntityCChild1 : EntityBase
        {
            public int ParentID { get; set; }
            public EntityC Parent { get; set; }

            public ICollection<EntityCChild2> Children { get; set; }
        }

        public class EntityCChild2 : EntityBase, ISoftDeleteC
        {
            public int ParentID { get; set; }
            public EntityCChild1 Parent { get; set; }

            public bool IsDeleted { get; set; }
        }

        public interface ISoftDeleteD
        {
            bool IsDeleted { get; set; }
        }

        public class EntityD : EntityBase
        {
            public ICollection<EntityDChild1> Children1 { get; set; }
            public ICollection<EntityDChild2> Children2 { get; set; }
        }

        public class EntityDChild1 : EntityBase, ISoftDeleteD
        {
            public int ParentID { get; set; }
            public EntityD Parent { get; set; }

            public bool IsDeleted { get; set; }

            public ICollection<EntityDChild1Child> Children { get; set; }
        }

        public class EntityDChild1Child : EntityBase, ISoftDeleteD
        {
            public int ParentID { get; set; }
            public EntityDChild1 Parent { get; set; }

            public bool IsDeleted { get; set; }
        }

        public class EntityDChild2 : EntityBase, ISoftDeleteD
        {
            public int ParentID { get; set; }
            public EntityD Parent { get; set; }

            public bool IsDeleted { get; set; }

            public ICollection<EntityDChild2Child> Children { get; set; }
        }

        public class EntityDChild2Child : EntityBase, ISoftDeleteD
        {
            public int ParentID { get; set; }
            public EntityDChild2 Parent { get; set; }

            public bool IsDeleted { get; set; }
        }

        public interface ISoftDeleteE
        {
            bool IsDeleted { get; set; }
        }

        public class EntityE: EntityBase, ISoftDeleteE
        {
            public bool IsDeleted { get; set; }

            public int ChildID { get; set; }
            public EntityEChild Child { get; set; }
        }

        public class EntityEChild : EntityBase, ISoftDeleteE
        {
            public bool IsDeleted { get; set; }

            //public int ParentID { get; set; }
            //public EntityE Parent { get; set; }
        }

        #endregion

        #region TestContext

        public class TestContext : TestContextBase<TestContext>, ITestContext
        {
            public DbSet<EntityA> EntityASet { get; set; }
            public DbSet<EntityAChild> EntityAChildSet { get; set; }

            public DbSet<EntityB> EntityBSet { get; set; }
            public DbSet<EntityBChild1> EntityBChild1Set { get; set; }
            public DbSet<EntityBChild2> EntityBChild2Set { get; set; }

            public DbSet<EntityC> EntityCSet { get; set; }
            public DbSet<EntityCChild1> EntityCChild1Set { get; set; }
            public DbSet<EntityCChild2> EntityCChild2Set { get; set; }

            public DbSet<EntityD> EntityDSet { get; set; }
            public DbSet<EntityDChild1> EntityDChild1Set { get; set; }
            public DbSet<EntityDChild1Child> EntityDChild1ChildSet { get; set; }
            public DbSet<EntityDChild2> EntityDChild2Set { get; set; }
            public DbSet<EntityDChild2Child> EntityDChild2ChildSet { get; set; }

            public DbSet<EntityE> EntityESet { get; set; }
            public DbSet<EntityEChild> EntityEChildSet { get; set; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Filter("ISoftDeleteAFilter", (ISoftDeleteA a, bool isDeleted) => a.IsDeleted == isDeleted, () => false, opt => opt.ApplyToChildProperties(false));
                modelBuilder.Filter("ISoftDeleteBFilter", (ISoftDeleteB b, bool isDeleted) => b.IsDeleted == isDeleted, () => false, opt => opt.ApplyRecursively(false));
                modelBuilder.Filter("ISoftDeleteCFilter", (ISoftDeleteC c, bool isDeleted) => c.IsDeleted == isDeleted, () => false, opt => opt.ApplyRecursively(false));
                modelBuilder.Filter("ISoftDeleteDFilter", (ISoftDeleteD d, bool isDeleted) => d.IsDeleted == isDeleted, () => false, opt => opt.ApplyRecursively(false));
                modelBuilder.Filter("ISoftDeleteEFilter", (ISoftDeleteE e, bool isDeleted) => e.IsDeleted == isDeleted, () => false, opt => opt.ApplyRecursively(false));
            }

            public override void Seed()
            {
                EntityASet.Add(new EntityA
                {
                    ID = 1,
                    IsDeleted = false,
                    Children = new List<EntityAChild>
                    {
                        new EntityAChild { ID = 1, IsDeleted = false },
                        new EntityAChild { ID = 2, IsDeleted = true },
                    }
                });
                EntityASet.Add(new EntityA
                {
                    ID = 2,
                    IsDeleted = true,
                    Children = new List<EntityAChild>
                    {
                        new EntityAChild { ID = 3, IsDeleted = false },
                        new EntityAChild { ID = 4, IsDeleted = true },
                    }
                });

                EntityBSet.Add(new EntityB
                {
                    ID = 1,
                    Children = new List<EntityBChild1>
                    {
                        new EntityBChild1
                        {
                            ID = 1,
                            IsDeleted = false,
                            Children = new List<EntityBChild2>
                            {
                                new EntityBChild2 { ID = 1, IsDeleted = false },
                                new EntityBChild2 { ID = 2, IsDeleted = true },
                            }
                        },
                        new EntityBChild1
                        {
                            ID = 2,
                            IsDeleted = true,
                            Children = new List<EntityBChild2>
                            {
                                new EntityBChild2 { ID = 3, IsDeleted = false },
                                new EntityBChild2 { ID = 4, IsDeleted = true },
                            }
                        },
                    }
                });
                EntityBSet.Add(new EntityB
                {
                    ID = 2,
                    Children = new List<EntityBChild1>
                    {
                        new EntityBChild1
                        {
                            ID = 3,
                            IsDeleted = false,
                            Children = new List<EntityBChild2>
                            {
                                new EntityBChild2 { ID = 5, IsDeleted = false },
                                new EntityBChild2 { ID = 6, IsDeleted = true },
                            }
                        },
                        new EntityBChild1
                        {
                            ID = 4,
                            IsDeleted = true,
                            Children = new List<EntityBChild2>
                            {
                                new EntityBChild2 { ID = 7, IsDeleted = false },
                                new EntityBChild2 { ID = 8, IsDeleted = true },
                            }
                        },
                    }
                });

                EntityCSet.Add(new EntityC
                {
                    ID = 1,
                    IsDeleted = false,
                    Children = new List<EntityCChild1>
                    {
                        new EntityCChild1
                        {
                            ID = 1,
                            Children = new List<EntityCChild2>
                            {
                                new EntityCChild2 { ID = 1, IsDeleted = false },
                                new EntityCChild2 { ID = 2, IsDeleted = true },
                            }
                        },
                        new EntityCChild1
                        {
                            ID = 2,
                            Children = new List<EntityCChild2>
                            {
                                new EntityCChild2 { ID = 3, IsDeleted = false },
                                new EntityCChild2 { ID = 4, IsDeleted = true },
                            }
                        },
                    }
                });
                EntityCSet.Add(new EntityC
                {
                    ID = 2,
                    IsDeleted = true,
                    Children = new List<EntityCChild1>
                    {
                        new EntityCChild1
                        {
                            ID = 3,
                            Children = new List<EntityCChild2>
                            {
                                new EntityCChild2 { ID = 5, IsDeleted = false },
                                new EntityCChild2 { ID = 6, IsDeleted = true },
                            }
                        },
                        new EntityCChild1
                        {
                            ID = 4,
                            Children = new List<EntityCChild2>
                            {
                                new EntityCChild2 { ID = 7, IsDeleted = false },
                                new EntityCChild2 { ID = 8, IsDeleted = true },
                            }
                        },
                    }
                });

                EntityDSet.Add(new EntityD
                {
                    ID = 1,
                    Children1 = new List<EntityDChild1>
                    {
                        new EntityDChild1
                        {
                            ID = 1,
                            IsDeleted = false,
                            Children = new List<EntityDChild1Child>
                            {
                                new EntityDChild1Child { ID = 1, IsDeleted = false },
                                new EntityDChild1Child { ID = 2, IsDeleted = true },
                            }
                        },
                        new EntityDChild1
                        {
                            ID = 2,
                            IsDeleted = true,
                            Children = new List<EntityDChild1Child>
                            {
                                new EntityDChild1Child { ID = 3, IsDeleted = false },
                                new EntityDChild1Child { ID = 4, IsDeleted = true },
                            }
                        }
                    },
                    Children2 = new List<EntityDChild2>
                    {
                        new EntityDChild2
                        {
                            ID = 1,
                            IsDeleted = false,
                            Children = new List<EntityDChild2Child>
                            {
                                new EntityDChild2Child { ID = 1, IsDeleted = false },
                                new EntityDChild2Child { ID = 2, IsDeleted = true },
                            }
                        },
                        new EntityDChild2
                        {
                            ID = 2,
                            IsDeleted = true,
                            Children = new List<EntityDChild2Child>
                            {
                                new EntityDChild2Child { ID = 3, IsDeleted = false },
                                new EntityDChild2Child { ID = 4, IsDeleted = true },
                            }
                        }
                    },
                });

                EntityESet.Add(new EntityE
                {
                    ID = 1,
                    IsDeleted = false,
                    Child = new EntityEChild { ID = 1, IsDeleted = false }
                });
                EntityESet.Add(new EntityE
                {
                    ID = 2,
                    IsDeleted = false,
                    Child = new EntityEChild { ID = 2, IsDeleted = true }
                });
                EntityESet.Add(new EntityE
                {
                    ID = 3,
                    IsDeleted = true,
                    Child = new EntityEChild { ID = 3, IsDeleted = false }
                });
                EntityESet.Add(new EntityE
                {
                    ID = 4,
                    IsDeleted = true,
                    Child = new EntityEChild { ID = 4, IsDeleted = true }
                });

                SaveChanges();
            }
        }

        #endregion
    }
}
