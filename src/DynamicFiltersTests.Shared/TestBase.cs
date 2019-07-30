using System;

namespace DynamicFiltersTests
{
    public abstract class TestBase
    {
        protected void HandleException(Exception ex)
        {
            //  If db provider can't change CommandText (i.e. SQL Server CE), an exception is the expected result so ignore it
            if (ex.InnerException?.Message?.Contains("does not support modifing the DbCommand.CommandText property") ?? false)
                return;

            throw ex;
        }
    }
}
