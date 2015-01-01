using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations.History;

namespace EntityFramework.DynamicFilters.Example
{

    public class MySqlConfiguration : DbConfiguration
    {
        public MySqlConfiguration()
        {
            //  Necessary because MySQL doesn't allow key length > 767.  The default __MigrationHistory table exceeds that.
            SetHistoryContext("MySql.Data.MySqlClient", (conn, schema) => new MySqlHistoryContext(conn, schema));
        }
    }

    public class MySqlHistoryContext : HistoryContext
    {
        public MySqlHistoryContext(DbConnection existingConnection, string defaultSchema)
            : base(existingConnection, defaultSchema)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<HistoryRow>().Property(h => h.MigrationId).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<HistoryRow>().Property(h => h.ContextKey).HasMaxLength(200).IsRequired();
        }
    }


    public class ExampleContext : DbContext
    {
        //  A static/globally scoped value that will be used to restrict queries against the BlogEntries table.
        public static Guid CurrentAccountID { get; set; }

        public ExampleContext()
        {
            Database.SetInitializer<ExampleContext>(new ContentInitializer());
            Database.Log = log => System.Diagnostics.Debug.WriteLine(log);
            Database.Initialize(false);
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<BlogEntry> BlogEntries { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Initialize the Dynamic Filters
            this.InitializeDynamicFilters();

            //  Filter to restrict all queries on BlogEntries to only those for the current user.
            //  Global filter is used here with a delegate so that it's evaluated every time it's needed.
            //  ** IMPORTANT: Make sure if a delegate/Func<object> is used for the value that it has *GLOBAL*
            //  scope in your application.  In this example, CurrentAccountID is static.  If it were not static,
            //  setting the CurrentAccountID would have no effect since the Func would be using the instance of
            //  the DbContext that was used in the call to OnModelCreating - which only happens once per app lifetime.
            //modelBuilder.Filter("BlogEntriesForCurrentUser", (BlogEntry b) => b.AccountID, () => CurrentAccountID);
            //modelBuilder.Filter("BlogEntriesForCurrentUser", (BlogEntry b, Guid accountID) => b.AccountID==accountID, () => CurrentAccountID);

            //  Global filter on any class implementing ISoftDelete to match on records with IsDeleted=false
            //modelBuilder.Filter("IsDeleted", (ISoftDelete d) => d.IsDeleted, false);
            //modelBuilder.Filter("IsDeleted", (BlogEntry b, bool isDeleted) => (b.IsDeleted == isDeleted), false);

            modelBuilder.Filter("BlogEntryFilter", (BlogEntry b, Guid accountID, bool isDeleted) => (b.AccountID == accountID) && (b.IsDeleted == isDeleted), 
                                                () => CurrentAccountID, () => false);

            //  Filter to test handling of entity properties that are mapped to a different conceptual name.
            //modelBuilder.Filter("ConceptualNameTest", (Account a) => a.RemappedEntityProp, false);
            modelBuilder.Filter("ConceptualNameTest", (Account a, bool remappedEntityProp) => a.RemappedEntityProp == remappedEntityProp, false);


            //  TODO: This generates invalid sql.  Creates where clause like this: WHERE  NOT ([Var_1].[IsDeleted])
            //modelBuilder.Filter("IsDeleted", (ISoftDelete d) => !d.IsDeleted);

            //  TODO: Support Contains() in lambda
            //List<Guid> accounts = new List<Guid> { Guid.Parse("3A298D91-3857-E411-829F-001C428D83FF"), Guid.Parse("3B298D91-3857-E411-829F-001C428D83FF") };
            //modelBuilder.Filter("BlogEntriesForCurrentUser", (BlogEntry b, List<Guid> accountList) => accountList.Contains(b.AccountID), () => accounts);
        }
    }

    public class ContentInitializer : DropCreateDatabaseAlways<ExampleContext>
    {
        protected override void Seed(ExampleContext context)
        {
            System.Diagnostics.Debug.Print("Seeding db");

            //  Seeds 2 accounts with 9 blog entries, 4 of which are deleted

            var homer = new Account
            {
                UserName = "homer",
                BlogEntries = new List<BlogEntry>
                {
                    new BlogEntry { Body="Homer's first blog entry", IsDeleted=false, IsActive=true},
                    new BlogEntry { Body="Homer's second blog entry", IsDeleted=false, IsActive=true},
                    new BlogEntry { Body="Homer's third blog entry (deleted)", IsDeleted=true, IsActive=true},
                    new BlogEntry { Body="Homer's fourth blog entry (deleted)", IsDeleted=true, IsActive=true},
                    new BlogEntry { Body="Homer's 5th blog entry (inactive)", IsDeleted=false, IsActive=false},
                    new BlogEntry { Body="Homer's 6th blog entry (deleted and inactive)", IsDeleted=true, IsActive=false},
                }
            };
            context.Accounts.Add(homer);

            var bart = new Account
            {
                UserName = "bart",
                BlogEntries = new List<BlogEntry>
                {
                    new BlogEntry { Body="Bart's first blog entry", IsDeleted=false, IsActive=true},
                    new BlogEntry { Body="Bart's second blog entry", IsDeleted=false, IsActive=true},
                    new BlogEntry { Body="Bart's third blog entry", IsDeleted=false, IsActive=true},
                    new BlogEntry { Body="Bart's fourth blog entry (deleted)", IsDeleted=true, IsActive=true},
                    new BlogEntry { Body="Bart's fifth blog entry (deleted)", IsDeleted=true, IsActive=true},
                    new BlogEntry { Body="Bart's 6th blog entry (inactive)", IsDeleted=false, IsActive=false},
                    new BlogEntry { Body="Bart's 7th blog entry (deleted and inactive)", IsDeleted=true, IsActive=false},
                }
            };
            context.Accounts.Add(bart);

            context.SaveChanges();
        }
    }
}
