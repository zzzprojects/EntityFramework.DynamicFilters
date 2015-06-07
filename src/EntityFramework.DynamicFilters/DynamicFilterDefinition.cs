using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq.Expressions;

namespace EntityFramework.DynamicFilters
{
    internal class DynamicFilterDefinition
    {
        public string FilterName { get; private set; }

        /// <summary>
        /// Set if the filter is a single column equality filter.  Null if filter is a Predicate (LambdaExpression)
        /// </summary>
        public string ColumnName { get; private set; }

        /// <summary>
        /// Set if the filter is a LambdaExpression.  Null if filter is a single column equality filter.
        /// </summary>
        public LambdaExpression Predicate { get; private set; }

        public Type CLRType { get; private set; }

        public string AttributeName { get { return string.Concat(DynamicFilterConstants.ATTRIBUTE_NAME_PREFIX, DynamicFilterConstants.DELIMETER, CLRType.Name, DynamicFilterConstants.DELIMETER, FilterName); } }

        internal DynamicFilterDefinition(string filterName, LambdaExpression predicate, string columnName, Type clrType)
        {
            FilterName = filterName;
            Predicate = predicate;
            ColumnName = columnName;
            CLRType = clrType;
        }

        #region Filter Name mapping

        //  These methods handle mapping Filter/Parameter names into a DB friendly parameter name.  This is necessary
        //  because Oracle has a 30 character max identifier length!

        private static Dictionary<Tuple<string, string>, int> _FilterParamToDBParamIndex = new Dictionary<Tuple<string, string>, int>();
        private static Dictionary<int, Tuple<string, string>> _ParamIndexToFilterAndParam = new Dictionary<int, Tuple<string, string>>();

        public string CreateDynamicFilterName(string parameterName)
        {
            var filterParamKey = Tuple.Create(FilterName, parameterName);

            lock (_FilterParamToDBParamIndex)
            {
                int dbParamIndex;
                if (!_FilterParamToDBParamIndex.TryGetValue(filterParamKey, out dbParamIndex))
                {
                    dbParamIndex = _FilterParamToDBParamIndex.Count + 1;
                    _FilterParamToDBParamIndex.Add(filterParamKey, dbParamIndex);
                    _ParamIndexToFilterAndParam[dbParamIndex] = filterParamKey;
                }

                return string.Concat(DynamicFilterConstants.PARAMETER_NAME_PREFIX, DynamicFilterConstants.DELIMETER, dbParamIndex.ToString());
            }
        }

        public string CreateFilterDisabledParameterName()
        {
            return CreateDynamicFilterName(DynamicFilterConstants.FILTER_DISABLED_NAME);
        }

        /// <summary>
        /// Reaturns the Filter name and Parameter names associated with the db parameter
        /// </summary>
        /// <param name="dbParameter"></param>
        /// <returns></returns>
        public static Tuple<string,string> GetFilterAndParamFromDBParameter(string dbParameter)
        {
            if (!dbParameter.StartsWith(DynamicFilterConstants.PARAMETER_NAME_PREFIX))
                return null;    //  Not dynamic filter param

            //  parts are:
            //  1 = Fixed string constant (DynamicFilterConstants.PARAMETER_NAME_PREFIX)
            //  2 = Index number used to look up the Filter & Parameter name
            var parts = dbParameter.Split(new string[] { DynamicFilterConstants.DELIMETER }, StringSplitOptions.None);
            if (parts.Length != 2)
                throw new ApplicationException(string.Format("Invalid format for Dynamic Filter parameter name: {0}", dbParameter));

            int dbParamIndex;
            if (!int.TryParse(parts[1], out dbParamIndex))
                throw new ApplicationException(string.Format("Unable to parse {0} as int", parts[1]));

            Tuple<string, string> filterParamKey;
            if (!_ParamIndexToFilterAndParam.TryGetValue(dbParamIndex, out filterParamKey))
                throw new ApplicationException(string.Format("Param {0} not found in _ParamIndexToFilterAndParam", dbParamIndex));

            return filterParamKey;
        }

        #endregion
    }
}
