using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace EntityFramework.DynamicFilters
{
    public class DynamicFilterParameters
    {
        //  Null will default to true but allows us to specifically enable/disable on the local scope level to
        //  override a filter that may be globally disabled.
        public bool? Enabled { get; set; }

        /// <summary>
        /// A delegate function that returns true/false to indicate if the filter is enabled.
        /// Can (optionally) take a single parameter for the current DbContext instance.
        /// Only evaluated if not null and if Enabled == true.
        /// </summary>
        public Func<DbContext, bool> EnableIfCondition { get; set; }

        public ConcurrentDictionary<string, Func<DbContext, object>> ParameterValues { get; private set; }

        public DynamicFilterParameters()
        {
            ParameterValues = new ConcurrentDictionary<string, Func<DbContext, object>>();
        }

        public void SetParameter(string parameterName, Func<DbContext, object> value)
        {
            ParameterValues.AddOrUpdate(parameterName, value, (k, v) => value);
        }
    }
}
