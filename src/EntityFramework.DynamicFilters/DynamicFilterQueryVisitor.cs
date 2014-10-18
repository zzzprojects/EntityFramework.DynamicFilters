using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq;

namespace EntityFramework.DynamicFilters
{
    public class DynamicFilterQueryVisitor : DefaultExpressionVisitor
    {
        public override DbExpression Visit(DbScanExpression expression)
        {
            var filterList = expression.Target.ElementType.MetadataProperties
                .Where(mp => mp.Name.Contains("customannotation:" + DynamicFilterConstants.ATTRIBUTE_NAME_PREFIX))
                .Select(m => m.Value as DynamicFilterDefinition);

            DbExpression current = base.Visit(expression);

            foreach(var filter in filterList)
            {
                //  Bind the filter parameter to a sql parameter
                var binding = DbExpressionBuilder.Bind(current);
                var columnProperty = DbExpressionBuilder.Property(DbExpressionBuilder.Variable(binding.VariableType, binding.VariableName), filter.ColumnName);
                var param = columnProperty.Property.TypeUsage.Parameter(filter.ParameterName);

                //  Creates an expression to match on the filter value *OR* a null filter value.  Null can be used to disable the filter completely.
                current = DbExpressionBuilder.Filter(binding, DbExpressionBuilder.Or(DbExpressionBuilder.Equal(columnProperty, param), DbExpressionBuilder.IsNull(param)));
            }

            return current;
        }
    }
}
