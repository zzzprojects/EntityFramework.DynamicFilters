using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;

namespace EntityFramework.DynamicFilters
{
    public class DynamicFilterInterceptor : IDbCommandTreeInterceptor
    {
        public void TreeCreated(DbCommandTreeInterceptionContext interceptionContext)
        {
            if (interceptionContext.OriginalResult.DataSpace == DataSpace.SSpace)
            {
                var queryCommand = interceptionContext.Result as DbQueryCommandTree;
                if (queryCommand != null)
                {
                    var context = interceptionContext.DbContexts.FirstOrDefault();
                    if (context != null)
                    {
                        var newQuery = queryCommand.Query.Accept(new DynamicFilterQueryVisitor(context));
                        interceptionContext.Result = new DbQueryCommandTree(
                            queryCommand.MetadataWorkspace,
                            queryCommand.DataSpace,
                            newQuery);
                    }
                }

                //  Can also check for other command types such as DbDeleteCommandTree and DbUpdateCommandTree to change
                //  their behaviors as well.  See https://github.com/rowanmiller/Demo-TechEd2014/blob/master/FakeEstate.ListingManager/Models/EFHelpers/SoftDeleteInterceptor.cs
            }
        }
    }

}
