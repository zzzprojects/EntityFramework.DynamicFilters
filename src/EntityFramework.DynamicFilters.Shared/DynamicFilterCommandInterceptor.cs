using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;

namespace EntityFramework.DynamicFilters
{
    class DynamicFilterCommandInterceptor : IDbCommandInterceptor
    {
        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
        }

        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            SetDynamicFilterParameterValues(command, interceptionContext.DbContexts.FirstOrDefault());
        }

        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
        }

        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            SetDynamicFilterParameterValues(command, interceptionContext.DbContexts.FirstOrDefault());
        }

        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
        }

        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            SetDynamicFilterParameterValues(command, interceptionContext.DbContexts.FirstOrDefault());
        }

        private void SetDynamicFilterParameterValues(DbCommand command, DbContext context)
        {
            if ((command == null) || (command.Parameters == null) || (command.Parameters.Count == 0) || (context == null))
                return;

            context.SetSqlParameters(command);
        }
    }
}
