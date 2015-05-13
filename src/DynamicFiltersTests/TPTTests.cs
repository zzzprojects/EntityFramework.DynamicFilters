using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityFramework.DynamicFilters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicFiltersTests
{
    //  Table-per-Type tests for issue #32.
    [TestClass]
    public class TPTTests
    {
        [TestMethod]
        public void TPT_IsDeleted_Students()
        {
            using (var context = new TestContext())
            {
                var list = context.Students.ToList();
                Assert.IsTrue((list.Count == 2) && list.All(a => (a.ID == 1) || (a.ID == 3)));
            }
        }

        [TestMethod]
        public void TPT_IsDeleted_Instructors()
        {
            using (var context = new TestContext())
            {
                var list = context.Instructors.ToList();
                Assert.IsTrue((list.Count == 2) && list.All(a => (a.ID == 11) || (a.ID == 12)));
            }
        }

        [TestMethod]
        public void TPT_IsDeleted_People()
        {
            using (var context = new TestContext())
            {
                var list = context.People.ToList();
                Assert.IsTrue((list.Count == 4) && list.All(a => (a.ID == 1) || (a.ID == 3) || (a.ID == 11) || (a.ID == 12)));
            }
        }

        #region Models

        public interface IEntity
        {
            bool IsDeleted { get; set; }
        }

        public class Person : IEntity
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }

            public bool IsDeleted { get; set; }
        }

        [Table("Instructors")]
        public class Instructor : Person
        {
            public DateTime HireDate { get; set; }
        }

        [Table("Students")]
        public class Student : Person
        {
            public DateTime EnrollmentDate { get; set; }
        }

        #endregion

        #region TestContext

        public class TestContext : DbContext
        {
            public DbSet<Person> People { get; set; }
            public DbSet<Instructor> Instructors { get; set; }
            public DbSet<Student> Students { get; set; }

            public TestContext()
                : base("TestContext")
            {
                Database.SetInitializer(new ContentInitializer<TestContext>());
                Database.Log = log => System.Diagnostics.Debug.WriteLine(log);
                Database.Initialize(false);

                this.EnableAllFilters();
            }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Filter("IsDeleted", (IEntity e) => e.IsDeleted, false);
            }
        }

        public class ContentInitializer<T> : DropCreateDatabaseAlways<T>
            where T : TestContext
        {
            protected override void Seed(T context)
            {
                System.Diagnostics.Debug.Print("Seeding db");

                context.Students.Add(new Student { ID = 1, IsDeleted = false, EnrollmentDate = new DateTime(2015, 1, 1) });
                context.Students.Add(new Student { ID = 2, IsDeleted = true, EnrollmentDate = new DateTime(2015, 1, 2) });
                context.Students.Add(new Student { ID = 3, IsDeleted = false, EnrollmentDate = new DateTime(2015, 1, 3) });

                context.Instructors.Add(new Instructor { ID = 10, IsDeleted = true, HireDate = new DateTime(2015, 2, 1) });
                context.Instructors.Add(new Instructor { ID = 11, IsDeleted = false, HireDate = new DateTime(2015, 2, 2) });
                context.Instructors.Add(new Instructor { ID = 12, IsDeleted = false, HireDate = new DateTime(2015, 2, 3) });

                context.SaveChanges();
            }
        }

        #endregion
    }
}
