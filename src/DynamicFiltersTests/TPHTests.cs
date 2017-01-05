using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using EntityFramework.DynamicFilters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicFiltersTests
{
    //  Table-per-Type tests for issue #32.
    [TestClass]
    public class TPTHests
    {
        /// <summary>
        /// Tests a filter on a derived class involved in TPH - issue #93
        /// </summary>
        [TestMethod]
        public void TPH_IsDeleted_Students()
        {
            using (var context = new TestContext())
            {
                var list = context.Students.ToList();
                Assert.IsTrue((list.Count == 2) && list.All(a => (a.ID == 1) || (a.ID == 3)));
            }
        }

        #region Models

        public interface ISoftDelete
        {
            bool IsDeleted { get; set; }
        }

        public class Person : ISoftDelete
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }

            public bool IsDeleted { get; set; }
        }

        public class Instructor : Person
        {
            public DateTime HireDate { get; set; }
        }

        public class Student : Person
        {
            public DateTime EnrollmentDate { get; set; }

            public int? InstructorID { get; set; }
            public Instructor Instructor { get; set; }
        }

        #endregion

        #region TestContext

        public class TestContext : TestContextBase<TestContext>, ITestContext
        {
            public DbSet<Person> People { get; set; }
            public DbSet<Instructor> Instructors { get; set; }
            public DbSet<Student> Students { get; set; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                //  Filter defined only on the derived class to test issue #93.
                modelBuilder.Filter("IsDeleted", (Student e) => e.IsDeleted == false);
            }

            public override void Seed()
            {
                System.Diagnostics.Debug.Print("Seeding db");

                var instructor1 = Instructors.Add(new Instructor { ID = 10, IsDeleted = true, HireDate = new DateTime(2015, 2, 1) });
                var instructor2 = Instructors.Add(new Instructor { ID = 11, IsDeleted = false, HireDate = new DateTime(2015, 2, 2) });
                var instructor3 = Instructors.Add(new Instructor { ID = 12, IsDeleted = false, HireDate = new DateTime(2015, 2, 3) });

                Students.Add(new Student { ID = 1, IsDeleted = false, EnrollmentDate = new DateTime(2015, 1, 1), Instructor = instructor1 });
                Students.Add(new Student { ID = 2, IsDeleted = true, EnrollmentDate = new DateTime(2015, 1, 2), Instructor = instructor2 });
                Students.Add(new Student { ID = 3, IsDeleted = false, EnrollmentDate = new DateTime(2015, 1, 3), Instructor = instructor3 });

                SaveChanges();
            }
        }

        #endregion
    }
}
