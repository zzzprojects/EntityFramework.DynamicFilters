#if DEBUG
//#define DEBUG_VISITS
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
    public class DynamicFilterQueryVisitor : DefaultExpressionVisitor
    {
        private readonly DbContext _ContextForInterception;
        private readonly ObjectContext _ObjectContext;

        public DynamicFilterQueryVisitor(DbContext contextForInterception)
        {
            _ContextForInterception = contextForInterception;
            _ObjectContext = ((IObjectContextAdapter)contextForInterception).ObjectContext;
        }

        //  Prior to switching to CSpace (to allow filters on navigation properties), also had to 
        //  override Visit(DbFilterExpression) to force the filter expression.Predicate to be parsed
        //  and build the filter expression.  When using CSpace, that is not necessary.  Those filters
        //  appear properly in the rest of the Visit overrides so no special handling is required.
#if !USE_CSPACE
        public override DbExpression Visit(DbFilterExpression expression)
        {
            //  If the query contains it's own filter condition (in a .Where() for example), this will be called
            //  before Visit(DbScanExpression).  And it will contain the Predicate specified in that filter.
            //  Need to inject our dynamic filters here and then 'and' the Predicate.  This is necessary so that
            //  the expressions are properly ()'d.
            //  It also allows us to attach our dynamic filter into the same DbExpressionBinding so it will avoid
            //  creating a new sub-query in MS SQL Server.
            var predicate = VisitExpression(expression.Predicate);      //  Visit the predicate so filters will be applied to any child properties inside it (issue #61)

            string entityName = expression.Input.Variable.ResultType.EdmType.Name;
            var containers = _ObjectContext.MetadataWorkspace.GetItems<EntityContainer>(DataSpace.SSpace).First();
            var filterList = FindFiltersForEntitySet(expression.Input.Variable.ResultType.EdmType.MetadataProperties, containers);

            var newFilterExpression = BuildFilterExpressionWithDynamicFilters(entityName, filterList, expression.Input, predicate);
            if (newFilterExpression != null)
            {
                //  If not null, a new DbFilterExpression has been created with our dynamic filters.
                return newFilterExpression;
            }

            return base.Visit(expression);
        }
#endif

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

            string entityName = expression.Target.Name;
            var filterList = FindFiltersForEntitySet(expression.Target.ElementType.MetadataProperties, expression.Target.EntityContainer);

            var baseResult = base.Visit(expression);
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

#if USE_CSPACE
        //  This is called for any navigation property reference so we can apply filters for those entities here.
        //  That includes any navigation properties referenced in functions (.Where() clauses) and also any
        //  child entities that are .Include()'d.
        public override DbExpression Visit(DbPropertyExpression expression)
        {
#if DEBUG_VISITS
            System.Diagnostics.Debug.Print("Visit(DbPropertyExpression): EdmType.Name={0}", expression.ResultType.ModelTypeUsage.EdmType.Name);
#endif
            var baseResult = base.Visit(expression);

            var basePropertyResult = baseResult as DbPropertyExpression;
            if (basePropertyResult == null)
                return baseResult;      //  base.Visit changed type!

            var navProp = basePropertyResult.Property as NavigationProperty;
            if (navProp != null)
            {
                var targetEntityType = navProp.ToEndMember.GetEntityType();

                string entityName = targetEntityType.Name;
                var containers = _ObjectContext.MetadataWorkspace.GetItems<EntityContainer>(DataSpace.CSpace).First();
                var filterList = FindFiltersForEntitySet(targetEntityType.MetadataProperties, containers);

                if (filterList.Any())
                {
                    //  If the expression contains a collection (i.e. the child property is an IEnumerable), we can bind directly to it.
                    //  Otherwise, we have to create a DbScanExpression over the ResultType in order to bind.
                    if (baseResult.ResultType.EdmType.BuiltInTypeKind == BuiltInTypeKind.CollectionType)
                    {
                        var binding = DbExpressionBuilder.Bind(baseResult);
                        var newFilterExpression = BuildFilterExpressionWithDynamicFilters(entityName, filterList, binding, null);
                        if (newFilterExpression != null)
                        {
                            //  If not null, a new DbFilterExpression has been created with our dynamic filters.
                            return newFilterExpression;
                        }
                    }
                    else if (baseResult.ResultType.EdmType.BuiltInTypeKind == BuiltInTypeKind.EntityType)
                    {
                        var entitySet = containers.EntitySets.FirstOrDefault(e => e.ElementType.Name == baseResult.ResultType.EdmType.Name);
                        var scanExpr = DbExpressionBuilder.Scan(entitySet);
                        var binding = DbExpressionBuilder.Bind(scanExpr);

                        //  Build the join conditions that are needed to join from the source object (basePropertyResult.Instance)
                        //  to the child object (the scan expression we just creating the binding for).
                        //  These conditions will be and'd with the filter conditions.
                        var associationType = navProp.RelationshipType as AssociationType;
                        if (associationType == null)
                            throw new ApplicationException(string.Format("Unable to find AssociationType on navigation property of single child property {0} in type {1}", navProp.Name, navProp.DeclaringType.FullName));
                        if (associationType.Constraint == null)
                        {
                            //  KNOWN_ISSUE:
                            //  If this happens, the model does not contain the foreign key (the "id" property).  EF will automatically generate
                            //  it based on naming rules when generating the SSpace/database models but does not expose the Constraint here in the
                            //  AssociationType.  In order for us to be able to generate the conditions correctly, those Foreign Keys need to be
                            //  specified on the model.  To fix/handle this, we would need to examine the SSpace Association Sets (which do have
                            //  these relations!!) to try to map that information back to CSpace to figure out the correct properties of the FK conditions.
                            //  or...the models just need to contain the necessary "ID" properties for the FK relations so that they are available here
                            //  (in CSpace) for us to generate the necessary join conditions.
                            throw new ApplicationException(string.Format("FK Constriant not found for association '{0}' - must directly specify foreign keys on model to be able to apply this filter", associationType.FullName));
                        }

                        //  Figure out if the "baseResults" are the from side or to side of the constraint so we can create the properties correctly
                        bool baseResultIsFromRole = (basePropertyResult.Instance.ResultType.EdmType == ((AssociationEndMember)associationType.Constraint.FromRole).GetEntityType());

                        DbExpression joinCondition = null;
                        for (int i = 0; i < associationType.Constraint.FromProperties.Count; i++)
                        {
                            var prop1 = DbExpressionBuilder.Property(basePropertyResult.Instance, baseResultIsFromRole ? associationType.Constraint.FromProperties[i] : associationType.Constraint.ToProperties[i]);
                            var prop2 = DbExpressionBuilder.Property(binding.Variable, baseResultIsFromRole ? associationType.Constraint.ToProperties[i] : associationType.Constraint.FromProperties[i]);

                            var condition = prop1.Equal(prop2) as DbExpression;
                            joinCondition = (joinCondition == null) ? condition : joinCondition.And(condition);
                        }

                        //  Translate the filter predicate into a DbExpression bound to the Scan expression of the target entity set.
                        //  Those conditions are then and'd with the join conditions necessary to join the target table with the source table.
                        var newFilterExpression = BuildFilterExpressionWithDynamicFilters(entityName, filterList, binding, joinCondition);
                        if (newFilterExpression != null)
                        {
                            //  Converts the collection results into a single row.  The expected output is a single item so EF will
                            //  then populate the results of that query into the property in the model.
                            //  The resulting SQL will be a normal "left outer join" just as it would normally be except that our
                            //  filter predicate conditions will be included with the normal join conditions.
                            return newFilterExpression.Element();
                        }
                    }
                }
            }

            return baseResult;
        }
#endif

        private IEnumerable<DynamicFilterDefinition> FindFiltersForEntitySet(ReadOnlyMetadataCollection<MetadataProperty> metadataProperties, EntityContainer entityContainer)
        {
#if USE_CSPACE
            var configuration = metadataProperties.FirstOrDefault(p => p.Name == "Configuration")?.Value;
            if (configuration == null)
                return new List<DynamicFilterDefinition>();

            var annotations = configuration.GetType().GetProperty("Annotations").GetValue(configuration, null) as Dictionary<string, object>;
            if (annotations == null)
                return new List<DynamicFilterDefinition>();

            var filterList = annotations.Select(a => a.Value as DynamicFilterDefinition).Where(a => a != null).ToList();
#else
            var filterList = metadataProperties
                .Where(mp => mp.Name.Contains("customannotation:" + DynamicFilterConstants.ATTRIBUTE_NAME_PREFIX))
                .Select(m => m.Value as DynamicFilterDefinition)
                .ToList();
#endif

            //  Note: Prior to the switch to use CSpace (which was done to allow filters on navigation properties),
            //  we had to remove filters that exist in base EntitySets to this entity to fix issues with 
            //  Table-per-Type inheritance (issue #32).  In CSpace none of that is necessary since we are working
            //  with the actual c# models now (in CSpace) so we always have the correct filters and access to all
            //  the inherited properties that we need.
#if !USE_CSPACE
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
#endif

            return filterList;
        }

#if !USE_CSPACE
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
#endif

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
#if USE_CSPACE
                    var edmProp = edmType.Properties.FirstOrDefault(p => p.Name == filter.ColumnName);
#else
                    //  Need to map through the EdmType properties to find the actual database/cspace name for the entity property.
                    //  It may be different from the entity property!
                    var edmProp = edmType.Properties.Where(p => p.MetadataProperties.Any(m => m.Name == "PreferredName" && m.Value.Equals(filter.ColumnName))).FirstOrDefault();
#endif
                    if (edmProp == null)
                        continue;       //  ???
                    //  database column name is now in edmProp.Name.  Use that instead of filter.ColumnName

                    var columnProperty = DbExpressionBuilder.Property(DbExpressionBuilder.Variable(binding.VariableType, binding.VariableName), edmProp.Name);
                    var param = columnProperty.Property.TypeUsage.Parameter(filter.CreateDynamicFilterName(filter.ColumnName));

#if USE_CSPACE
                    dbExpression = DbExpressionBuilder.Equal(columnProperty, param);
#else
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
#endif
                }
                else if (filter.Predicate != null)
                {
                    //  Lambda expression filter
                    dbExpression = LambdaToDbExpressionVisitor.Convert(filter, binding, _ObjectContext);
                }
                else
                    throw new System.ArgumentException(string.Format("Filter {0} does not contain a ColumnName or a Predicate!", filter.FilterName));

                if (DynamicFilterExtensions.AreFilterDisabledConditionsAllowed(filter.FilterName))
                {
                    //  Create an expression to check to see if the filter has been disabled and include that check with the rest of the filter expression.
                    //  When this parameter is null, the filter is enabled.  It will be set to true (in DynamicFilterExtensions.GetFilterParameterValue) if
                    //  the filter has been disabled.
                    var boolPrimitiveType = LambdaToDbExpressionVisitor.TypeUsageForPrimitiveType(typeof(bool?), _ObjectContext);
                    var isDisabledParam = boolPrimitiveType.Parameter(filter.CreateFilterDisabledParameterName());

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
