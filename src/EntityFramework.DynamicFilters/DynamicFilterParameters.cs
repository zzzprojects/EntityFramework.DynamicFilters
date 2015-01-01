using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityFramework.DynamicFilters
{
    public class DynamicFilterParameters
    {
        public bool Enabled { get; set; }

        public ConcurrentDictionary<string, object> ParameterValues { get; private set; }

        public DynamicFilterParameters()
        {
            Enabled = true;
            ParameterValues = new ConcurrentDictionary<string, object>();
        }

        public void SetParameter(string parameterName, object value)
        {
            ParameterValues.AddOrUpdate(parameterName, value, (k, v) => value);
        }
    }
}
