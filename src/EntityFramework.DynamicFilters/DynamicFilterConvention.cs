using System;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq.Expressions;

namespace EntityFramework.DynamicFilters
{
    internal class DynamicFilterConvention : Convention
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterName"></param>
        /// <param name="entityType"></param>
        /// <param name="predicate"></param>
        /// <param name="columnName"></param>
        /// <param name="selectEntityTypeCondition">If not null, this delegate should return true if the filter should be applied to the given entity Type.
        /// False if not.  Allows additional logic to be applied to determine if the filter should be applied to an Entity of type "TEntity".
        /// i.e. To apply the filter to all entities of a particular interface but not if those entities also implement another interface.</param>
        public DynamicFilterConvention(string filterName, Type entityType, LambdaExpression predicate,
                                        string columnName, Func<Type, bool> selectEntityTypeCondition)
        {
            var configuration = Types().Where(t => entityType.IsAssignableFrom(t) && ((selectEntityTypeCondition == null) || selectEntityTypeCondition(t)));
            configuration.Configure(ctc =>
            {
                var filterDefinition = new DynamicFilterDefinition(filterName, predicate, columnName, ctc.ClrType);

                ctc.HasTableAnnotation(filterDefinition.AttributeName, filterDefinition);
            });
        }
    }
}
