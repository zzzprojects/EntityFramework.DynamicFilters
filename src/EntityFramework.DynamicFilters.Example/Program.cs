using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

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
                context2.DisableFilter("BlogEntryFilter");

                Console.WriteLine("");
                Console.WriteLine("Querying with BlogEntryFilter filter disabled");
                Query(context2, "homer", 9);
                Query(context2, "bart", 9);

                //  Re-enable the filter and disable only the isDeleted check
                context2.EnableFilter("BlogEntryFilter");
                context2.SetFilterScopedParameterValue("BlogEntryFilter", "isDeleted", null);
                Console.WriteLine("");
                Console.WriteLine("Querying with BlogEntryFilter enabled and isDeleted check disabled");
                Query(context2, "homer", 4);
                Query(context2, "bart", 5);

                //  Change isDeleted param to true to re-enable it
                context2.SetFilterScopedParameterValue("BlogEntryFilter", "isDeleted", true);

                Console.WriteLine("");
                Console.WriteLine("Querying for deleted records only");
                Query(context2, "homer", 2);
                Query(context2, "bart", 2);
            }

            //  Re-query using the original context1 object to demonstrate that the changes
            //  made to context2 have no effect on it and were properly scoped.
            Console.WriteLine("");
            Console.WriteLine("Re-Querying with original context");
            Query(context1, "homer", 2);
            Query(context1, "bart", 3);

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
        }

        private static void Query(ExampleContext context, string userName, int expected)
        {
            var account = context.Accounts.Where(a => a.UserName == userName).FirstOrDefault();

            //  Set the CurrentAccountID in the static/global property used in the filter definition (see ExampleContext.OnModelCreating)
            //  For creating a filter on the current user, this could come from the Thread.CurrentPrincipal...
            ExampleContext.CurrentAccountID = account.ID;

            //  Query blog entries.  This will use the global filter created in ExampleContext.OnModelCreating.
            var blogEntries = context.BlogEntries.ToList();
            System.Diagnostics.Debug.Assert(blogEntries.Count == expected);

            Console.WriteLine(string.Format("Current User = {0}: Selected {1} blog entries", userName, blogEntries.Count));
            if (blogEntries.Count != expected)
                Console.WriteLine(string.Format("  *** Expected {0} blog entries!", expected));
        }
    }
}
