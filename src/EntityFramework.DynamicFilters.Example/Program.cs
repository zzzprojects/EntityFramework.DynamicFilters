using System;
using System.Collections.Generic;
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

            //var list = context1.BlogEntries.ToList();
            //System.Diagnostics.Debug.Print("Got {0} items", list.Count());

            Console.WriteLine("");
            Console.WriteLine("Querying with IsDeleted filter enabled");
            Query(context1, "homer", 2, true);
            Query(context1, "bart", 3, true);

            {
                //  Query each account with the IsDeleted filter disabled.  The filter is disabled
                //  by setting the value to null using SetFilterScopedParameterValue() so that it applies
                //  ONLY to this context and will not affect any other active or future contexts.
                var context2 = new ExampleContext();
                context2.DisableFilter("BlogEntryFilter");

                Console.WriteLine("");
                Console.WriteLine("Querying with BlogEntryFilter filter disabled");
                Query(context2, "homer", 9, false);
                Query(context2, "bart", 9, false);

                //  Re-enable the filter and disable only the isDeleted check
                context2.EnableFilter("BlogEntryFilter");
                context2.SetFilterScopedParameterValue("BlogEntryFilter", "isDeleted", null);
                Console.WriteLine("");
                Console.WriteLine("Querying with BlogEntryFilter enabled and isDeleted check disabled");
                Query(context2, "homer", 4, true);
                Query(context2, "bart", 5, true);

                //  Change isDeleted param to true to re-enable it
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

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
        }

        private static void Query(ExampleContext context, string userName, int expected, bool blogFilterIsEnabled, bool reusedContext = false)
        {
            var account = context.Accounts
                .Include(a => a.BlogEntries)
                .Where(a => a.UserName == userName).FirstOrDefault();

            //  Set the CurrentAccountID in the static/global property used in the filter definition (see ExampleContext.OnModelCreating)
            //  For creating a filter on the current user, this could come from the Thread.CurrentPrincipal...
            ExampleContext.CurrentAccountID = account.ID;

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
                    System.Diagnostics.Debug.Assert(reusedContext);
                }
            }

            //  Query blog entries.  This will use the global filter created in ExampleContext.OnModelCreating.
            //  Filter on IsActive is to reproduce a situation seen with MySQL that was adding those filters and not enclosing them in ()'s so the
            //  'or' condition here was not properly enclosed - caused our dynamic filters to not be used correctly.
            bool? active = true;
            var blogEntries = context.BlogEntries.Where(b => (!active.HasValue || (b.IsActive == active.Value))).ToList();
            System.Diagnostics.Debug.Assert(blogEntries.Count == expected);

            Console.WriteLine(string.Format("Current User = {0}: Selected {1} blog entries", userName, blogEntries.Count));
            if (blogEntries.Count != expected)
                Console.WriteLine(string.Format("  *** Expected {0} blog entries!", expected));
        }
    }
}
