using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq.Expressions;

namespace EntityFramework.DynamicFilters
{
    [Serializable]
    public class DynamicFilterDefinition
    {
        /// <summary>
        /// Unique ID assigned to each distinct filter.  Used to find unique filters on an entity
        /// which may be involved in TPT/TPH (which can cause the same filter to be added to the
        /// base class as well as derived classes).
        /// </summary>
        public Guid ID { get; private set; }

        public string FilterName { get; private set; }

        /// <summary>
        /// Set if the filter is a single column equality filter.  Null if filter is a Predicate (LambdaExpression)
        /// </summary>
        public string ColumnName { get; private set; }



        [NonSerialized]
        private LambdaExpression _predicate;


        /// <summary>
        /// Set if the filter is a LambdaExpression.  Null if filter is a single column equality filter.
        /// </summary>        

        public LambdaExpression Predicate { get { return _predicate; } private set { _predicate = value; } }

        public Type CLRType { get; private set; }

        public DynamicFilterOptions Options { get; private set; }

        public string AttributeName { get { return DynamicFilterConstants.ATTRIBUTE_NAME_PREFIX; } }


        internal DynamicFilterDefinition(Guid id, string filterName, LambdaExpression predicate, string columnName, Type clrType, DynamicFilterOptions options)
        {
            ID = id;
            FilterName = filterName;
            Predicate = predicate;
            ColumnName = columnName;
            CLRType = clrType;
            Options = options;
        }

        #region Filter Name mapping

        //  These methods handle mapping Filter/Parameter names into a DB friendly parameter name.  This is necessary
        //  because Oracle has a 30 character max identifier length!

        private static Dictionary<Tuple<string, string, DataSpace>, int> _FilterParamToDBParamIndex = new Dictionary<Tuple<string, string, DataSpace>, int>();
        private static Dictionary<int, Tuple<string, string, DataSpace>> _ParamIndexToFilterAndParam = new Dictionary<int, Tuple<string, string, DataSpace>>();

        public string CreateDynamicFilterName(string parameterName, DataSpace dataSpace)
        {
            var filterParamKey = Tuple.Create(FilterName, parameterName, dataSpace);

            lock (_FilterParamToDBParamIndex)
            {
                int dbParamIndex;
                if (!_FilterParamToDBParamIndex.TryGetValue(filterParamKey, out dbParamIndex))
                {
                    dbParamIndex = _FilterParamToDBParamIndex.Count + 1;
                    _FilterParamToDBParamIndex.Add(filterParamKey, dbParamIndex);
                    _ParamIndexToFilterAndParam[dbParamIndex] = filterParamKey;
                }

                //  Using 6 digits here because we are now looking for the "is disabled" parameter by name in the sql statement to
                //  remove it when the filter is enabled.  Don't want to match on "_10" when we're looking for "_1".
                return string.Format("{0}{1}{2:D6}", DynamicFilterConstants.PARAMETER_NAME_PREFIX, DynamicFilterConstants.DELIMETER, dbParamIndex);
            }
        }

        public string CreateFilterDisabledParameterName(DataSpace dataSpace)
        {
            return CreateDynamicFilterName(DynamicFilterConstants.FILTER_DISABLED_NAME, dataSpace);
        }

        /// <summary>
        /// Reaturns the Filter name and Parameter names associated with the db parameter
        /// </summary>
        /// <param name="dbParameter"></param>
        /// <returns></returns>
        public static Tuple<string, string, DataSpace> GetFilterAndParamFromDBParameter(string dbParameter)
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

            Tuple<string, string, DataSpace> filterParamKey;
            if (!_ParamIndexToFilterAndParam.TryGetValue(dbParamIndex, out filterParamKey))
                throw new ApplicationException(string.Format("Param {0} not found in _ParamIndexToFilterAndParam", dbParamIndex));

            return filterParamKey;
        }

        #endregion
    }
}
