using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
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
        private ObjectContext _ObjectContext;

        private Dictionary<Expression, DbExpression> _ExpressionToDbExpressionMap = new Dictionary<Expression, DbExpression>();
        private Dictionary<string, DbPropertyExpression> _Properties = new Dictionary<string, DbPropertyExpression>();
        private Dictionary<string, DbParameterReferenceExpression> _Parameters = new Dictionary<string, DbParameterReferenceExpression>();

        #endregion

        #region Static methods & private Constructor

        public static DbExpression Convert(DynamicFilterDefinition filter, DbExpressionBinding binding, ObjectContext objectContext)
        {
            var visitor = new LambdaToDbExpressionVisitor(filter, binding, objectContext);
            var expression = visitor.Visit(filter.Predicate) as LambdaExpression;

            return visitor.GetDbExpressionForExpression(expression.Body);
        }

        private LambdaToDbExpressionVisitor(DynamicFilterDefinition filter, DbExpressionBinding binding, ObjectContext objectContext)
        {
            _Filter = filter;
            _Binding = binding;
            _ObjectContext = objectContext;
        }

        #endregion

        #region ExpressionVisitor Overrides

        protected override Expression VisitBinary(BinaryExpression node)
        {
            System.Diagnostics.Debug.Print("VisitBinary: {0}", node);
            var expression = base.VisitBinary(node) as BinaryExpression;

            DbExpression leftExpression = GetDbExpressionForExpression(expression.Left);
            DbExpression rightExpression = GetDbExpressionForExpression(expression.Right);

            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                    CreateBinaryExpression(expression, DbExpressionBuilder.Equal(leftExpression, rightExpression));
                    break;
                case ExpressionType.NotEqual:
                    CreateBinaryExpression(expression, DbExpressionBuilder.NotEqual(leftExpression, rightExpression));
                    break;
                case ExpressionType.GreaterThan:
                    CreateBinaryExpression(expression, DbExpressionBuilder.GreaterThan(leftExpression, rightExpression));
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    CreateBinaryExpression(expression, DbExpressionBuilder.GreaterThanOrEqual(leftExpression, rightExpression));
                    break;
                case ExpressionType.LessThan:
                    CreateBinaryExpression(expression, DbExpressionBuilder.LessThan(leftExpression, rightExpression));
                    break;
                case ExpressionType.LessThanOrEqual:
                    CreateBinaryExpression(expression, DbExpressionBuilder.LessThanOrEqual(leftExpression, rightExpression));
                    break;

                case ExpressionType.AndAlso:
                    CreateBinaryExpression(expression, DbExpressionBuilder.And(leftExpression, rightExpression));
                    break;
                case ExpressionType.OrElse:
                    CreateBinaryExpression(expression, DbExpressionBuilder.Or(leftExpression, rightExpression));
                    break;

                default:
                    throw new NotImplementedException(string.Format("Unhandled NodeType of {0} in LambdaToDbExpressionVisitor.VisitBinary", expression.NodeType));
            }

            return expression;
        }

        /// <summary>
        /// Creates the DbBinaryExpression for the given BinaryExpression.  Handles creating the "or param is null"
        /// condition for any parameter references so that the filter can be disabled.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="conditionExpression"></param>
        private void CreateBinaryExpression(BinaryExpression expression, DbBinaryExpression binaryExpression)
        {
            var leftParam = binaryExpression.Left as DbParameterReferenceExpression;
            var rightParam = binaryExpression.Right as DbParameterReferenceExpression;

            //  If any of these are DbParameterReferenceExpressions, need to 'or' a null check against them.
            //  This allows us to disable the parameter check (by setting the parameter value to null).
            //  There is no other way to disable the filters since the query must be fully cached and can
            //  never be rebuilt once it's cached.
            if (leftParam != null)
                binaryExpression = DbExpressionBuilder.Or(binaryExpression, DbExpressionBuilder.IsNull(leftParam));
            if (rightParam != null)
                binaryExpression = DbExpressionBuilder.Or(binaryExpression, DbExpressionBuilder.IsNull(rightParam));

            MapExpressionToDbExpression(expression, binaryExpression);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            System.Diagnostics.Debug.Print("VisitConstant: {0}", node);
            var expression = base.VisitConstant(node);

            if (node.Type == typeof(byte[]))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromBinary((byte[])node.Value));
            else if (node.Type == typeof(bool))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromBoolean((bool?)node.Value));
            else if (node.Type == typeof(byte))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromByte((byte?)node.Value));
            else if (node.Type == typeof(DateTime))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromDateTime((DateTime?)node.Value));
            else if (node.Type == typeof(DateTimeOffset))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromDateTimeOffset((DateTimeOffset?)node.Value));
            else if (node.Type == typeof(decimal))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromDecimal((decimal?)node.Value));
            else if (node.Type == typeof(double))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromDouble((double?)node.Value));
            else if (node.Type == typeof(Guid))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromGuid((Guid?)node.Value));
            else if (node.Type == typeof(Int16))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromInt16((Int16?)node.Value));
            else if (node.Type == typeof(Int32))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromInt32((Int32?)node.Value));
            else if (node.Type == typeof(Int64))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromInt64((Int64?)node.Value));
            else if (node.Type == typeof(float))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromSingle((float?)node.Value));
            else if (node.Type == typeof(string))
                MapExpressionToDbExpression(expression, DbConstantExpression.FromString((string)node.Value));
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
            System.Diagnostics.Debug.Print("VisitMember: {0}", node);
            var expression = base.VisitMember(node) as MemberExpression;

            var edmType = _Binding.VariableType.EdmType as EntityType;

            string propertyName = expression.Member.Name;
            DbPropertyExpression propertyExpression;
            if (!_Properties.TryGetValue(propertyName, out propertyExpression))
            {
                //  Not created yet

                //  Need to map through the EdmType properties to find the actual database/cspace name for the entity property.
                //  It may be different from the entity property!
                var edmProp = edmType.Properties.Where(p => p.MetadataProperties.Any(m => m.Name == "PreferredName" && m.Value.Equals(propertyName))).FirstOrDefault();
                if (edmProp == null)
                {
                    //  Accessing properties outside the main entity is not supported and will cause this exception.
                    throw new ApplicationException(string.Format("Property {0} not found in Entity Type {1}", propertyName, expression.Expression.Type.Name));
                }
                //  database column name is now in edmProp.Name.  Use that instead of filter.ColumnName

                propertyExpression = DbExpressionBuilder.Property(DbExpressionBuilder.Variable(_Binding.VariableType, _Binding.VariableName), edmProp.Name);
                _Properties.Add(propertyName, propertyExpression);
                System.Diagnostics.Debug.Print("Created new property expression for {0}", propertyName);
            }

            MapExpressionToDbExpression(expression, propertyExpression);
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
            System.Diagnostics.Debug.Print("VisitParameter: {0}", node);
            var expression = base.VisitParameter(node);

            if (node.Type.IsClass || node.Type.IsInterface)
                return expression;      //  Ignore class or interface param

            if (_Parameters.ContainsKey(node.Name))
                return expression;      //  Already created sql parameter for this node.Name

            //  Create a new DbParameterReferenceExpression for this parameter.
            var typeUsage = TypeUsageForPrimitiveType(node.Type);
            string dynFilterParamName = _Filter.CreateDynamicFilterName(node.Name);
            var param = typeUsage.Parameter(dynFilterParamName);

            System.Diagnostics.Debug.Print("Created new parameter for {0}: {1}", node.Name, dynFilterParamName);
            _Parameters.Add(node.Name, param);

            MapExpressionToDbExpression(expression, param);
            return expression;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            System.Diagnostics.Debug.Print("VisitUnary: {0}", node);
            var expression = base.VisitUnary(node) as UnaryExpression;

            DbExpression operandExpression = GetDbExpressionForExpression(expression.Operand);

            switch(expression.NodeType)
            {
                case ExpressionType.Not:
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
            System.Diagnostics.Debug.Print("VisitConditional: {0}", node);
            throw new NotImplementedException("Conditionals in Lambda expressions are not supported");
            //return base.VisitConditional(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            System.Diagnostics.Debug.Print("VisitMethodCall: {0}", node);

            //  TODO: Support this to handle Contains() by translating into a DbInExpression
            //if (node.Method.Name == "Contains")
            //    System.Diagnostics.Debug.Print("TODO: Found 'Contains' method call.  Need to map this to a DbInExpression");

            throw new NotImplementedException(string.Format("Unhandled Method of {0} in LambdaToDbExpressionVisitor.VisitMethodCall", node.Method.Name));

            //return base.VisitMethodCall(node);
        }

        #endregion

        #region Expression Mapping helpers

        private void MapExpressionToDbExpression(Expression expression, DbExpression dbExpression)
        {
            _ExpressionToDbExpressionMap[expression] = dbExpression;
        }

        private DbExpression GetDbExpressionForExpression(Expression expression)
        {
            DbExpression dbExpression;
            if (!_ExpressionToDbExpressionMap.TryGetValue(expression, out dbExpression))
                throw new FormatException(string.Format("DbExpression not found for expression: {0}", expression));

            return dbExpression;
        }

        private TypeUsage TypeUsageForPrimitiveType(Type type)
        {
            bool isNullable = (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));

            var genericArgs = type.GetGenericArguments();
            if ((genericArgs != null) && (genericArgs.Length == 1))
            {
                //  Generic collection or nullable.  Need to find the primitive type of the generic arg
                type = genericArgs[0];
            }

            //  Find equivalent EdmType in CSpace.  This is a 1-to-1 mapping to CLR types except for the Geometry/Geography types
            //  (so not supporting those atm).
            var primitiveTypeList = _ObjectContext.MetadataWorkspace
                .GetPrimitiveTypes(DataSpace.CSpace)
                .Where(p => p.ClrEquivalentType == type)
                .ToList();
            if (primitiveTypeList.Count != 1)
                throw new ApplicationException(string.Format("Unable to map parameter of type {0} to TypeUsage.  Found {1} matching types", type.Name, primitiveTypeList.Count));
            var primitiveType = primitiveTypeList.FirstOrDefault();

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

            return TypeUsage.Create(primitiveType, facetList);
        }

        #endregion
    }
}
