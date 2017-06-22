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
    public class DynamicFilterQueryVisitorCSpace : DefaultExpressionVisitor
    {
        private readonly DbContext _DbContext;
        private readonly ObjectContext _ObjectContext;

        /// <summary>
        /// Returns true if the database does not support the DbExpression.Element() method - at least in the
        /// context that we try to use it when applying a filter to a child entity.
        /// Requires us to do an extra step of processing to apply those filters using SSpace.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool DoesNotSupportElementMethod(DbContext context)
        {
            //  Oracle 11g (and prior?) do not support this (see https://community.oracle.com/message/10168766#10168766).
            //  But as of Oracle 12, it does.

            return context.OracleVersion()?.Major < 12;     //  If this is true, we are connected to Oracle 11 (or older)
        }

        public DynamicFilterQueryVisitorCSpace(DbContext contextForInterception)
        {
            _DbContext = contextForInterception;
            _ObjectContext = ((IObjectContextAdapter)contextForInterception).ObjectContext;
        }

        public override DbExpression Visit(DbOfTypeExpression expression)
        {
#if DEBUG_VISITS
            System.Diagnostics.Debug.Print("Visit(DbOfTypeExpression): OfType={0}", expression.OfType.EdmType.Name);
#endif

            //  This visitor is called for TPH & TPT entities that need to map from base entity type to the
            //  actual type.  Argument contains the base type expression (i.e. User) and OfType contains the
            //  derived type (i.e. Student).
            //  We need to check for any filters that are defined only on the derived type and apply those filters
            //  here - they will not be detected during the DbScanExpression visitor because those filters will not
            //  exist on the base type.
            //  Filters defined on the base type will be found on both entities.  So we only want to apply the filter
            //  that are defined on the OfType entity that are not defined on the Argument entity (as those will be
            //  applied during the DbScanExpression visitor).
            IEnumerable<DynamicFilterDefinition> scanFilterList = null;
            var scanExp = expression.Argument as DbScanExpression;
            if (scanExp != null)
                scanFilterList = FindFiltersForEntitySet(scanExp.Target.ElementType.MetadataProperties);
            var ofTypeFilterList = FindFiltersForEntitySet(expression.OfType.EdmType.MetadataProperties);
            var filtersToApply = ofTypeFilterList.Where(f => (scanFilterList == null) || !scanFilterList.Any(f2 => f2.ID == f.ID));

            var baseResult = base.Visit(expression);

            if (filtersToApply.Any())
            {
                var binding = DbExpressionBuilder.Bind(baseResult);
                var newFilterExpression = BuildFilterExpressionWithDynamicFilters(filtersToApply, binding, null);
                if (newFilterExpression != null)
                {
                    //  If not null, a new DbFilterExpression has been created with our dynamic filters.
                    return newFilterExpression;
                }
            }

            return baseResult;
        }

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

            var filterList = FindFiltersForEntitySet(expression.Target.ElementType.MetadataProperties);
            System.Diagnostics.Debug.Print("  Found {0} filters", filterList.Count());

            var baseResult = base.Visit(expression);
            if (filterList.Any())
            {
                var binding = DbExpressionBuilder.Bind(baseResult);
                var newFilterExpression = BuildFilterExpressionWithDynamicFilters(filterList, binding, null);
                if (newFilterExpression != null)
                {
                    //  If not null, a new DbFilterExpression has been created with our dynamic filters.
                    return newFilterExpression;
                }
            }

            return baseResult;
        }

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

                var containers = _ObjectContext.MetadataWorkspace.GetItems<EntityContainer>(DataSpace.CSpace).First();
                var filterList = FindFiltersForEntitySet(targetEntityType.MetadataProperties);

                if (filterList.Any())
                {
                    //  If the expression contains a collection (i.e. the child property is an IEnumerable), we can bind directly to it.
                    //  Otherwise, we have to create a DbScanExpression over the ResultType in order to bind.
                    if (baseResult.ResultType.EdmType.BuiltInTypeKind == BuiltInTypeKind.CollectionType)
                    {
                        var binding = DbExpressionBuilder.Bind(baseResult);
                        var newFilterExpression = BuildFilterExpressionWithDynamicFilters(filterList, binding, null);
                        if (newFilterExpression != null)
                        {
                            //  If not null, a new DbFilterExpression has been created with our dynamic filters.
                            return newFilterExpression;
                        }
                    }
                    else if (baseResult.ResultType.EdmType.BuiltInTypeKind == BuiltInTypeKind.EntityType)
                    {
                        if (DoesNotSupportElementMethod(_DbContext))
                        {
                            //  Oracle and MySQL do not support the "newFilterExpression.Element()" method that we need to call 
                            //  at the end of this block.  Oracle *MAY* support it in a newer release but not sure
                            //  (see https://community.oracle.com/message/10168766#10168766).
                            //  But users may not have the option of upgrading their database so decided to try to support it.
                            //  If we find it is supported by newer versions, can detect those versions and allow the normal handling.
                            //  To apply any necessary filters to these entities, we're going to have to do it using SSpace.
                            //  These entities will be visited via the DbScan visit method so we will apply filters there.
                            //  If one of those filters then references a child property, the filter will fail.
                            return baseResult;
                        }

                        DbExpression scanExpr;

                        var entitySet = containers.EntitySets.FirstOrDefault(e => e.ElementType.Name == baseResult.ResultType.EdmType.Name);
                        if (entitySet == null)
                        {
                            if (baseResult.ResultType.EdmType.BaseType == null)
                                throw new ApplicationException(string.Format("EntitySet not found for {0}", baseResult.ResultType.EdmType.Name));

                            //  Did not find the entity set for the property but it has a base type.
                            //  This means the entity set of the property is a derived class of a TPT base class.
                            //  Find the entity set of the parent and then map it to the type we need.
                            //  Then we can bind against that expression.
                            var currentBaseType = baseResult.ResultType.EdmType.BaseType;
                            while (entitySet == null && currentBaseType != null)
                            {
                                entitySet = containers.EntitySets.FirstOrDefault(e => e.ElementType.Name == currentBaseType.Name);

                                if (entitySet == null)
                                {
                                    currentBaseType = currentBaseType.BaseType;
                                }
                            }

                            if (entitySet == null)        //  hope we don't need to do this recursively...
                                throw new ApplicationException(string.Format("EntitySet not found for {0} or BaseType {1}", baseResult.ResultType.EdmType.Name, baseResult.ResultType.EdmType.BaseType.Name));

                            var parentScanExpr = DbExpressionBuilder.Scan(entitySet);
                            scanExpr = DbExpressionBuilder.OfType(parentScanExpr, baseResult.ResultType);
                        }
                        else
                            scanExpr = DbExpressionBuilder.Scan(entitySet);

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
                        //  Note that this navigation property may be the same type as parent entity (so both association types
                        //  will be the same type).  In that case, we need to figure out which side of the association matches the
                        //  PKs of the main entity.
                        var fromEdmType = ((AssociationEndMember)associationType.Constraint.FromRole).GetEntityType();
                        var toEdmType = ((AssociationEndMember)associationType.Constraint.ToRole).GetEntityType();
                        bool baseResultIsFromRole;
                        if (fromEdmType != toEdmType)
                            baseResultIsFromRole = (basePropertyResult.Instance.ResultType.EdmType == fromEdmType);
                        else
                        {
                            //  When same, basePropertyResult is the child property and binding is the main entity.
                            //  Fixes issue #85.
                            baseResultIsFromRole = false;
                        }

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
                        var newFilterExpression = BuildFilterExpressionWithDynamicFilters(filterList, binding, joinCondition);
                        if (newFilterExpression != null)
                        {
                            //  Converts the collection results into a single row.  The expected output is a single item so EF will
                            //  then populate the results of that query into the property in the model.
                            //  The resulting SQL will be a normal "left outer join" just as it would normally be except that our
                            //  filter predicate conditions will be included with the normal join conditions.

                            //  MySQL needs this Limit() applied here or it throws an error saying:
                            //  Unable to cast object of type 'MySql.Data.Entity.SelectStatement' to type 'MySql.Data.Entity.LiteralFragment'.
                            //  But don't do that unless necessary because it produces extra "outer apply" sub queries in MS SQL.
                            //  This trick does not work for Oracle...
                            if (_DbContext.IsMySql())
                                return newFilterExpression.Limit(DbConstantExpression.FromInt32(1)).Element();

                            return newFilterExpression.Element();
                        }
                    }
                }
            }

            return baseResult;
        }

        private IEnumerable<DynamicFilterDefinition> FindFiltersForEntitySet(ReadOnlyMetadataCollection<MetadataProperty> metadataProperties)
        {
            var configuration = metadataProperties.FirstOrDefault(p => p.Name == "Configuration")?.Value;
            if (configuration == null)
                return new List<DynamicFilterDefinition>();

            //  The "Annotations" property will not exist if this is a navigation property (because configuration
            //  is a NavigationPropertyConfiguration object not an EntityTypeConfiguration object.
            //  That happens if we use the entry.Load() command to load a child collection.  See issue #71.
            var annotations = configuration.GetType().GetProperty("Annotations")?.GetValue(configuration, null) as Dictionary<string, object>;
            if (annotations == null)
                return new List<DynamicFilterDefinition>();

            var filterList = annotations.Select(a => a.Value as DynamicFilterDefinition).Where(a => a != null).ToList();

            //  Note: Prior to the switch to use CSpace (which was done to allow filters on navigation properties),
            //  we had to remove filters that exist in base EntitySets to this entity to fix issues with 
            //  Table-per-Type inheritance (issue #32).  In CSpace none of that is necessary since we are working
            //  with the actual c# models now (in CSpace) so we always have the correct filters and access to all
            //  the inherited properties that we need.

            return filterList;
        }

        private DbFilterExpression BuildFilterExpressionWithDynamicFilters(IEnumerable<DynamicFilterDefinition> filterList, DbExpressionBinding binding, DbExpression predicate)
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
                    var edmProp = edmType.Properties.FirstOrDefault(p => p.Name == filter.ColumnName);
                    if (edmProp == null)
                        continue;       //  ???
                    //  database column name is now in edmProp.Name.  Use that instead of filter.ColumnName

                    var columnProperty = DbExpressionBuilder.Property(DbExpressionBuilder.Variable(binding.VariableType, binding.VariableName), edmProp.Name);
                    var param = columnProperty.Property.TypeUsage.Parameter(filter.CreateDynamicFilterName(filter.ColumnName, DataSpace.CSpace));

                    dbExpression = DbExpressionBuilder.Equal(columnProperty, param);

                    //  When using SSpace, need some special handling for an Oracle Boolean property.
                    //  Not necessary when using CSpace since the translation into the Oracle types has not happened yet.
                }
                else if (filter.Predicate != null)
                {
                    //  Lambda expression filter
                    dbExpression = LambdaToDbExpressionVisitor.Convert(filter, binding, _DbContext, DataSpace.CSpace);
                }
                else
                    throw new System.ArgumentException(string.Format("Filter {0} does not contain a ColumnName or a Predicate!", filter.FilterName));

                if (DynamicFilterExtensions.AreFilterDisabledConditionsAllowed(filter.FilterName))
                {
                    //  Create an expression to check to see if the filter has been disabled and include that check with the rest of the filter expression.
                    //  When this parameter is null, the filter is enabled.  It will be set to true (in DynamicFilterExtensions.GetFilterParameterValue) if
                    //  the filter has been disabled.
                    var boolPrimitiveType = LambdaToDbExpressionVisitor.TypeUsageForPrimitiveType(typeof(bool?), _ObjectContext, DataSpace.CSpace);
                    var isDisabledParam = boolPrimitiveType.Parameter(filter.CreateFilterDisabledParameterName(DataSpace.CSpace));

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

        public override DbExpression Visit(DbFilterExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbFilterExpression): {0}", expression);
            return base.Visit(expression);
        }

        public override DbExpression Visit(DbCastExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbCastExpression): {0}", expression);
            return base.Visit(expression);
        }

        public override DbExpression Visit(DbLimitExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbLimitExpression): {0}", expression);
            return base.Visit(expression);
        }

        public override DbExpression Visit(DbIsOfExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbIsOfExpression): {0}", expression);
            return base.Visit(expression);
        }

        public override DbExpression Visit(DbQuantifierExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbQuantifierExpression): {0}", expression);
            return base.Visit(expression);
        }

        public override DbExpression Visit(DbTreatExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbTreatExpression): {0}", expression);
            return base.Visit(expression);
        }

        public override DbExpression Visit(DbUnionAllExpression expression)
        {
            System.Diagnostics.Debug.Print("Visit(DbUnionAllExpression): {0}", expression);
            return base.Visit(expression);
        }
#endif

#endregion
    }
}
