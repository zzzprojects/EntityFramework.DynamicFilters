using System;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace EntityFramework.DynamicFilters
{
    internal class DynamicFilterConvention : Convention
    {
        public DynamicFilterConvention(string filterName, Type entityType, string columnName)
        {
            var configuration = Types().Where(entityType.IsAssignableFrom);
            configuration.Configure(ctc =>
            {
                var filterDefinition = new DynamicFilterDefinition(filterName, columnName, ctc.ClrType);

                ctc.HasTableAnnotation(filterDefinition.AttributeName, filterDefinition);
            });
        }
    }
}
