using System;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq.Expressions;

namespace EntityFramework.DynamicFilters
{
    internal class DynamicFilterConvention : Convention
    {
        public DynamicFilterConvention(string filterName, Type entityType, string columnName)
            :this(filterName, entityType, null, columnName)
        {
        }

        public DynamicFilterConvention(string filterName, Type entityType, LambdaExpression predicate)
            : this(filterName, entityType, predicate, null)
        {
        }

        public DynamicFilterConvention(string filterName, Type entityType, LambdaExpression predicate,
                                        string columnName)
        {
            var configuration = Types().Where(entityType.IsAssignableFrom);
            configuration.Configure(ctc =>
            {
                var filterDefinition = new DynamicFilterDefinition(filterName, predicate, columnName, ctc.ClrType);

                ctc.HasTableAnnotation(filterDefinition.AttributeName, filterDefinition);
            });
        }
    }
}
