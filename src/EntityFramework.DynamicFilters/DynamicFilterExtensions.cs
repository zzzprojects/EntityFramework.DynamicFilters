using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.Entity.ModelConfiguration;
using System.Linq.Expressions;
using System.Reflection;

namespace EntityFramework.DynamicFilters
{
    public static class DynamicFilterExtensions
    {
        #region Privates

        private static ConcurrentDictionary<DbContext, ConcurrentDictionary<string, object>> _ScopedParameterValues = new ConcurrentDictionary<DbContext, ConcurrentDictionary<string, object>>();
        private static ConcurrentDictionary<string, object> _GlobalParameterValues = new ConcurrentDictionary<string, object>();

        #endregion

        #region Initialize

        /// <summary>
        /// Initialize the Dynamic Filters.  Call this in OnModelCreating().
        /// </summary>
        /// <param name="context"></param>
        public static void InitializeDynamicFilters(this DbContext context)
        {
            DbInterception.Add(new DynamicFilterCommandInterceptor());
            DbInterception.Add(new DynamicFilterInterceptor());
        }

        #endregion

        #region Add Filters

        /// <summary>
        /// Add a filter to a single entity.  Use in OnModelCreating() as:
        ///     modelBuilder.Entity<MyEntity>().Filter(...)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="config"></param>
        /// <param name="filterName"></param>
        /// <param name="columnName"></param>
        /// <param name="globalValue">If not null, specifies a globally scoped value for this parameter</param>
        /// <returns></returns>
        public static EntityTypeConfiguration<TEntity> Filter<TEntity>(this EntityTypeConfiguration<TEntity> config, 
            string filterName, string columnName, object globalValue = null)
            where TEntity : class
        {
            filterName = ScrubFilterName(filterName);
            var filterDefinition = new DynamicFilterDefinition(filterName, columnName, typeof(TEntity));

            config.HasTableAnnotation(filterDefinition.AttributeName, filterDefinition);

            if (globalValue != null)
                SetFilterGlobalParameterValue(null, filterName, globalValue);

            return config;
        }

        /// <summary>
        /// Add a filter to a single entity.  Use in OnModelCreating() as:
        ///     modelBuilder.Entity<MyEntity>().Filter(...)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="config"></param>
        /// <param name="filterName"></param>
        /// <param name="path"></param>
        /// <param name="globalFuncValue">If not null, specifies a globally scoped value for this parameter as a delegate.</param>
        /// <returns></returns>
        public static EntityTypeConfiguration<TEntity> Filter<TEntity, TProperty>(this EntityTypeConfiguration<TEntity> config, 
            string filterName, Expression<Func<TEntity, TProperty>> path, Func<object> globalFuncValue = null)
            where TEntity : class
        {
            return config.Filter(filterName, ParseColumnNameFromExpression(path), globalFuncValue);
        }

        public static void Filter<TEntity, TProperty>(this DbModelBuilder modelBuilder, string filterName, Expression<Func<TEntity, TProperty>> path, object globalValue = null)
        {
            filterName = ScrubFilterName(filterName);

            modelBuilder.Conventions.Add(new DynamicFilterConvention(filterName, typeof(TEntity), ParseColumnNameFromExpression(path)));

            if (globalValue != null)
                SetFilterGlobalParameterValue(null, filterName, globalValue);
        }

        public static void Filter<TEntity, TProperty>(this DbModelBuilder modelBuilder, string filterName, Expression<Func<TEntity, TProperty>> path, Func<object> globalFuncValue)
            where TEntity : class
        {
            modelBuilder.Filter(filterName, path, (object)globalFuncValue);
        }

        #endregion

        #region Set Filter Parameter Values

        /// <summary>
        /// Set the parameter for a filter within the current DbContext scope.  Once the DbContext is disposed, this
        /// parameter will no longer be in scope and will be removed.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filterName"></param>
        /// <param name="func">A delegate that returns the value of the parameter.  This will be evaluated each time
        /// the parameter value is needed.</param>
        public static void SetFilterScopedParameterValue(this DbContext context, string filterName, Func<object> func)
        {
            context.SetFilterScopedParameterValue(filterName, (object)func);
        }

        /// <summary>
        /// Set the parameter for a filter within the current DbContext scope.  Once the DbContext is disposed, this
        /// parameter will no longer be in scope and will be removed.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filterName"></param>
        /// <param name="value"></param>
        public static void SetFilterScopedParameterValue(this DbContext context, string filterName, object value)
        {
            filterName = ScrubFilterName(filterName);

            var newFilterParams = new ConcurrentDictionary<string, object>();
            var filterParams = _ScopedParameterValues.GetOrAdd(context, newFilterParams);
            filterParams.AddOrUpdate(filterName, value, (k, v) => value);

            if (filterParams == newFilterParams)
            {
                System.Diagnostics.Debug.Print("Created new scoped filter params.  Have {0} scopes", _ScopedParameterValues.Count);

                //  We created new filter params for this scope.  Add an event handler to the OnDispose to clean them up when
                //  the context is disposed.
                var internalContext = typeof(DbContext)
                    .GetProperty("InternalContext", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetGetMethod(true)
                    .Invoke(context, null);

                var eventInfo = internalContext.GetType().GetEvent("OnDisposing", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                eventInfo.AddEventHandler(internalContext, new EventHandler<EventArgs>((o, e) => context.ClearScopedParameters()));
            }
        }

        /// <summary>
        /// Set the parameter value for a filter with global scope.  If a scoped parameter value is not found, this
        /// value will be used.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filterName"></param>
        /// <param name="func">A delegate that returns the value of the parameter.  This will be evaluated each time
        /// the parameter value is needed.</param>
        public static void SetFilterGlobalParameterValue(this DbContext context, string filterName, Func<object> func)
        {
            context.SetFilterGlobalParameterValue(filterName, (object)func);
        }

        /// <summary>
        /// Set the parameter value for a filter with global scope.  If a scoped parameter value is not found, this
        /// value will be used.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filterName"></param>
        /// <param name="value"></param>
        public static void SetFilterGlobalParameterValue(this DbContext context, string filterName, object value)
        {
            filterName = ScrubFilterName(filterName);

            _GlobalParameterValues.AddOrUpdate(filterName, value, (k, v) => value);
        }

        #endregion

        #region Get Filter Parameter Values

        /// <summary>
        /// Returns the value for the filter.  If a scoped value exists within this DbContext, that is returned.
        /// Otherwise, a global parameter value will be returned.  If the parameter was set with a delegate, the
        /// delegate is evaluated and the result is returned.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filterName"></param>
        /// <returns></returns>
        public static object GetFilterParameterValue(this DbContext context, string filterName)
        {
            filterName = ScrubFilterName(filterName);

            ConcurrentDictionary<string, object> filterParams;
            object value;

            //  First try to get the value from _ScopedParameterValues
            if (_ScopedParameterValues.TryGetValue(context, out filterParams))
            {
                if (filterParams.TryGetValue(filterName, out value))
                {
                    var func = value as Func<object>;
                    return (func == null) ? value : func();
                }
            }

            //  Then try _GlobalParameterValues
            if (_GlobalParameterValues.TryGetValue(filterName, out value))
            {
                var func = value as Func<object>;
                return (func == null) ? value : func();
            }

            //  Not found anywhere???
            return null;
        }

        #endregion

        #region Clear Parameter Values

        /// <summary>
        /// Clear all parameter values within the DbContext scope.
        /// </summary>
        /// <param name="context"></param>
        public static void ClearScopedParameters(this DbContext context)
        {
            ConcurrentDictionary<string, object> filterParams;
            _ScopedParameterValues.TryRemove(context, out filterParams);

            System.Diagnostics.Debug.Print("Cleared scoped parameters.  Have {0} scopes", _ScopedParameterValues.Count);
        }

        #endregion

        #region Set Sql Parameters

        internal static void SetSqlParameter(this DbContext context, DbParameter param)
        {
            if (!param.ParameterName.StartsWith(DynamicFilterConstants.PARAMETER_NAME_PREFIX))
                return;

            //  parts are:
            //  1 = Fixed string constant (DynamicFilterConstants.PARAMETER_NAME_PREFIX)
            //  2 = Filter Name (delimiter char is scrubbed from this field when creating a filter)
            //  3+ = Column Name (this can contain the delimiter char)
            var parts = param.ParameterName.Split(new string[] { DynamicFilterConstants.DELIMETER }, StringSplitOptions.None);
            if (parts.Length < 3)
                return;

            object value = context.GetFilterParameterValue(parts[1]);       //  Middle is the filter name

            //  If not found, leave as the default that EF assigned
            if (value != null)
                param.Value = value;
        }

        #endregion

        #region Private Methods

        private static string ScrubFilterName(string filterName)
        {
            //  Do not allow the delimiter char in the filter name at all because it will interfere with us parsing out
            //  the filter name from the parameter name.  Doesn't matter in column name though.
            return filterName.Replace(DynamicFilterConstants.DELIMETER, "");
        }

        private static string ParseColumnNameFromExpression(LambdaExpression expression)
        {
            if (expression == null)
                throw new ArgumentNullException("Lambda expression is null");

            var body = expression.Body as MemberExpression;
            if ((body == null) || (body.Member == null) || string.IsNullOrEmpty(body.Member.Name))
                throw new InvalidCastException("Lambda expression does not contain a property name");

            return body.Member.Name;
        }

        #endregion
    }
}
