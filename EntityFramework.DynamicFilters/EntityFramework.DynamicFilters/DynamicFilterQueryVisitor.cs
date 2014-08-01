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
                var binding = DbExpressionBuilder.Bind(current);
                var columnProperty = DbExpressionBuilder.Property(DbExpressionBuilder.Variable(binding.VariableType, binding.VariableName), filter.ColumnName);
                current = DbExpressionBuilder.Filter(binding, DbExpressionBuilder.Equal(columnProperty, columnProperty.Property.TypeUsage.Parameter(filter.ParameterName)));
            }

            return current;
        }
    }
}
