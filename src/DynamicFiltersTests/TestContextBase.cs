using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using EntityFramework.DynamicFilters;
using Oracle.ManagedDataAccess.Client;

namespace DynamicFiltersTests
{
    public interface ITestContext
    {
        void Seed();

        string SchemaName { get; }
    }

    /// <summary>
    /// Base DbContext used by all tests.  Handles special requirements for initializing an Oracle database.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class TestContextBase<T> : DbContext
        where T : DbContext, ITestContext
    {
        /// <summary>
        /// Default constructor that sets the Connection String Name to TestContext and uses the default initializer.
        /// </summary>
        public TestContextBase()
            : base("TestContext")
        {
            Database.SetInitializer(new ContentInitializer<T>());
            Database.Log = log => System.Diagnostics.Debug.WriteLine(log);
            Database.Initialize(false);
        }

        /// <summary>
        /// Constructor that only sets the Connection String Name - database initializer is not set.
        /// </summary>
        /// <param name="nameOrConnectionString"></param>
        public TestContextBase(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        /// <summary>
        /// Consatructor that uses the given dbConnection and configures the default initializer
        /// </summary>
        /// <param name="dbConnextion"></param>
        public TestContextBase(DbConnection dbConnextion)
            : base(dbConnextion, false)
        {
            Database.SetInitializer(new ContentInitializer<T>());
            Database.Log = log => System.Diagnostics.Debug.WriteLine(log);
            Database.Initialize(false);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            if (this.IsOracle())
            {
                //  For Oracle, we must set the default schema or EF will try to use "dbo" which will not be valid.
                //  And it must be upper case.
                modelBuilder.HasDefaultSchema(SchemaName);
            }

            //  Reset DynamicFilters to an initial state - this discards anything statically cached from other tests
            modelBuilder.ResetDynamicFilters(); //  *** Do not do this in normal production code! ***

            base.OnModelCreating(modelBuilder);
        }

        public abstract void Seed();

        public string SchemaName
        {
            get
            {
                if (!this.IsOracle())
                    return "dbo";

                //  Can't use the EF default schema of "dbo" for Oracle...
                var builder = new OracleConnectionStringBuilder(Database.Connection.ConnectionString);
                return builder.UserID.ToUpper();
            }
        }
    }

    public class ContentInitializer<T> : DropCreateDatabaseAlways<T>
        where T : DbContext, ITestContext
    {
        #region Initialize/Seed

        public override void InitializeDatabase(T context)
        {
            if (context.IsOracle())
            {
                //  Under Oracle, the DropCreateDatabaseAlways doesn't do what it says it does...
                //  It throws a table already exists error if __MigrationHistory is already there.
                //  It does not remove any existing objects but it does replace any existing objects
                //  that are in this context.
                DropExistingOracleObjects(context);
            }

            base.InitializeDatabase(context);
        }

        protected override void Seed(T context)
        {
            context.Seed();
        }

        #endregion

        #region DropExistingOracleObjects

        private void DropExistingOracleObjects(T context)
        {
            var schema = context.SchemaName;
            var connectionString = context.Database.Connection.ConnectionString;

            foreach (var tableName in FindOracleTableNames(connectionString))
                DropOracleTable(context, schema, tableName);

            foreach (var sequenceName in FindOracleSequenceNames(connectionString))
                DropOracleSequence(context, schema, sequenceName);
        }

        private IEnumerable<string> FindOracleTableNames(string connectionString)
        {
            var tablesNames = new List<string>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                using (var cmd = new OracleCommand("select table_name from user_tables", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            tablesNames.Add(reader.GetString(0));
                    }
                }
            }

            return tablesNames;
        }

        private void DropOracleTable(T context, string schema, string tableName)
        {
            try
            {
                context.Database.ExecuteSqlCommand(string.Format("drop table \"{0}\".\"{1}\" cascade constraints", schema, tableName));
            }
            catch { }
        }

        private IEnumerable<string> FindOracleSequenceNames(string connectionString)
        {
            var sequenceNames = new List<string>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                using (var cmd = new OracleCommand("select sequence_name from user_sequences", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            sequenceNames.Add(reader.GetString(0));
                    }
                }
            }

            return sequenceNames;
        }

        private void DropOracleSequence(T context, string schema, string sequenceName)
        {
            try
            {
                context.Database.ExecuteSqlCommand(string.Format("drop sequence \"{0}\".\"{1}\"", schema, sequenceName));
            }
            catch { }
        }

        #endregion

    }
}
