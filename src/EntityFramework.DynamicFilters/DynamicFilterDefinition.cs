
using System;
namespace EntityFramework.DynamicFilters
{
    internal class DynamicFilterDefinition
    {
        public string FilterName { get; private set; }
        public string ColumnName { get; private set; }
        public Type CLRType { get; private set; }

        public string AttributeName { get { return string.Concat(DynamicFilterConstants.ATTRIBUTE_NAME_PREFIX,  DynamicFilterConstants.DELIMETER, CLRType.Name, DynamicFilterConstants.DELIMETER, FilterName); } }
        public string ParameterName { get { return string.Concat(DynamicFilterConstants.PARAMETER_NAME_PREFIX, DynamicFilterConstants.DELIMETER, FilterName, DynamicFilterConstants.DELIMETER, ColumnName); } }

        internal DynamicFilterDefinition(string filterName, string columnName, Type clrType)
        {
            FilterName = filterName;
            ColumnName = columnName;
            CLRType = clrType;
        }
    }
}
