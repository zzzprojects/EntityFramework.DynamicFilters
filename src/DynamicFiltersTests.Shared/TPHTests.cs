using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using EntityFramework.DynamicFilters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicFiltersTests
{
    //  Table-per-Type tests for issue #32.
    [TestClass]
    public class TPHTests
    {
        /// <summary>
        ///     Tests a filter on a derived class involved in TPH - issue #93
        /// </summary>
        [TestMethod]
        public void TPH_IsDeleted_Students()
        {
            using (var context = new TestContext())
            {
                var list = context.Students.ToList();
                Assert.IsTrue(list.Count == 2 && list.All(a => a.ID == 1 || a.ID == 3));
            }
        }

        /// <summary>
        ///     Address the issue where an access to a deeply nested class in a inheritance hierarchy throw.
        ///     <see cref="http://github.com/jcachat/EntityFramework.DynamicFilters/issues/106" />
        /// </summary>
        [TestMethod]
        public void TPH_Projection_Works_For_Deep_Inheritance_Level_With_Include_Call()
        {
            using (var context = new TestContext())
            {
                var list = context.Set<Level5B>().Include(o => o.Level5A).ToList();
                Assert.IsTrue(list.Count == 2);
            }
        }

        #region TestContext

        public class TestContext : TestContextBase<TestContext>, ITestContext
        {
            public DbSet<Person> People { get; set; }
            public DbSet<Instructor> Instructors { get; set; }
            public DbSet<Student> Students { get; set; }
            public DbSet<Level3> Level3 { get; set; }

            public override void Seed()
            {
                Debug.Print("Seeding db");

                var instructor1 = Instructors.Add(
                    new Instructor {ID = 10, IsDeleted = true, HireDate = new DateTime(2015, 2, 1)});
                var instructor2 = Instructors.Add(
                    new Instructor {ID = 11, IsDeleted = false, HireDate = new DateTime(2015, 2, 2)});
                var instructor3 = Instructors.Add(
                    new Instructor {ID = 12, IsDeleted = false, HireDate = new DateTime(2015, 2, 3)});

                Students.Add(new Student
                {
                    ID = 1,
                    IsDeleted = false,
                    EnrollmentDate = new DateTime(2015, 1, 1),
                    Instructor = instructor1
                });
                Students.Add(new Student
                {
                    ID = 2,
                    IsDeleted = true,
                    EnrollmentDate = new DateTime(2015, 1, 2),
                    Instructor = instructor2
                });
                Students.Add(new Student
                {
                    ID = 3,
                    IsDeleted = false,
                    EnrollmentDate = new DateTime(2015, 1, 3),
                    Instructor = instructor3
                });


                Level3.Add(new Level5B {Id = 11, IsDeleted = false});
                Level3.Add(new Level5B {Id = 12, IsDeleted = true});
                Level3.Add(new Level5B {Id = 13, IsDeleted = false});

                SaveChanges();
            }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                //  Filter defined only on the derived class to test issue #93.
                modelBuilder.Filter("IsDeleted", (Student e) => e.IsDeleted == false);
                modelBuilder.Filter("IsDeleted", (Level0 e) => e.IsDeleted == false);
            }
        }

        #endregion

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

        public abstract class Level0
        {
            public bool IsDeleted { get; set; }

            public int Id { get; set; }
        }

        public class Level1 : Level0
        {
        }

        public abstract class Level2 : Level1
        {
        }

        public abstract class Level3 : Level2
        {
        }

        public abstract class Level4 : Level3
        {
        }

        public class Level5A : Level4
        {
        }

        public class Level5B : Level4
        {
            public int? Level5AId { get; set; }
            public virtual Level5A Level5A { get; set; }
        }

        #endregion
    }
}