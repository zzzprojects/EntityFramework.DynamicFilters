using System;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq.Expressions;

namespace EntityFramework.DynamicFilters
{
    internal class DynamicFilterConvention : Convention
    {
        /// <summary>
        /// </summary>
        /// <param name="filterName"></param>
        /// <param name="entityType"></param>
        /// <param name="predicate"></param>
        /// <param name="columnName"></param>
        /// <param name="config">Options for how and when to apply this filter</param>
        public DynamicFilterConvention(string filterName, Type entityType, LambdaExpression predicate,
                                        string columnName, Func<DynamicFilterConfig, DynamicFilterOptions> config)
        {
            var options = (config == null) ? new DynamicFilterOptions() : config(new DynamicFilterConfig());

            var id = Guid.NewGuid();

            var configuration = Types().Where(t => entityType.IsAssignableFrom(t) && ((options.SelectEntityTypeCondition == null) || options.SelectEntityTypeCondition(t)));
            configuration.Configure(ctc =>
            {
                var filterDefinition = new DynamicFilterDefinition(id, filterName, predicate, columnName, ctc.ClrType, options);

                ctc.HasTableAnnotation(filterDefinition.AttributeName, filterDefinition);
            });
        }
    }
}
