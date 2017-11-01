#if DEBUG
//#define DEBUG_VISITS
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EntityFramework.DynamicFilters
{
    internal class LambdaToDbExpressionVisitor : ExpressionVisitor
    {
        #region Privates

        private DynamicFilterDefinition _Filter;
        private DbExpressionBinding _Binding;
        private DbContext _DbContext;
        private ObjectContext _ObjectContext;
        private DataSpace _DataSpace;

        private Dictionary<Expression, DbExpression> _ExpressionToDbExpressionMap = new Dictionary<Expression, DbExpression>();
        private Dictionary<string, DbPropertyExpression> _Properties = new Dictionary<string, DbPropertyExpression>();
        private Dictionary<string, DbParameterReferenceExpression> _Parameters = new Dictionary<string, DbParameterReferenceExpression>();

        #endregion

        #region Static methods & private Constructor

        public static DbExpression Convert(DynamicFilterDefinition filter, DbExpressionBinding binding, DbContext dbContext, DataSpace dataSpace)
        {
            var visitor = new LambdaToDbExpressionVisitor(filter, binding, dbContext, dataSpace);
            var expression = visitor.Visit(filter.Predicate) as LambdaExpression;

            var dbExpression = visitor.GetDbExpressionForExpression(expression.Body);

            if (dbExpression is DbPropertyExpression)
            {
                //  Special case to handle a condition that is just a plain "boolFlag" or a nullable generic condition.
                //  For a nullable type, we only get here when the filter has either not specified a value for the nullable
                //  parameter or it has specified "null" - both evaluate the same as far as the method prototypes can tell
                //  since the method signature is "param = null".  This needs to generate a sql "is null" condition.
                //  Otherwise, no param value was specified so we are assuming that we need to generate a "positive"
                //  condition.  i.e. the filter just said "b.prop" which generally means "b.prop == true".
                //  To generate that condition correctly for all types (may not necessarily be a bool), we create a condition
                //  like "!(b.prop == [defaultValue])"

                if (IsNullableType(expression.Body.Type))
                {
                    dbExpression = DbExpressionBuilder.IsNull(dbExpression);
                }
                else
                {
                    var defaultValue = DbExpressionBuilder.Constant(dbExpression.ResultType, Activator.CreateInstance(expression.Body.Type));
                    dbExpression = DbExpressionBuilder.Not(DbExpressionBuilder.Equal(dbExpression, defaultValue));
                }
            }

            return dbExpression;
        }

        private LambdaToDbExpressionVisitor(DynamicFilterDefinition filter, DbExpressionBinding binding, DbContext dbContext, DataSpace dataSpace)
        {
            _Filter = filter;
            _Binding = binding;
            _DbContext = dbContext;
            _ObjectContext = ((IObjectContextAdapter) dbContext).ObjectContext;
            _DataSpace = dataSpace;
        }

        #endregion

        #region ExpressionVisitor Overrides

        protected override Expression VisitBinary(BinaryExpression node)
        {
#if (DEBUG_VISITS)
            System.Diagnostics.Debug.Print("VisitBinary: {0}", node);
#endif

            var expression = base.VisitBinary(node) as BinaryExpression;

            DbExpression dbExpression;

            //  Need special handling for comparisons against the null constant.  If we don't translate these
            //  using an "IsNull" expression, EF will convert it literally as "= null" which doesn't work in SQL Server.
            if (IsNullConstantExpression(expression.Right))
                dbExpression = MapNullComparison(expression.Left, expression.NodeType);
            else if (IsNullConstantExpression(expression.Left))
                dbExpression = MapNullComparison(expression.Right, expression.NodeType);
            else
            {
                DbExpression leftExpression = GetDbExpressionForExpression(expression.Left);
                DbExpression rightExpression = GetDbExpressionForExpression(expression.Right);

                switch (expression.NodeType)
                {
                    case ExpressionType.Equal:
                        //  DbPropertyExpression = class property that has been mapped to a database column
                        //  DbParameterReferenceExpression = lambda parameter
                        if (IsNullableExpressionOfType<DbPropertyExpression>(leftExpression) && IsNullableExpressionOfType<DbParameterReferenceExpression>(rightExpression))
                            dbExpression = CreateEqualComparisonOfNullablePropToNullableParam(leftExpression, rightExpression);
                        else if (IsNullableExpressionOfType<DbPropertyExpression>(rightExpression) && IsNullableExpressionOfType<DbParameterReferenceExpression>(leftExpression))
                            dbExpression = CreateEqualComparisonOfNullablePropToNullableParam(rightExpression, leftExpression);
                        else
                            dbExpression = DbExpressionBuilder.Equal(leftExpression, rightExpression);
                        break;
                    case ExpressionType.NotEqual:
                        dbExpression = DbExpressionBuilder.NotEqual(leftExpression, rightExpression);
                        break;
                    case ExpressionType.GreaterThan:
                        dbExpression = DbExpressionBuilder.GreaterThan(leftExpression, rightExpression);
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        dbExpression = DbExpressionBuilder.GreaterThanOrEqual(leftExpression, rightExpression);
                        break;
                    case ExpressionType.LessThan:
                        dbExpression = DbExpressionBuilder.LessThan(leftExpression, rightExpression);
                        break;
                    case ExpressionType.LessThanOrEqual:
                        dbExpression = DbExpressionBuilder.LessThanOrEqual(leftExpression, rightExpression);
                        break;

                    case ExpressionType.AndAlso:
                        dbExpression = DbExpressionBuilder.And(leftExpression, rightExpression);
                        break;
                    case ExpressionType.OrElse:
                        dbExpression = DbExpressionBuilder.Or(leftExpression, rightExpression);
                        break;

                    case ExpressionType.And:
                        dbExpression = EdmFunctions.BitwiseAnd(leftExpression, rightExpression);
                        break;
                    case ExpressionType.Or:
                        dbExpression = EdmFunctions.BitwiseOr(leftExpression, rightExpression);
                        break;
                    case ExpressionType.ExclusiveOr:
                        dbExpression = EdmFunctions.BitwiseXor(leftExpression, rightExpression);
                        break;

                    case ExpressionType.Coalesce:
                        //  EF does not expose the "coalesce" function.  So best we can do is a case statement.  Issue #77.
                        var whenExpressions = new List<DbExpression>() {DbExpressionBuilder.IsNull(leftExpression)};
                        var thenExpressions = new List<DbExpression>() {rightExpression};
                        dbExpression = DbExpressionBuilder.Case(whenExpressions, thenExpressions, leftExpression);
                        break;

                    default:
                        throw new NotImplementedException(string.Format("Unhandled NodeType of {0} in LambdaToDbExpressionVisitor.VisitBinary", expression.NodeType));
                }
            }

            MapExpressionToDbExpression(expression, dbExpression);

            return expression;
        }

        /// <summary>
        /// Creates an Equal comparison of a nullable property (db column) to a nullable parameter (lambda param)
        /// that adds the necessary "is null" checks to support a filter like "e.TenantId = tenantId".
        /// Results in sql: (e.TenantID is null and @tenantID is null) or (e.TenantID is not null and e.TenantID = @tenantID)
        /// which will support parmeter values that are "null" or a specific value and will correctly filter on columns that
        /// are "null" or a specific value.
        /// </summary>
        /// <param name="propExpression"></param>
        /// <param name="paramExpression"></param>
        /// <returns></returns>
        private DbExpression CreateEqualComparisonOfNullablePropToNullableParam(DbExpression propExpression, DbExpression paramExpression)
        {
            var condition1 = propExpression.IsNull().And(paramExpression.IsNull());
            var condition2 = propExpression.IsNull().Not().And(propExpression.Equal(paramExpression));
            return condition1.Or(condition2);
        }

        /// <summary>
        /// Maps a comparison of an expression to a "null" constant.
        /// </summary>
        /// <returns></returns>
        private DbExpression MapNullComparison(Expression expression, ExpressionType comparisonType)
        {
            DbExpression dbExpression = DbExpressionBuilder.IsNull(GetDbExpressionForExpression(expression));

            switch (comparisonType)
            {
                case ExpressionType.Equal:
                    return dbExpression;
                case ExpressionType.NotEqual:
                    //  Creates expression: !([expression] is null)
                    return DbExpressionBuilder.Not(dbExpression);
            }

            throw new NotImplementedException(string.Format("Unhandled comparisonType of {0} in LambdaToDbExpressionVisitor.MapNullComparison", comparisonType));
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
#if (DEBUG_VISITS)
            System.Diagnostics.Debug.Print("VisitConstant: {0}", node);
#endif

            var expression = base.VisitConstant(node);

            var type = node.Type;
            if (IsNullableType(type))
            {
                var genericArgs = type.GetGenericArguments();
                if ((genericArgs != null) && (genericArgs.Length == 1))
                    type = genericArgs[0];
            }

            if (type == typeof(byte[]))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromBinary((byte[]) node.Value));
            else if (type == typeof(bool))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromBoolean((bool?) node.Value));
            else if (type == typeof(byte))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromByte((byte?) node.Value));
            else if (type == typeof(DateTime))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromDateTime((DateTime?) node.Value));
            else if (type == typeof(DateTimeOffset))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromDateTimeOffset((DateTimeOffset?) node.Value));
            else if (type == typeof(decimal))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromDecimal((decimal?) node.Value));
            else if (type == typeof(double))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromDouble((double?) node.Value));
            else if (type == typeof(Guid))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromGuid((Guid?) node.Value));
            else if (type == typeof(Int16))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromInt16((Int16?) node.Value));
            else if (type == typeof(Int32))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromInt32((Int32?) node.Value));
            else if (type.IsEnum)
            {
                if (_DataSpace == DataSpace.CSpace)
                {
                    var typeUsage = TypeUsageForPrimitiveType(node.Type);
                    MapExpressionToDbExpression(expression, DbExpressionBuilder.Constant(typeUsage, node.Value));
                }
                else
                    MapExpressionToDbExpression(expression, DbConstantExpression.FromInt32((Int32) node.Value));
            }
            else if (type == typeof(Int64))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromInt64((Int64?) node.Value));
            else if (type == typeof(float))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromSingle((float?) node.Value));
            else if (type == typeof(string))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromString((string) node.Value));
            else
                throw new NotImplementedException(string.Format("Unhandled Type of {0} for Constant value {1} in LambdaToDbExpressionVisitor.VisitConstant", node.Type.Name, node.Value ?? "null"));

            return expression;
        }

        /// <summary>
        /// Visit a Member Expression.  Creates a mapping of the MemberExpression to a DbPropertyExpression
        /// which is a reference to the table/column name that matches the MemberExpression.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
#if (DEBUG_VISITS)
            System.Diagnostics.Debug.Print("VisitMember: {0}, expression.NodeType={1}, Member={2}", node, node.Expression.NodeType, node.Member);
#endif

            //  This handles fields/properties that are embedded directly inside the linq statement.
            //  These values are evaluated as constants and because we can only do this when the query is compiled
            //  (not each time it is executed), it will always have the same value.
            //  If the value should change or be re-evaluated each time the query is executed, it should
            //  be made a parameter of the filter!
            //  See https://github.com/jcachat/EntityFramework.DynamicFilters/issues/109
            if ((node.Expression is ConstantExpression) || //  class field/property
                ((node.Expression == null) && (node.NodeType == ExpressionType.MemberAccess))) //  static field/property
            {
                //  Class fields & properties must reference the container (the class instance that contains the field/property)
                object container = (node.Expression != null) ? ((ConstantExpression) node.Expression).Value : null;

                if (node.Member is FieldInfo) //  regular field property (not a get accessor)
                {
                    var value = ((FieldInfo) node.Member).GetValue(container);
                    return VisitConstant(Expression.Constant(value));
                }
                else if (node.Member is PropertyInfo) //  get accessor
                {
                    object value = ((PropertyInfo) node.Member).GetValue(container, null);
                    return VisitConstant(Expression.Constant(value));
                }
                else
                    throw new NotImplementedException();
            }

            var expression = base.VisitMember(node) as MemberExpression;

            if ((expression.Expression.NodeType == ExpressionType.Parameter) && (expression.Expression.Type.IsClass || expression.Expression.Type.IsInterface))
            {
                //  expression is a reference to a class/interface property.  Need to map it to a sql parameter or look up
                //  the existing parameter.
                //  The class/interface is defined by expression.Expression while the property is in expression.Member.
                string propertyName;
                if (IsNullableType(expression.Member.ReflectedType))
                {
                    var subExpression = expression.Expression as MemberExpression;
                    propertyName = subExpression.Member.Name;
                }
                else
                    propertyName = expression.Member.Name;

                //  TODO: To support different class/interfaces, can we figure out the correct binding from what's in expression.Expression?
                var edmType = _Binding.VariableType.EdmType as EntityType;

                DbPropertyExpression propertyExpression;
                if (!_Properties.TryGetValue(propertyName, out propertyExpression))
                {
                    //  Not created yet

                    //  Need to map through the EdmType properties to find the actual database/cspace name for the entity property.
                    //  It may be different from the entity property!
                    EdmMember edmProp;
                    if (_DataSpace == DataSpace.CSpace)
                        edmProp = edmType.Members.FirstOrDefault(m => m.Name == propertyName);
                    else
                        edmProp = edmType.Properties.Where(p => p.MetadataProperties.Any(m => m.Name == "PreferredName" && m.Value.Equals(propertyName))).FirstOrDefault();

                    if (edmProp == null)
                    {
                        //  If using SSpace: Accessing properties outside the main entity is not supported and will cause this exception.
                        //  If using CSpace: Navigation properties are handled (and will be found above)
                        throw new ApplicationException(string.Format("Property {0} not found in Entity Type {1}", propertyName, expression.Expression.Type.Name));
                    }

                    //  If edmProp is a navigation property (only available when using CSpace), EF will automatically add the join to it when it sees we are referencing this property.
                    propertyExpression = DbExpressionBuilder.Property(DbExpressionBuilder.Variable(_Binding.VariableType, _Binding.VariableName), edmProp.Name);
                    _Properties.Add(propertyName, propertyExpression);

#if (DEBUG_VISITS)
                    System.Diagnostics.Debug.Print("Created new property expression for {0}", propertyName);
#endif
                }

                //  Nothing else to do here
                MapExpressionToDbExpression(expression, propertyExpression);
                return expression;
            }

            //  We are accessing a member property such that expression.Expression is the object and expression.Member is the property.
            //  And the property is one that requires special handling.  Regular class properties are all handled up above.
            var objectExpression = GetDbExpressionForExpression(expression.Expression);

            DbExpression dbExpression;
            var isNullableType = IsNullableType(expression.Expression.Type);
            int? dummy;
            if (isNullableType && (expression.Member.Name == nameof(dummy.HasValue)))
            {
                //  Map HasValue to !IsNull
                dbExpression = DbExpressionBuilder.Not(DbExpressionBuilder.IsNull(objectExpression));
            }
            else if (isNullableType && (expression.Member.Name == nameof(dummy.Value)))
            {
                //  This is a nullable Value accessor so just map to the object itself and it will be mapped for us
                dbExpression = objectExpression;
            }
            else
            {
                if (_DataSpace == DataSpace.CSpace)
                {
                    //  When using CSpace, we can map a property to the class member and EF will figure out the relationship for us.
                    dbExpression = DbExpressionBuilder.Property(objectExpression, expression.Member.Name);
                }
                else
                {
                    //  When using SSpace, we cannot access class members
                    throw new ApplicationException(string.Format("Unhandled property accessor in expression: {0}", expression));
                }
            }

            MapExpressionToDbExpression(expression, dbExpression);
            return expression;
        }

        /// <summary>
        /// Visit a Parameter Expression.  Creates a mapping from the ParameterExpression to a
        /// DbParameterReferenceExpression which is a reference to a SQL Parameter bound to the
        /// table being queries (_Binding).
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
#if (DEBUG_VISITS)
            System.Diagnostics.Debug.Print("VisitParameter: {0}", node);
#endif

            var expression = base.VisitParameter(node);

            if (node.Type.IsClass || node.Type.IsInterface)
                return expression; //  Ignore class or interface param

            if (_Parameters.ContainsKey(node.Name))
                return expression; //  Already created sql parameter for this node.Name

            //  Create a new DbParameterReferenceExpression for this parameter.
            var param = CreateParameter(node.Name, node.Type);

            MapExpressionToDbExpression(expression, param);
            return expression;
        }

        private DbParameterReferenceExpression CreateParameter(string name, Type type)
        {
            DbParameterReferenceExpression param;
            if (_Parameters.TryGetValue(name, out param))
                return param;

            var typeUsage = TypeUsageForPrimitiveType(type);
            string dynFilterParamName = _Filter.CreateDynamicFilterName(name, _DataSpace);
            param = typeUsage.Parameter(dynFilterParamName);

#if (DEBUG_VISITS)
            System.Diagnostics.Debug.Print("Created new parameter for {0}: {1}", name, dynFilterParamName);
#endif

            _Parameters.Add(name, param);

            return param;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
#if (DEBUG_VISITS)
            System.Diagnostics.Debug.Print("VisitUnary: {0}", node);
#endif

            var expression = base.VisitUnary(node) as UnaryExpression;

            DbExpression operandExpression = GetDbExpressionForExpression(expression.Operand);

            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    if (operandExpression is DbPropertyExpression)
                    {
                        //  Special case to handle "!boolFlag": operandExpression is the property for "boolFlag".
                        //  In order for the sql to generate correct, we need to turn this into "boolFlag = 0"
                        //  (or we could translate it literally as "not (@noolFlag = 1)")
                        //  Figuring out the defaultValue like this should produce the default (0, false, empty) value for the type
                        //  we are working with.  So checking for "@var = defaultValue" should produce the correct condition.
                        var defaultValue = DbExpressionBuilder.Constant(operandExpression.ResultType, Activator.CreateInstance(expression.Operand.Type));
                        MapExpressionToDbExpression(expression, DbExpressionBuilder.Equal(operandExpression, defaultValue));
                    }
                    else
                        MapExpressionToDbExpression(expression, DbExpressionBuilder.Not(operandExpression));
                    break;
                case ExpressionType.Convert:
                    MapExpressionToDbExpression(expression, DbExpressionBuilder.CastTo(operandExpression, TypeUsageForPrimitiveType(expression.Type)));
                    break;
                default:
                    throw new NotImplementedException(string.Format("Unhandled NodeType of {0} in LambdaToDbExpressionVisitor.VisitUnary", expression.NodeType));
            }

            return expression;
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
#if (DEBUG_VISITS)
            System.Diagnostics.Debug.Print("VisitConditional: {0}", node);
#endif

            throw new NotImplementedException("Conditionals in Lambda expressions are not supported");
            //return base.VisitConditional(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
#if (DEBUG_VISITS)
            System.Diagnostics.Debug.Print("VisitMethodCall: {0}", node);
#endif
            //  Do not call base.VisitMethodCall(node) here because of the method that is being
            //  called has a lambdas as an argument, we need to handle it differently.  If we call the base,
            //  the visits that we do will be against the current binding - not the source of the method call.
            //  So these "Map" methods are all responsible for calling the base if necessary.
            Expression expression;
            switch (node.Method.Name)
            {
                case "Contains":
                    if (node.Method.DeclaringType == typeof(string))
                        expression = MapStringLikeExpression(node, false, false);
                    else
                        expression = MapEnumerableContainsExpression(node);
                    break;
                case "StartsWith":
                    expression = MapStringLikeExpression(node, true, false);
                    break;
                case "EndsWith":
                    expression = MapStringLikeExpression(node, false, true);
                    break;
                case "Any":
                case "All":
                    expression = MapAnyOrAllExpression(node);
                    break;
                case "ToLower":
                    expression = MapSimpleExpression(node, EdmFunctions.ToLower);
                    break;
                case "ToUpper":
                    expression = MapSimpleExpression(node, EdmFunctions.ToUpper);
                    break;
                default:
                    //  Anything else is invoked and handled as a constant.  This allows us to handle user-defined methods.
                    //  If this evaluates to something that is not a constant, it will throw an exception...which is what we used to do anyway.
                    //  Because we can only do this when the query is compiled (not each time it is executed), it will always have the 
                    //  same value.  If the value should change or be re-evaluated each time the query is executed, it should
                    //  be made a parameter of the filter!
                    //  See https://github.com/jcachat/EntityFramework.DynamicFilters/issues/109
                    var func = Expression.Lambda(node).Compile();
                    var value = func.DynamicInvoke();
                    return VisitConstant(Expression.Constant(value));
            }

            return expression;
        }

        private Expression MapEnumerableContainsExpression(MethodCallExpression node)
        {
            var expression = base.VisitMethodCall(node) as MethodCallExpression;

            //  For some reason, if the list is IEnumerable and not the List class, the 
            //  list object (the ParameterExpression object) will be in Argument[0] and the param
            //  of the Contains() function will be in Argument[1].  And expression.object is null.
            //  In all other cases, the list object is in expression.Object and the Contains() param is Arguments[0]!

            DbExpression argExpression = null;
            ParameterExpression collectionObjExp = null;

            if ((expression.Arguments.Count > 1) && (expression.Object == null))
                collectionObjExp = expression.Arguments[0] as ParameterExpression;
            if (collectionObjExp != null)
                argExpression = GetDbExpressionForExpression(expression.Arguments[1]); //  IEnumerable
            else
            {
                argExpression = GetDbExpressionForExpression(expression.Arguments[0]); //  List, IList, ICollection
                collectionObjExp = expression.Object as ParameterExpression;
            }

            DbExpression dbExpression;

            if (collectionObjExp != null)
            {
                //  collectionObjExp is a parameter expression.  This means the content of the collection is
                //  dynamic.  DbInExpression only supports a fixed size list of constant values.
                //  So the only way to handle a dynamic collection is for us to create a single Equals expression
                //  with a DbParameterReference.  Then when we intercept that parameter, we will see that it's
                //  for a collection and we will modify the SQL to change it from an "=" to an "in".  The single
                //  Parameter Reference is set to the first value in the collection and the rest of the values
                //  are inserted into the SQL "in" clause.
                string paramName = collectionObjExp.Name;
                Type paramType = PrimitiveTypeForType(collectionObjExp.Type, _DataSpace);

                var param = CreateParameter(paramName, paramType);
                dbExpression = DbExpressionBuilder.Equal(argExpression, param);
            }
            else
            {
                var listExpression = expression.Object as ListInitExpression;
                if (listExpression == null)
                    throw new NotSupportedException(string.Format("Unsupported object type used in Contains() - type = {0}", expression.Object?.GetType().Name ?? "null"));

                //  This is a fixed size list that may contain parameter references or constant values.
                //  This can be handled using either a DbInExpression (if all are constants) or with
                //  a series of OR conditions.
                //  Find all of the constant & parameter expressions.
                var constantExpressionList = listExpression.Initializers
                    .Select(i => i.Arguments.FirstOrDefault() as ConstantExpression)
                    .Where(c => (c != null) && (c.Value != null)) //  null not supported - can only use DbConstant in "In" expression
                    .Select(c => CreateConstantExpression(c.Value))
                    .ToList();
                constantExpressionList.AddRange(listExpression.Initializers
                    .Select(i => i.Arguments.FirstOrDefault() as UnaryExpression)
                    .Where(c => (c != null) && (c.Operand is ConstantExpression))
                    .Select(c => CreateConstantExpression(((ConstantExpression) c.Operand).Value)));
                var parameterExpressionList = listExpression.Initializers
                    .Select(i => i.Arguments.FirstOrDefault() as ParameterExpression)
                    .Where(c => c != null)
                    .Select(c => CreateParameter(c.Name, c.Type))
                    .ToList();

                if (constantExpressionList.Count + parameterExpressionList.Count != listExpression.Initializers.Count)
                    throw new NotSupportedException(string.Format("Unrecognized parameters in Contains list.  Null parameters not supported."));

                if (parameterExpressionList.Any() || !SupportsIn())
                {
                    //  Have parameters or the EF provider does not support the DbInExpression.  Need to build a series of OR conditions so 
                    //  that we can include the DbParameterReferences.  EF will optimize this into an "in" condition but with our
                    //  DbParameterReferences preserved (which is not possible with a DbInExpression).
                    //  The DbParameterReferences will be intercepted as any other parameter.
                    dbExpression = null;
                    var allExpressions = parameterExpressionList.Cast<DbExpression>().Union(constantExpressionList.Cast<DbExpression>());
                    foreach (var paramReference in allExpressions)
                    {
                        var equalsExpression = DbExpressionBuilder.Equal(argExpression, paramReference);
                        if (dbExpression == null)
                            dbExpression = equalsExpression;
                        else
                            dbExpression = dbExpression.Or(equalsExpression);
                    }
                }
                else
                {
                    //  All values are constants so can use DbInExpression
                    dbExpression = DbExpressionBuilder.In(argExpression, constantExpressionList);
                }
            }

            MapExpressionToDbExpression(expression, dbExpression);
            return expression;
        }

        /// <summary>
        /// Returns true if this provider supports the DbInExpression.  Does this by checking to see if the provider
        /// is one that is known to NOT to support it will default to assuming it does.
        /// </summary>
        /// <returns></returns>
        private bool SupportsIn()
        {
            var entityConnection = _ObjectContext.Connection as System.Data.Entity.Core.EntityClient.EntityConnection;
            if (entityConnection == null)
                return true;

            //  Oracle does not support it
            return !entityConnection.StoreConnection.GetType().FullName.Contains("Oracle");
        }

        private Expression MapSimpleExpression(MethodCallExpression node, Func<DbExpression, DbFunctionExpression> dbExpressionFactory)
        {
            var expression = base.VisitMethodCall(node) as MethodCallExpression;

            DbExpression srcExpression = GetDbExpressionForExpression(expression.Object);
            var dbExpression = dbExpressionFactory(srcExpression);
            MapExpressionToDbExpression(expression, dbExpression);
            return expression;
        }

        private Expression MapStringLikeExpression(MethodCallExpression node, bool matchStart, bool matchEnd)
        {
            var expression = base.VisitMethodCall(node) as MethodCallExpression;

            if ((expression.Arguments == null) || (expression.Arguments.Count != 1))
                throw new ApplicationException("Did not find exactly 1 Argument to StartsWith function");

            DbExpression srcExpression = GetDbExpressionForExpression(expression.Object);

            DbExpression dbExpression;

            if (expression.Arguments[0] is ConstantExpression)
            {
                var constantExpression = GetDbExpressionForExpression(expression.Arguments[0]) as DbConstantExpression;
                if ((constantExpression == null) || (constantExpression.Value == null))
                    throw new NullReferenceException("Parameter to StartsWith cannot be null");

                string value = matchStart ? "" : "%";
                value += constantExpression.Value.ToString();
                if (!matchEnd)
                    value += "%";

                dbExpression = DbExpressionBuilder.Like(srcExpression, DbExpressionBuilder.Constant(value));
            }
            else
            {
                var argExpression = GetDbExpressionForExpression(expression.Arguments[0]);

                //  Note: Can also do this using StartsWith function on srcExpression (which avoids having to hardcode the % character).
                //  It works but generates some crazy conditions using charindex which I don't think will use indexes as well as "like"...
                //dbExpression = DbExpressionBuilder.Equal(DbExpressionBuilder.True, srcExpression.StartsWith(argExpression));

                DbExpression value = matchStart ? argExpression : DbExpressionBuilder.Constant("%").Concat(argExpression);
                if (!matchEnd)
                    value = value.Concat(DbExpressionBuilder.Constant("%"));

                dbExpression = DbExpressionBuilder.Like(srcExpression, value);
            }

            MapExpressionToDbExpression(expression, dbExpression);
            return expression;
        }

        private Expression MapAnyOrAllExpression(MethodCallExpression node)
        {
            if (_DataSpace != DataSpace.CSpace)
                throw new ApplicationException("Filters on child collections are only supported when using CSpace");

            if ((node.Arguments == null) || (node.Arguments.Count > 2))
                throw new ApplicationException("Any function call has more than 2 arguments");

            //  Visit the first argument so that we can get the DbPropertyExpression which is the source of the method call.
            var argument = node.Arguments[0];
            argument = RemoveConvert(argument);
            var sourceExpression = Visit(argument);
            var collectionExpression = GetDbExpressionForExpression(sourceExpression);

            //  Visit this DbExpression using the QueryVisitor in case it has it's own filters that need to be applied.
            var queryVisitor = new DynamicFilterQueryVisitorCSpace(_DbContext);
            collectionExpression = collectionExpression.Accept(queryVisitor);

            DbExpression dbExpression;
            if (node.Arguments.Count == 2)
            {
                //  The method call has a predicate that needs to be evaluated.  This must be done against the source
                //  argument - not the current binding.
                var binding = collectionExpression.Bind();

                //  Visit the lambda expression against this binding (which will evaluate all of the
                //  conditions in the expression against this binding and for this filter).
                var lambdaExpression = node.Arguments[1] as LambdaExpression;
                var visitor = new LambdaToDbExpressionVisitor(_Filter, binding, _DbContext, _DataSpace);
                var subExpression = visitor.Visit(lambdaExpression) as LambdaExpression;
                var subDbExpression = visitor.GetDbExpressionForExpression(subExpression.Body);

                //  Create an "Any" or "All" DbExpression from the results
                if (node.Method.Name == "All")
                    dbExpression = DbExpressionBuilder.All(binding, subDbExpression);
                else
                    dbExpression = DbExpressionBuilder.Any(binding, subDbExpression);
            }
            else
            {
                //  This should not even be possible - linq/IEnumerable does not have such a method!
                if (node.Method.Name == "All")
                    throw new ApplicationException("All() with no parameters is not supported");

                //  No predicate so just create an Any DbExpression against the collection expression
                dbExpression = DbExpressionBuilder.Any(collectionExpression);
            }

            MapExpressionToDbExpression(node, dbExpression);
            return node;
        }

        /// <summary>An Expression extension method that removes the convert described by @this.</summary>
        /// <param name="this">The @this to act on.</param>
        /// <returns>An Expression.</returns>
        internal static Expression RemoveConvert(Expression @this)
        {
            while (@this.NodeType == ExpressionType.Convert || @this.NodeType == ExpressionType.ConvertChecked)
            {
                @this = ((UnaryExpression)@this).Operand;
            }

            return @this;
        }

        private DbConstantExpression CreateConstantExpression(object value)
        {
            //  This is not currently supported (DbExpressionBuilder.Constant throws exceptions).  But, DbConstant has
            //  other constructors available that do take nullable types.  Maybe those would work.
            if (value == null)
                throw new ApplicationException("null is not convertable to a DbConstantExpression");

            if (_DataSpace == DataSpace.CSpace)
            {
                //  Must create the constant using a TypeUsage to handle Enum types.
                var typeUsage = TypeUsageForPrimitiveType(value.GetType());
                return DbExpressionBuilder.Constant(typeUsage, value);
            }
            else
            {
                //  Must map Enums to an int or EF/SQL will not know what to do with them.
                if (value.GetType().IsEnum)
                    return DbExpressionBuilder.Constant((int)value);

                return DbExpressionBuilder.Constant(value);
            }
        }

        #endregion

        #region Expression Mapping helpers

        private void MapExpressionToDbExpression(Expression expression, DbExpression dbExpression)
        {
            _ExpressionToDbExpressionMap[expression] = dbExpression;
        }

        private DbExpression GetDbExpressionForExpression(Expression expression)
        {
            var paramExpression = expression as ParameterExpression;
            if (paramExpression != null)
            {
                string paramName = paramExpression.Name;
                Type paramType = PrimitiveTypeForType(paramExpression.Type, _DataSpace);

                return CreateParameter(paramName, paramType);
            }

            DbExpression dbExpression;
            if (!_ExpressionToDbExpressionMap.TryGetValue(expression, out dbExpression))
                throw new FormatException(string.Format("DbExpression not found for expression: {0}", expression));

            return dbExpression;
        }

        private TypeUsage TypeUsageForPrimitiveType(Type type)
        {
            return TypeUsageForPrimitiveType(type, _ObjectContext, _DataSpace);
        }

        public static TypeUsage TypeUsageForPrimitiveType(Type type, ObjectContext objectContext, DataSpace dataSpace)
        {
            bool isNullable = IsNullableType(type);
            type = PrimitiveTypeForType(type, dataSpace);

            //  Find equivalent EdmType in CSpace.  This is a 1-to-1 mapping to CLR types except for the Geometry/Geography types
            //  (so not supporting those atm).
            EdmType edmType;
            if (dataSpace == DataSpace.CSpace)
            {
                if (type.IsEnum)
                {
                    edmType = objectContext.MetadataWorkspace.GetItems<EnumType>(DataSpace.CSpace).FirstOrDefault(e => e.MetadataProperties.Any(m => m.Name.EndsWith(":ClrType") && (Type)m.Value == type));
                    if (edmType == null)
                        throw new ApplicationException(string.Format("Unable to map parameter of type {0} to TypeUsage", type.FullName));
                }
                else
                {
                    var primitiveTypeList = objectContext.MetadataWorkspace
                        .GetPrimitiveTypes(DataSpace.CSpace)
                        .Where(p => p.ClrEquivalentType == type)
                        .ToList();
                    if (primitiveTypeList.Count != 1)
                        throw new ApplicationException(string.Format("Unable to map parameter of type {0} to TypeUsage.  Found {1} matching types", type.Name, primitiveTypeList.Count));
                    edmType = primitiveTypeList.FirstOrDefault();
                }
            }
            else
            {
                var primitiveTypeList = objectContext.MetadataWorkspace
                    .GetPrimitiveTypes(DataSpace.CSpace)
                    .Where(p => p.ClrEquivalentType == type)
                    .ToList();
                if (primitiveTypeList.Count != 1)
                    throw new ApplicationException(string.Format("Unable to map parameter of type {0} to TypeUsage.  Found {1} matching types", type.Name, primitiveTypeList.Count));
                edmType = primitiveTypeList.FirstOrDefault();
            }

            var facetList = new List<Facet>();
            if (isNullable)
            {
                //  May not even be necessary to specify these Facets, but just to be safe.  And only way to create them is to call the internal Create method...
                var createMethod = typeof(Facet).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(FacetDescription), typeof(object) }, null);

                var facetDescription = Facet.GetGeneralFacetDescriptions().FirstOrDefault(fd => fd.FacetName == "Nullable");
                if (facetDescription != null)
                    facetList.Add((Facet)createMethod.Invoke(null, new object[] { facetDescription, true }));

                facetDescription = Facet.GetGeneralFacetDescriptions().FirstOrDefault(fd => fd.FacetName == "DefaultValue");
                if (facetDescription != null)
                    facetList.Add((Facet)createMethod.Invoke(null, new object[] { facetDescription, null }));
            }

            return TypeUsage.Create(edmType, facetList);
        }

        /// <summary>
        /// Returns the primitive type of the Type.  If this is a collection type, this is the type of the objects inside the collection.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="dataSpace"></param>
        /// <returns></returns>
        private static Type PrimitiveTypeForType(Type type, DataSpace dataSpace)
        {
            if (dataSpace == DataSpace.SSpace)
            {
                if (type.IsEnum)
                    type = typeof(Int32);
            }

            if (type.IsGenericType)
            {
                //  Generic type of some sort.  Could be IEnumerable<T> or INullable<T>.  The primitive type is in the
                //  Generic Arguments.
                var genericArgs = type.GetGenericArguments();
                if ((genericArgs != null) && (genericArgs.Length == 1))
                {
                    type = genericArgs[0];

                    if (dataSpace == DataSpace.SSpace)
                    {
                        if (type.IsEnum)
                            type = typeof(Int32);
                    }

                    return type;
                }
            }

            if ((typeof(IEnumerable).IsAssignableFrom(type) && (type != typeof(String))) || (type == typeof(object)))
            {
                //  Non-generic collection (such as ArrayList) are not supported.  We require that we are able to enforce correct
                //  type matching between the collection and the db column.
                throw new NotSupportedException("Non generic collections or System.Object types are not supported");
            }

            return type;
        }

        /// <summary>
        /// Returns true if the expression is a DbPropertyExpression (i.e. a class property that has been mapped
        /// to a database column) and the type is a Nullable type.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private static bool IsNullableExpressionOfType<T>(DbExpression expression)
            where T : DbExpression
        {
            if (expression == null)
                return false;

            if (!typeof(T).IsAssignableFrom(expression.GetType()))
                return false;

            return expression.ResultType.Facets.Any(f => f.Name == "Nullable" && ((bool)f.Value));
        }

        private static bool IsNullableType(Type type)
        {
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        /// <summary>
        /// Returns true if the expression is for the "null" constant
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private bool IsNullConstantExpression(Expression expression)
        {
            var constantExpr = expression as ConstantExpression;
            if (constantExpr == null)
            {
                //  At some point, null constants were stored as a UnaryExpression (at least it was when commit
                //  #24a5699 on 2/17/2015 was made!)  Not sure when or what caused it to change so left this handling here.
                UnaryExpression unaryExpr = expression as UnaryExpression;
                if (unaryExpr != null)
                    constantExpr = unaryExpr.Operand as ConstantExpression;
            }

            if (constantExpr == null)
                return false;

            return (constantExpr.Value == null);
        }

        #endregion
    }
}
