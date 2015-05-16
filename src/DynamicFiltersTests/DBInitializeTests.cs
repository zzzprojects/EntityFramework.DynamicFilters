using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EntityFramework.DynamicFilters;
using System;
using System.Data.Entity.Infrastructure;

namespace DynamicFiltersTests
{
    //  Tests for handling Database.Initialize not being called prior to enable/disbale/set - issue #34, 36, 24
    //  Each of these tests needs to be done in it's own class with it's own DbContext so that we ensure that
    //  when each test starts, the DbContext has not been initialized yet.  If they used the same DbContext class,
    //  once one tests triggered the initialize, all tests would then pass.

    [TestClass]
    public class DBInitializeTests_DisableFilter
    {
        [TestMethod]
        public void DBInitialize_DisableFilter()
        {
            using (var context = new TestContext())
            {
                context.DisableFilter("EntityAFilter" + context.FilterSuffix);
                var list = context.EntityASet.ToList();
                Assert.IsTrue(list.Count == 10);
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "DisableFilter"; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    [TestClass]
    public class DBInitializeTests_EnableFilter
    {
        [TestMethod]
        public void DBInitialize_EnableFilter()
        {
            using (var context = new TestContext())
            {
                context.EnableFilter("EntityBFilter" + context.FilterSuffix);
                var list = context.EntityBSet.ToList();
                Assert.IsTrue(list.Count == 4);
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "EnableFilter"; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    [TestClass]
    public class DBInitializeTests_DisableAllFilters
    {
        [TestMethod]
        public void DBInitialize_DisableAllFilters()
        {
            using (var context = new TestContext())
            {
                context.DisableAllFilters();
                var list = context.EntityASet.ToList();
                Assert.IsTrue(list.Count == 10);
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "DisableAllFilters"; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    [TestClass]
    public class DBInitializeTests_EnableAllFilters
    {
        [TestMethod]
        public void DBInitialize_EnableAllFilters()
        {
            using (var context = new TestContext())
            {
                context.EnableAllFilters();
                var list = context.EntityBSet.ToList();
                Assert.IsTrue(list.Count == 4);
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "EnableAllFilters" ; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    [TestClass]
    public class DBInitializeTests_IsFilterEnabled
    {
        [TestMethod]
        public void DBInitialize_IsFilterEnabled()
        {
            using (var context = new TestContext())
            {
                var filterAEnabled = context.IsFilterEnabled("EntityAFilter" + context.FilterSuffix);
                var filterBEnabled = context.IsFilterEnabled("EntityBFilter" + context.FilterSuffix);
                Assert.IsTrue(filterAEnabled && !filterBEnabled);
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "IsFilterEnabled"; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    [TestClass]
    public class DBInitializeTests_SetScopedParameterValueConstant
    {
        [TestMethod]
        public void DBInitialize_SetScopedParameterValueConstant()
        {
            using (var context = new TestContext())
            {
                context.SetFilterScopedParameterValue("EntityAFilter" + context.FilterSuffix, 2);
                var list = context.EntityASet.ToList();
                Assert.IsTrue(list.Count == 1);
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "SetScopedParameterValueConstant"; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    [TestClass]
    public class DBInitializeTests_SetScopedParameterValueFunc
    {
        [TestMethod]
        public void DBInitialize_SetScopedParameterValueFunc()
        {
            using (var context = new TestContext())
            {
                context.SetFilterScopedParameterValue("EntityAFilter" + context.FilterSuffix, () => 3);
                var list = context.EntityASet.ToList();
                Assert.IsTrue(list.Count == 2);
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "SetScopedParameterValueFunc"; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    [TestClass]
    public class DBInitializeTests_SetScopedParameterValueNamed
    {
        [TestMethod]
        public void DBInitialize_SetScopedParameterValueNamed()
        {
            using (var context = new TestContext())
            {
                context.SetFilterScopedParameterValue("EntityCFilter" + context.FilterSuffix, "value", () => "B");
                var list = context.EntityCSet.ToList();
                Assert.IsTrue(list.All(c => c.ID == 2));
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "SetScopedParameterValueNamed"; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    [TestClass]
    public class DBInitializeTests_SetGlobalParameterValueConstant
    {
        [TestMethod]
        public void DBInitialize_SetGlobalParameterValueConstant()
        {
            using (var context = new TestContext())
            {
                context.SetFilterGlobalParameterValue("EntityAFilter" + context.FilterSuffix, 2);
                var list = context.EntityASet.ToList();
                Assert.IsTrue(list.Count == 1);

                context.ResetGlobalFilterParameterValues();     //  So changes don't interfere with other tests
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "SetGlobalParameterValueConstant"; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    [TestClass]
    public class DBInitializeTests_SetGlobalParameterValueFunc
    {
        [TestMethod]
        public void DBInitialize_SetGlobalParameterValueFunc()
        {
            using (var context = new TestContext())
            {
                context.SetFilterGlobalParameterValue("EntityAFilter" + context.FilterSuffix, () => 3);
                var list = context.EntityASet.ToList();
                Assert.IsTrue(list.Count == 2);

                context.ResetGlobalFilterParameterValues();     //  So changes don't interfere with other tests
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "SetGlobalParameterValueFunc"; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    [TestClass]
    public class DBInitializeTests_SetGlobalParameterValueNamed
    {
        [TestMethod]
        public void DBInitialize_SetGlobalParameterValueNamed()
        {
            using (var context = new TestContext())
            {
                context.SetFilterGlobalParameterValue("EntityCFilter" + context.FilterSuffix, "value", () => "B");
                var list = context.EntityCSet.ToList();
                Assert.IsTrue(list.All(c => c.ID == 2));

                context.ResetGlobalFilterParameterValues();     //  So changes don't interfere with other tests
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "SetGlobalParameterValueNamed"; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    [TestClass]
    public class DBInitializeTests_ClearScopedParam
    {
        [TestMethod]
        public void DBInitialize_ClearScopedParam()
        {
            using (var context = new TestContext())
            {
                context.ClearScopedParameters();
                var list = context.EntityASet.ToList();
                Assert.IsTrue(list.Count == 4);
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "ClearScopedParam"; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    [TestClass]
    public class DBInitializeTests_GetParameterValue
    {
        [TestMethod]
        public void DBInitialize_GetParameterValue()
        {
            using (var context = new TestContext())
            {
                var paramValue = context.GetFilterParameterValue("EntityAFilter" + context.FilterSuffix, "id");
                Assert.IsTrue((paramValue != null) && (paramValue.GetType() == typeof(int)) && ((int)paramValue == 5));
            }
        }

        public class TestContext : DBInitializeTestContextBase
        {
            public override string FilterSuffix { get { return "GetParameterValue"; } }

            public TestContext()
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
            }
        }
    }

    #region DBInitializeTestContextBase

    public abstract class DBInitializeTestContextBase : DbContext
    {
        #region Models

        public abstract class EntityBase
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }
        }

        public class EntityA : EntityBase
        { }

        public class EntityB : EntityBase
        { }

        public class EntityC : EntityBase
        {
            public string Value { get; set; }
        }

        #endregion

        public DbSet<EntityA> EntityASet { get; set; }
        public DbSet<EntityB> EntityBSet { get; set; }
        public DbSet<EntityC> EntityCSet { get; set; }

        public DBInitializeTestContextBase()
            : base("TestContext")
        {
            //  SetInitializer must be done in derived class so that it's registered with the correct concrete type.
            //  If we do it here, it will not Initialize & Seed for each derived class.
            //Database.SetInitializer(new ContentInitializer<T>());

            Database.Log = log => System.Diagnostics.Debug.WriteLine(log);
            
            //  Initialize not done here in these tests - relying on DynamicFilters to do this for us
            //Database.Initialize(false);
        }

        /// <summary>
        /// Suffix to apply to all filters created in this context - to keep them all unique between tests
        /// so that the static caches inside DynamicFilters will not cause false results.
        /// </summary>
        public abstract string FilterSuffix { get; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //  Reset DynamicFilters to an initial state - this discards anything statically cached from other tests
            modelBuilder.ResetDynamicFilters(); //  *** Do not do this in normal production code! ***

            modelBuilder.Filter("EntityAFilter" + FilterSuffix, (EntityA a, int id) => a.ID < id, () => 5);
            modelBuilder.Filter("EntityBFilter" + FilterSuffix, (EntityB b, int id) => b.ID < id, () => 5);
            modelBuilder.DisableFilterGlobally("EntityBFilter" + FilterSuffix);

            modelBuilder.Filter("EntityCFilter" + FilterSuffix, (EntityC c, int id, string value) => (c.ID < id) && (c.Value == value), () => 5, () => "A");
        }

        public void ResetGlobalFilterParameterValues()
        {
            this.SetFilterGlobalParameterValue("EntityAFilter" + FilterSuffix, () => 5);
            this.SetFilterGlobalParameterValue("EntityBFilter" + FilterSuffix, () => 5);
            this.SetFilterGlobalParameterValue("EntityCFilter" + FilterSuffix, "id", () => 5);
            this.SetFilterGlobalParameterValue("EntityCFilter" + FilterSuffix, "value", () => "A");
        }

        public class ContentInitializer<T> : DropCreateDatabaseAlways<T>
            where T : DBInitializeTestContextBase
        {
            protected override void Seed(T context)
            {
                System.Diagnostics.Debug.Print("Seeding db");

                for (int i = 1; i <= 10; i++)
                {
                    context.EntityASet.Add(new EntityA { ID = i });
                    context.EntityBSet.Add(new EntityB { ID = i });
                    context.EntityCSet.Add(new EntityC { ID = i, Value = System.Convert.ToChar(64 + i).ToString() });
                }

                context.SaveChanges();
            }
        }
    }

    #endregion

}
