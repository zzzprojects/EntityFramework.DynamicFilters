using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EntityFramework.DynamicFilters;
using System.Collections.Generic;

namespace DynamicFiltersTests
{
    [TestClass]
    public class AccountTest
    {

        //  Tests the following issues: #1, #2, #3, #6, #7, #9, #14
        [TestMethod]
        public void AccountAndBlogEntries()
        {
            using (var context1 = new TestContext())
            {
                Console.WriteLine("");
                Console.WriteLine("Querying with IsDeleted filter enabled");
                Query(context1, "homer", 2, true);
                Query(context1, "bart", 3, true);

                //  Query each account with the IsDeleted filter disabled.  The filter is disabled
                //  by setting the value to null using SetFilterScopedParameterValue() so that it applies
                //  ONLY to this context and will not affect any other active or future contexts.
                using (var context2 = new TestContext())
                {
                    context2.DisableFilter("BlogEntryFilter");

                    Console.WriteLine("");
                    Console.WriteLine("Querying with BlogEntryFilter filter disabled");
                    Query(context2, "homer", 9, false);
                    Query(context2, "bart", 9, false);

                    //  Re-enable the filter and query for deleted records
                    context2.EnableFilter("BlogEntryFilter");
                    context2.SetFilterScopedParameterValue("BlogEntryFilter", "isDeleted", true);
                    Console.WriteLine("");
                    Console.WriteLine("Querying for deleted records only");
                    Query(context2, "homer", 2, true, true);
                    Query(context2, "bart", 2, true, true);
                }

                //  Re-query using the original context1 object to demonstrate that the changes
                //  made to context2 have no effect on it and were properly scoped.
                Console.WriteLine("");
                Console.WriteLine("Re-Querying with original context");
                Query(context1, "homer", 2, true);
                Query(context1, "bart", 3, true);
            }
        }

        private static void Query(TestContext context, string userName, int expected, bool blogFilterIsEnabled, bool reusedContext = false)
        {
            //  Tests a .Where() clause in addition to filter.  These conditions should be included in same query.  Issue #2 & #6
            var account = context.Accounts
                .Include(a => a.BlogEntries)
                .Where(a => a.UserName == userName).FirstOrDefault();

            //  Set the CurrentAccountID in the static/global property used in the filter definition (see ExampleContext.OnModelCreating)
            //  For creating a filter on the current user, this could come from the Thread.CurrentPrincipal...
            TestContext.CurrentAccountID = account.ID;

            if (blogFilterIsEnabled)
            {
                //  Don't do this check if the blog filter is disabled.  Otherwise, the blog records in account.BlogEntries
                //  will be filtered by the Account.ID while allBlogEntries will not (since the AccountID filter is now
                //  part of the "BlogEntryFilter" filter).

                //  Test to make sure that Include()'d collections are also filtered
                var allBlogEntries = context.BlogEntries.ToList();
                if (account.BlogEntries.Count != allBlogEntries.Count)
                {
                    //  This will happen if the context is being reused and EF already has other blog entries cached.  Those
                    //  cached objects will be automatically attached to the account object even if they do not match the
                    //  current sql query filter.  There is nothing that can be done about this.  Care should be taken to
                    //  not re-use the same DbContext when filter values are changed.
                    //  See https://github.com/jcachat/EntityFramework.DynamicFilters/issues/5
                    System.Diagnostics.Debug.Print("account.BlogEntries={0}, allBlogEntries={1}, reusedContext={2}", account.BlogEntries.Count, allBlogEntries.Count, reusedContext);
                    Assert.IsTrue(reusedContext, string.Format("BlogEntry counts do not match: account.BlogEntries={0}, allBlogEntries={1}, reusedContext={2}", account.BlogEntries.Count, allBlogEntries.Count, reusedContext));
                }
            }

            //  Query blog entries.  This will use the global filter created in ExampleContext.OnModelCreating.
            //  Filter on IsActive is to reproduce a situation seen with MySQL that was adding those filters and not enclosing them in ()'s so the
            //  'or' condition here was not properly enclosed - caused our dynamic filters to not be used correctly.
            bool? active = true;
            var blogEntries = context.BlogEntries.Where(b => (!active.HasValue || (b.IsActive == active.Value))).ToList();

            Console.WriteLine(string.Format("Current User = {0}: Selected {1} blog entries", userName, blogEntries.Count));
            Assert.IsTrue(blogEntries.Count == expected, string.Format("Incorrect number of blog entries: Expected {0}, got {1}", expected, blogEntries.Count));
        }

        #region Models

        public interface ISoftDelete
        {
            bool IsDeleted { get; set; }
        }

        public class Account
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]   //  Oracle does not support generating Guid IDs
            public Guid ID { get; set; }

            [MaxLength(100)] //  Must set MaxLength or Oracle will set column datatype to NCLOB which will then fail comparisons against a string/nvarchar!
            public string UserName { get; set; }

            public ICollection<BlogEntry> BlogEntries { get; set; }

            /// <summary>
            /// Column used to verify handling of Entity properties mapped to different conceptual property names.
            /// </summary>
            [Column("RemappedDBProp")]
            public bool RemappedEntityProp { get; set; }
        }

        //  Tests issue with TPH inheritance causing duplicate annotation names being added to the model conventions (issue #3)
        public class DerivedAccount : Account
        {
        }

        public class BlogEntry : ISoftDelete
        {
            [Key]
            [Required]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]   //  Oracle does not support generating Guid IDs
            public Guid ID { get; set; }

            public Account Account { get; set; }
            public Guid AccountID { get; set; }

            [MaxLength(100)] //  Must set MaxLength or Oracle will set column datatype to NCLOB which will then fail comparisons against a string/nvarchar!
            public string Body { get; set; }

            public bool IsDeleted { get; set; }

            public int? IntValue { get; set; }

            [MaxLength(100)] //  Must set MaxLength or Oracle will set column datatype to NCLOB which will then fail comparisons against a string/nvarchar!
            public string StringValue { get; set; }

            public DateTime? DateValue { get; set; }

            public bool IsActive { get; set; }
        }

        #endregion

        #region TestContext

        public class TestContext : TestContextBase<TestContext>, ITestContext
        {
            //  A static/globally scoped value that will be used to restrict queries against the BlogEntries table.
            public static Guid CurrentAccountID { get; set; }

            public DbSet<Account> Accounts { get; set; }
            public DbSet<BlogEntry> BlogEntries { get; set; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                //  Lambda filter (issues #7 & #9)
                modelBuilder.Filter("BlogEntryFilter", (BlogEntry b, Guid accountID, bool isDeleted) => (b.AccountID == accountID) && (b.IsDeleted == isDeleted),
                                                    () => CurrentAccountID, () => false);

                //  Filter to test CSpace mapping (issue #1)
                modelBuilder.Filter("ConceptualNameTest", (Account a, bool remappedEntityProp) => a.RemappedEntityProp == remappedEntityProp, false);
            }

            public override void Seed()
            {
                System.Diagnostics.Debug.Print("Seeding db");

                //  Seeds 2 accounts with 9 blog entries, 4 of which are deleted

                var homer = new Account
                {
                    ID = Guid.NewGuid(),
                    UserName = "homer",
                    BlogEntries = new List<BlogEntry>
                    {
                        new BlogEntry { ID=Guid.NewGuid(), Body="Homer's first blog entry", IsDeleted=false, IsActive=true, StringValue="1"},
                        new BlogEntry { ID=Guid.NewGuid(), Body="Homer's second blog entry", IsDeleted=false, IsActive=true, StringValue="2"},
                        new BlogEntry { ID=Guid.NewGuid(), Body="Homer's third blog entry (deleted)", IsDeleted=true, IsActive=true, StringValue="3"},
                        new BlogEntry { ID=Guid.NewGuid(), Body="Homer's fourth blog entry (deleted)", IsDeleted=true, IsActive=true, StringValue="4"},
                        new BlogEntry { ID=Guid.NewGuid(), Body="Homer's 5th blog entry (inactive)", IsDeleted=false, IsActive=false, StringValue="5"},
                        new BlogEntry { ID=Guid.NewGuid(), Body="Homer's 6th blog entry (deleted and inactive)", IsDeleted=true, IsActive=false, StringValue="6"},
                    }
                };
                Accounts.Add(homer);

                var bart = new Account
                {
                    ID = Guid.NewGuid(),
                    UserName = "bart",
                    BlogEntries = new List<BlogEntry>
                    {
                        new BlogEntry { ID=Guid.NewGuid(), Body="Bart's first blog entry", IsDeleted=false, IsActive=true, StringValue="7"},
                        new BlogEntry { ID=Guid.NewGuid(), Body="Bart's second blog entry", IsDeleted=false, IsActive=true, StringValue="8"},
                        new BlogEntry { ID=Guid.NewGuid(), Body="Bart's third blog entry", IsDeleted=false, IsActive=true, StringValue="9"},
                        new BlogEntry { ID=Guid.NewGuid(), Body="Bart's fourth blog entry (deleted)", IsDeleted=true, IsActive=true, StringValue="10"},
                        new BlogEntry { ID=Guid.NewGuid(), Body="Bart's fifth blog entry (deleted)", IsDeleted=true, IsActive=true, StringValue="11"},
                        new BlogEntry { ID=Guid.NewGuid(), Body="Bart's 6th blog entry (inactive)", IsDeleted=false, IsActive=false, StringValue="12"},
                        new BlogEntry { ID=Guid.NewGuid(), Body="Bart's 7th blog entry (deleted and inactive)", IsDeleted=true, IsActive=false, StringValue="13"},
                    }
                };
                Accounts.Add(bart);

                SaveChanges();
            }
        }

        #endregion
    }


}
