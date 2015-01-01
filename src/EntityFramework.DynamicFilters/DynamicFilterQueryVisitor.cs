using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace EntityFramework.DynamicFilters
{
    public class DynamicFilterQueryVisitor : DefaultExpressionVisitor
    {
        private readonly DbContext _ContextForInterception;
        private readonly ObjectContext _ObjectContext;

        public DynamicFilterQueryVisitor(DbContext contextForInterception)
        {
            _ContextForInterception = contextForInterception;
            _ObjectContext = ((IObjectContextAdapter)contextForInterception).ObjectContext;
        }

        public override DbExpression Visit(DbScanExpression expression)
        {
            var filterList = expression.Target.ElementType.MetadataProperties
                .Where(mp => mp.Name.Contains("customannotation:" + DynamicFilterConstants.ATTRIBUTE_NAME_PREFIX))
                .Select(m => m.Value as DynamicFilterDefinition);

            var baseResult = base.Visit(expression);
            if (!filterList.Any())
                return baseResult;

            var binding = DbExpressionBuilder.Bind(baseResult);

            var edmType = binding.VariableType.EdmType as EntityType;
            if (edmType == null)
                return baseResult;  //  ???

            List<DbExpression> conditionList = new List<DbExpression>();

            foreach (var filter in filterList)
            {
                if (!string.IsNullOrEmpty(filter.ColumnName))
                {
                    //  Single column equality filter
                    //  Need to map through the EdmType properties to find the actual database/cspace name for the entity property.
                    //  It may be different from the entity property!
                    var edmProp = edmType.Properties.Where(p => p.MetadataProperties.Any(m => m.Name == "PreferredName" && m.Value.Equals(filter.ColumnName))).FirstOrDefault();
                    if (edmProp == null)
                        continue;       //  ???
                    //  database column name is now in edmProp.Name.  Use that instead of filter.ColumnName

                    var columnProperty = DbExpressionBuilder.Property(DbExpressionBuilder.Variable(binding.VariableType, binding.VariableName), edmProp.Name);
                    var param = columnProperty.Property.TypeUsage.Parameter(filter.CreateDynamicFilterName(filter.ColumnName));

                    //  Create an expression to match on the filter value *OR* a null filter value.  Null can be used to disable the filter completely.
                    conditionList.Add(DbExpressionBuilder.Or(DbExpressionBuilder.Equal(columnProperty, param), DbExpressionBuilder.IsNull(param)));
                }
                else if (filter.Predicate != null)
                {
                    //  Lambda expression filter
                    var dbExpression = LambdaToDbExpressionVisitor.Convert(filter, binding, _ObjectContext);
                    conditionList.Add(dbExpression);
                }
            }

            int numConditions = conditionList.Count;
            switch (numConditions)
            {
                case 0:
                    return baseResult;

                case 1:
                    return DbExpressionBuilder.Filter(binding, conditionList.First());

                default:
                    //  Have multiple conditions.  Need to append them together using 'and' conditions.
                    var leftExpression = conditionList.First();

                    for (int i = 1; i < numConditions; i++)
                        leftExpression = leftExpression.And(conditionList[i]);

                    return DbExpressionBuilder.Filter(binding, leftExpression);
            }
        }
    }
}
