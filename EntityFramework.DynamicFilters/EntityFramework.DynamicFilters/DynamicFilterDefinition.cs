
namespace EntityFramework.DynamicFilters
{
    internal class DynamicFilterDefinition
    {
        public string FilterName { get; private set; }
        public string ColumnName { get; private set; }

        public string AttributeName { get { return string.Concat(DynamicFilterConstants.ATTRIBUTE_NAME_PREFIX, DynamicFilterConstants.DELIMETER, FilterName); } }
        public string ParameterName { get { return string.Concat(DynamicFilterConstants.PARAMETER_NAME_PREFIX, DynamicFilterConstants.DELIMETER, FilterName, DynamicFilterConstants.DELIMETER, ColumnName); } }

        internal DynamicFilterDefinition(string filterName, string columnName)
        {
            FilterName = filterName;
            ColumnName = columnName;
        }
    }
}
