using System;
using System.Data.Entity.Infrastructure.Interception;

namespace EntityFramework.DynamicFilters
{
    /// <summary>Manager for dynamic filters.</summary>
    public static class DynamicFilterManager
    {
        /// <summary>
        /// Gets or sets if the DynamicFilterInterceptor should be ignored.
        /// </summary>
        /// <value>True if the DynamicFilterInterceptor should be ignored.</value>
        public static Func<DbCommandTreeInterceptionContext, bool> ShouldIgnoreDynamicFilterInterceptor { get; set; }
    }
}
