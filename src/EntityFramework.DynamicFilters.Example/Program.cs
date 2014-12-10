using System;
using System.Data.Entity;
using System.Linq;

namespace EntityFramework.DynamicFilters.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            //  Run a query for each Account using the default/global filters.
            var context1 = new ExampleContext();
            Console.WriteLine("");
            Console.WriteLine("Querying with IsDeleted filter enabled");
            Query(context1, "homer", 2);
            Query(context1, "bart", 3);

            {
                //  Query each account with the IsDeleted filter disabled.  The filter is disabled
                //  by setting the value to null using SetFilterScopedParameterValue() so that it applies
                //  ONLY to this context and will not affect any other active or future contexts.
                var context2 = new ExampleContext();
                context2.SetFilterScopedParameterValue("IsDeleted", null);  //  Setting to null disables filter (so yes, at the moment, can't filter on null values)

                Console.WriteLine("");
                Console.WriteLine("Querying with IsDeleted filter disabled");
                Query(context2, "homer", 4);
                Query(context2, "bart", 5);

                //  Change the IsDeleted filter to query on IsDeleted=true
                context2.SetFilterScopedParameterValue("IsDeleted", true);

                Console.WriteLine("");
                Console.WriteLine("Querying for deleted records only");
                Query(context2, "homer", 2, true);
                Query(context2, "bart", 2, true);
            }

            //  Re-query using the original context1 object to demonstrate that the changes
            //  made to context2 have no effect on it and were properly scoped.
            Console.WriteLine("");
            Console.WriteLine("Re-Querying with original context (IsDeleted filter enabled)");
            Query(context1, "homer", 2);
            Query(context1, "bart", 3);

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
        }

        private static void Query(ExampleContext context, string userName, int expected, bool reusedContext = false)
        {
            var account = context.Accounts
                .Include(a => a.BlogEntries)
                .Where(a => a.UserName == userName).FirstOrDefault();

            //  Set the CurrentAccountID in the static/global property used in the filter definition (see ExampleContext.OnModelCreating)
            //  For creating a filter on the current user, this could come from the Thread.CurrentPrincipal...
            ExampleContext.CurrentAccountID = account.ID;

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
                System.Diagnostics.Debug.Assert(reusedContext);
            }

            //  Query blog entries.  This will use the global filter created in ExampleContext.OnModelCreating.
            //  Filter on IsActive is to reproduce a situation seen with MySQL that was adding those filters and not enclosing them in ()'s so the
            //  'or' condition here was not properly enclosed - caused our dynamic filters to not be used correctly.
            bool? active = true;
            var blogEntries = context.BlogEntries.Where(b => (!active.HasValue || (b.IsActive == active.Value))).ToList();
            //var blogEntries = context.BlogEntries.ToList();
            System.Diagnostics.Debug.Assert(blogEntries.Count == expected);

            Console.WriteLine(string.Format("Current User = {0}: Selected {1} blog entries", userName, blogEntries.Count));
        }
    }
}
