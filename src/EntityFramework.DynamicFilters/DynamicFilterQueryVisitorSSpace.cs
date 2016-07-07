#if DEBUG
#define DEBUG_VISITS
#endif

using System;
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
    /// <summary>
    /// This visitor is only required if the database/EF provider does not support the DbExpression.Element()
    /// method that is needed when we are applying filters to child entities in the CSpace visitor.
    /// To work around that, we use this visitor to visit the DbScan expressions to apply those filters.
    /// If those filters reference child properties/navigation properties, they will fail translation here...
    /// </summary>
    public class DynamicFilterQueryVisitorSSpace : DefaultExpressionVisitor
    {
        private readonly DbContext _DbContext;
        private readonly ObjectContext _ObjectContext;

        public DynamicFilterQueryVisitorSSpace(DbContext contextForInterception)
        {
            _DbContext = contextForInterception;
            _ObjectContext = ((IObjectContextAdapter)contextForInterception).ObjectContext;
        }

        private bool _IgnoreNextDbScan = true;
        public override DbExpression Visit(DbScanExpression expression)
        {
#if DEBUG_VISITS
            System.Diagnostics.Debug.Print("Visit(DbScanExpression): Target.Name={0}", expression.Target.Name);
#endif
            //  If using SSpace:
            //  This method will be called for all query expressions.  If there is a filter included (in a .Where() for example),
            //  Visit(DbFilterExpression) will be called first and our dynamic filters will have already been included.
            //  Otherwise, we do that here.
            //  If using CSpace:
            //  This method will only be called for the initial entity.  Any other references to other entities will be
            //  handled by the Visit(DbPropertyExpression) - this includes filter references and includes.

            var baseResult = base.Visit(expression);
            if (_IgnoreNextDbScan)
            {
                _IgnoreNextDbScan = false;
                return baseResult;
            }

            string entityName = expression.Target.Name;
            var filterList = FindFiltersForEntitySet(expression.Target.ElementType.MetadataProperties, expression.Target.EntityContainer);

            if (filterList.Any())
            {
                var binding = DbExpressionBuilder.Bind(baseResult);
                var newFilterExpression = BuildFilterExpressionWithDynamicFilters(entityName, filterList, binding, null);
                if (newFilterExpression != null)
                {
                    //  If not null, a new DbFilterExpression has been created with our dynamic filters.
                    return newFilterExpression;
                }
            }

            return baseResult;
        }

        private IEnumerable<DynamicFilterDefinition> FindFiltersForEntitySet(ReadOnlyMetadataCollection<MetadataProperty> metadataProperties, EntityContainer entityContainer)
        {
            var filterList = metadataProperties
                .Where(mp => mp.Name.Contains("customannotation:" + DynamicFilterConstants.ATTRIBUTE_NAME_PREFIX))
                .Select(m => m.Value as DynamicFilterDefinition)
                .ToList();

            //  Note: Prior to the switch to use CSpace (which was done to allow filters on navigation properties),
            //  we had to remove filters that exist in base EntitySets to this entity to fix issues with 
            //  Table-per-Type inheritance (issue #32).  In CSpace none of that is necessary since we are working
            //  with the actual c# models now (in CSpace) so we always have the correct filters and access to all
            //  the inherited properties that we need.
            if (filterList.Any())
            {
                //  Recursively remove any filters that exist in base EntitySets to this entity.
                //  This happens when an entity uses Table-per-Type inheritance.  Filters will be added
                //  to all derived EntitySets because of the inheritance in the C# classes.  But the database
                //  representation (the EntitySet) does not give access to inherited propeties since they
                //  only exist in the child EntitySet.  And on queries of entities involved in TPT, the
                //  query will generate a DbScanExpression for each EntitySet - so we only want the filters
                //  applied to the DbScanExpression to which they apply.
                //  See issue #32.
                RemoveFiltersForBaseClass(filterList.First().CLRType, filterList, entityContainer);
            }

            return filterList;
        }

        private void RemoveFiltersForBaseClass(Type clrType, List<DynamicFilterDefinition> filterList, EntityContainer entityContainer)
        {
            if (!filterList.Any())
                return;

            //  Find the base class (if there is one) for clrType
            var baseCLRType = clrType.BaseType;
            if (baseCLRType.FullName == "System.Object")
                return;     //  No base

            //  Find the EntitySet for the base type (if there is one)
            var baseEntitySet = entityContainer.EntitySets
                .Where(e => e.ElementType.MetadataProperties.Any(mp => mp.Name.Contains("customannotation:" + DynamicFilterConstants.ATTRIBUTE_NAME_PREFIX)
                                                                        && (((DynamicFilterDefinition)mp.Value).CLRType == baseCLRType)))
                .FirstOrDefault();
            if (baseEntitySet == null)
                return;     //  Base class does not have an EntitySet or it does not contain a filter - so not using TPT/nothing to do

            //  baseCLRType has an EntitySet that has filters on it.  This means the entities are using Table-per-Type inheritance.
            //  We need to remove filters from filterList that also exist in the baseEntitySet.  If we tried to process the
            //  fiter on the super class, it would fail since it doesn't have the properties (database columns) in it.
            var baseFilterNameList = new HashSet<string>(baseEntitySet.ElementType.MetadataProperties
                .Where(mp => mp.Name.Contains("customannotation:" + DynamicFilterConstants.ATTRIBUTE_NAME_PREFIX))
                .Select(m => ((DynamicFilterDefinition)m.Value).FilterName));
            filterList.RemoveAll(f => baseFilterNameList.Contains(f.FilterName));

            //  Check base classes of baseCLRType
            RemoveFiltersForBaseClass(baseCLRType, filterList, entityContainer);
        }

        private DbFilterExpression BuildFilterExpressionWithDynamicFilters(string entityName, IEnumerable<DynamicFilterDefinition> filterList, DbExpressionBinding binding, DbExpression predicate)
        {
            if (!filterList.Any())
                return null;

            var edmType = binding.VariableType.EdmType as EntityType;
            if (edmType == null)
                return null;

            List<DbExpression> conditionList = new List<DbExpression>();

            HashSet<string> processedFilterNames = new HashSet<string>();
            foreach (var filter in filterList)
            {
                if (processedFilterNames.Contains(filter.FilterName))
                    continue;       //  Already processed this filter - attribute was probably inherited in a base class
                processedFilterNames.Add(filter.FilterName);

                DbExpression dbExpression;
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
                    var param = columnProperty.Property.TypeUsage.Parameter(filter.CreateDynamicFilterName(filter.ColumnName, DataSpace.SSpace));

                    //  When using SSpace, need some special handling for an Oracle Boolean property.
                    //  Not necessary when using CSpace since the translation into the Oracle types has not happened yet.
                    if ((columnProperty.ResultType.EdmType.FullName == "Edm.Boolean")
                        && param.ResultType.EdmType.FullName.StartsWith("Oracle", StringComparison.CurrentCultureIgnoreCase) && (param.ResultType.EdmType.Name == "number"))    //  Don't trust Oracle's type name to stay the same...
                    {
                        //  Special handling needed for columnProperty boolean.  For some reason, the Oracle EF driver does not correctly
                        //  set the ResultType to a number(1) in columnProperty like it does in columnProperty.Property.TypeUsage.  That
                        //  results in us trying to do a comparison of a Boolean to a number(1) which causes DbExpressionBuilder.Equal
                        //  to throw an exception.  To get this to process correctly, we need to do a cast on the columnProperty to
                        //  "number(1)" so that it matches the param.ResultType.  And that results in the sql sent to Oracle converting
                        //  the column to the type that it already is...
                        dbExpression = DbExpressionBuilder.Equal(DbExpressionBuilder.CastTo(columnProperty, param.ResultType), param);
                    }
                    else
                        dbExpression = DbExpressionBuilder.Equal(columnProperty, param);
                }
                else if (filter.Predicate != null)
                {
                    //  Lambda expression filter
                    dbExpression = LambdaToDbExpressionVisitor.Convert(filter, binding, _DbContext, DataSpace.SSpace);
                }
                else
                    throw new System.ArgumentException(string.Format("Filter {0} does not contain a ColumnName or a Predicate!", filter.FilterName));

                if (DynamicFilterExtensions.AreFilterDisabledConditionsAllowed(filter.FilterName))
                {
                    //  Create an expression to check to see if the filter has been disabled and include that check with the rest of the filter expression.
                    //  When this parameter is null, the filter is enabled.  It will be set to true (in DynamicFilterExtensions.GetFilterParameterValue) if
                    //  the filter has been disabled.
                    var boolPrimitiveType = LambdaToDbExpressionVisitor.TypeUsageForPrimitiveType(typeof(bool?), _ObjectContext, DataSpace.SSpace);
                    var isDisabledParam = boolPrimitiveType.Parameter(filter.CreateFilterDisabledParameterName(DataSpace.SSpace));

                    conditionList.Add(DbExpressionBuilder.Or(dbExpression, DbExpressionBuilder.Not(DbExpressionBuilder.IsNull(isDisabledParam))));
                }
                else
                    conditionList.Add(dbExpression);
            }

            int numConditions = conditionList.Count;
            DbExpression newPredicate;
            switch (numConditions)
            {
                case 0:
                    return null;

                case 1:
                    newPredicate = conditionList.First();
                    break;

                default:
                    //  Have multiple conditions.  Need to append them together using 'and' conditions.
                    newPredicate = conditionList.First();

                    for (int i = 1; i < numConditions; i++)
                        newPredicate = newPredicate.And(conditionList[i]);
                    break;
            }

            //  'and' the existing Predicate if there is one
            if (predicate != null)
                newPredicate = newPredicate.And(predicate);

            return DbExpressionBuilder.Filter(binding, newPredicate);
        }

        #region Extra (currently not needed) Visit calls - for debugging

#if DEBUG_VISITS

        public override DbExpression Visit(DbVariableReferenceExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbVariableReferenceExpression): VariableName={0}, ResultType.EdmType.Name={1}", expression.VariableName, expression.ResultType.EdmType.Name);
            return base.Visit(expression);
        }

        public override DbExpression Visit(DbApplyExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbApplyExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbCrossJoinExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbCrossJoinExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbJoinExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbJoinExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbLambdaExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbLambdaExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbNewInstanceExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbNewInstanceExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbParameterReferenceExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbParameterReferenceExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbProjectExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbProjectExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbRelationshipNavigationExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbRelationshipNavigationExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbDerefExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbDerefExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbElementExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbElementExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbEntityRefExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbEntityRefExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbRefExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbRefExpression): {0}", expression);
            return base.Visit(expression);
        }
        public override DbExpression Visit(DbRefKeyExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbRefKeyExpression): {0}", expression);
            return base.Visit(expression);
        }

#endif

        #endregion
    }
}
