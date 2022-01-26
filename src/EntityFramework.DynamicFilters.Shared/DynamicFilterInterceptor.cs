using System;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.Entity.Migrations.History;
using System.Linq;

namespace EntityFramework.DynamicFilters
{
    public class DynamicFilterInterceptor : IDbCommandTreeInterceptor
    {
        public void TreeCreated(DbCommandTreeInterceptionContext interceptionContext)
        {
            // https://github.com/zzzprojects/EntityFramework.DynamicFilters/issues/153
            if (DynamicFilterManager.ShouldIgnoreDynamicFilterInterceptor != null && DynamicFilterManager.ShouldIgnoreDynamicFilterInterceptor(interceptionContext)) return;

            var queryCommand = interceptionContext.Result as DbQueryCommandTree;
            if (queryCommand != null)
            {
                var context = interceptionContext.DbContexts.FirstOrDefault();
                if (context != null)
                {
                    DbExpressionVisitor<DbExpression> visitor;
#if (USE_CSPACE)
                    //  Intercepting CSpace instead of SSpace gives us access to all of the navigation properties
                    //  so we are able to handle filters on them as well!
                    if (interceptionContext.OriginalResult.DataSpace == DataSpace.CSpace)
                        visitor = new DynamicFilterQueryVisitorCSpace(context);
                    else if ((interceptionContext.OriginalResult.DataSpace == DataSpace.SSpace) && DynamicFilterQueryVisitorCSpace.DoesNotSupportElementMethod(context))
                    {
                        //  Some database (currently Oracle & MySQL) do not support the DbExpression.Element() method that
                        //  is needed when applying filters to child entities.  To work around that, we need to also
                        //  visit the query using this SSpace visitor which will apply those filters on all of the DbScan visits.
                        visitor = new DynamicFilterQueryVisitorSSpace(context);
                    }
                    else
                        return;
#else
                    //  Old method of visiting only in SSpace.  Does not support navigation properties but left
                    //  here in case need to revert or compare results.
                    if (interceptionContext.OriginalResult.DataSpace == DataSpace.SSpace)
                        visitor = new DynamicFilterQueryVisitorOld(context);
                    else
                        return;
#endif

                    var newQuery = queryCommand.Query.Accept(visitor);

                    //  When using CSpace, must set the useDatabaseNullSemantics parameter to false or nullable types will not have
                    //  null values translated to sql like this: ([Extent1].[TenantID] = @p__linq__0) OR (([Extent1].[TenantID] IS NULL) AND (@p__linq__0 IS NULL))
                    //  and will instead just generate sql like this: [Extent1].[TenantID] = @p__linq__0
                    //  If the parameter is not specified, it defaults to true...
                    interceptionContext.Result = new DbQueryCommandTree(queryCommand.MetadataWorkspace, queryCommand.DataSpace, newQuery, true, 
                                                                        (interceptionContext.OriginalResult.DataSpace != DataSpace.CSpace));
                }
            }

            //  Can also check for other command types such as DbDeleteCommandTree and DbUpdateCommandTree to change
            //  their behaviors as well.  See https://github.com/rowanmiller/Demo-TechEd2014/blob/master/FakeEstate.ListingManager/Models/EFHelpers/SoftDeleteInterceptor.cs
            //}
        }
    }

}
