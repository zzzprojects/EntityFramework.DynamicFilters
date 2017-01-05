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

        //  See https://github.com/jcachat/EntityFramework.DynamicFilters/issues/76
        [TestMethod]
        public void TPT_InstructorAsProperty()
        {
            using (var context = new TestContext())
            {
                var list = context.Students.Include(s => s.Instructor).ToList();
                Assert.IsTrue((list.Count == 2) && list.Any(a => (a.ID == 1) && (a.Instructor == null)) && list.Any(a => (a.ID == 3) && (a.Instructor != null)));
            }
        }

        [TestMethod]
        public void TPT_Residents()
        {
            using (var context = new TestContext())
            {
                var list = context.Residents.ToList();
                Assert.IsTrue((list.Count == 1) && list.All(a => (a.Id == 1)));
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

            public int? InstructorID { get; set; }
            public Instructor Instructor { get; set; }
        }

        public class BaseModel
        {
            public DateTime? Created { get; set; }

            [MaxLength(32)]
            public String CreatedBy { get; set; }

            public DateTime? Updated { get; set; }

            [MaxLength(32)]
            public String UpdatedBy { get; set; }

            [Index]
            public DateTime? Deleted { get; set; }
        }

        [Table("People2")]
        public class Person2 : BaseModel
        {
            public int Id { get; set; }

            [Required, Display(Name = "First Name"), MaxLength(32)]
            public string FirstName { get; set; }

            [Display(Name = "Middle Name"), MaxLength(64)]
            public string MiddleName { get; set; }

            [Required, Display(Name = "Last Name"), MaxLength(32)]
            public string LastName { get; set; }
        }

        [Table("Residents")]
        public class Resident : Person2
        {
            [Required, MaxLength(4)]
            public string Title { get; set; }

            [MaxLength(32), Display(Name = "Maiden Name")]
            public string MaidenName { get; set; }

            [MaxLength(32), Display(Name = "Famailiar Name")]
            public string FamiliarName { get; set; }

            [Required, MaxLength(1)]
            public string Gender { get; set; } // M or F

            [Display(Name = "Date of Birth"), DataType(DataType.Date)]
            public DateTime? DateOfBirth { get; set; }
        }

        #endregion

        #region TestContext

        public class TestContext : TestContextBase<TestContext>, ITestContext
        {
            public DbSet<Person> People { get; set; }
            public DbSet<Instructor> Instructors { get; set; }
            public DbSet<Student> Students { get; set; }

            public DbSet<Person2> People2 { get; set; }
            public DbSet<Resident> Residents { get; set; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Filter("IsDeleted", (IEntity e) => e.IsDeleted == false);

                modelBuilder.Filter("SoftDelete", (BaseModel x) => x.Deleted == null);
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

                Residents.Add(new Resident { Created=new DateTime(2015, 1, 1), CreatedBy="me", Id=1, FirstName="Fred", LastName="Flintstone", Title="Mr", FamiliarName="Fred", Gender="M" });
                Residents.Add(new Resident { Created=new DateTime(2015, 2, 1), CreatedBy="me", Deleted=new DateTime(2015, 2, 3), Id=2, FirstName="Barney", LastName="Rubble", Title="Mr", FamiliarName="Barney", Gender="M" });

                SaveChanges();
            }
        }

        #endregion
    }
}
