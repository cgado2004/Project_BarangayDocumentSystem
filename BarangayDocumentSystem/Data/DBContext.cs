// ---------------------------------------------------------------------------
//  DBContext.cs - the doorway to the database.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;

namespace BarangayDocumentSystem.Data
{
    /// <summary>
    /// Holds the connection settings and hands out the helper everything else
    /// uses.
    ///
    /// I will answer the question my panel asked me twice ("where is the
    /// DBContext?") the same way here as I do in the report: this is it. It is
    /// the object that knows which engine we are talking to, what its
    /// connection string is, whether the database is reachable, whether the
    /// stored procedures are installed, and how to open a transaction. It is
    /// built once at startup in Program.cs and passed down to the repository
    /// and the screens, so no form ever builds its own connection.
    ///
    /// The important design choice: the screens never see this class. They see
    /// IBarangayRepository. That is what lets me run the whole test suite
    /// without a database at all.
    /// </summary>
    public class DBContext : IDisposable
    {
        private bool _disposed;

        public IDbProvider Provider { get; private set; }
        public string ConnectionString { get; private set; }
        public DBHelper Helper { get; private set; }

        /// <summary>What the status bar shows: engine and database name.</summary>
        public string Description
        {
            get
            {
                if (Provider == null) return "No database configured";

                string name = AppConfig.DatabaseName;
                return Provider.DisplayName + (string.IsNullOrEmpty(name) ? string.Empty : " - " + name);
            }
        }

        public DBContext(IDbProvider provider, string connectionString)
        {
            if (provider == null) throw new ArgumentNullException("provider");

            Provider = provider;
            ConnectionString = connectionString ?? string.Empty;
            Helper = new DBHelper(provider, ConnectionString);
        }

        /// <summary>
        /// Builds the context the program is configured to use, straight from
        /// App.config.
        /// </summary>
        public static DBContext FromConfiguration()
        {
            IDbProvider provider = DbProviderFactory.Create(AppConfig.StorageProvider);
            return new DBContext(provider, provider.SupportsDatabaseCreation ? AppConfig.ConnectionString : string.Empty);
        }

        // ==================================================================
        //  Is it alive?
        // ==================================================================

        /// <summary>
        /// Answers "is the database connected?" - the question the login
        /// screen asks before it shows a password box.
        ///
        /// When it cannot connect, the sentence that comes back names the
        /// engine and the database, because "the database is not running" is
        /// useless if you have two engines installed and cannot tell which one
        /// the program wants.
        /// </summary>
        public bool TryConnect(out string message)
        {
            if (Provider == null || !Provider.SupportsDatabaseCreation)
            {
                message = "The in-memory store is in use, so nothing is saved. Change Storage in App.config to MySQL or SqlServer to keep records.";
                return true;
            }

            try
            {
                if (!Helper.CanConnect())
                {
                    message = "I reached the server but it did not answer the test query.";
                    return false;
                }

                message = "Connected to " + Description + ".";
                return true;
            }
            catch (RepositoryException error)
            {
                message = error.Message;
                return false;
            }
        }

        /// <summary>The database the context points at, if it exists yet.</summary>
        public bool DatabaseExists()
        {
            if (Provider == null || !Provider.SupportsDatabaseCreation) return true;

            string databaseName = AppConfig.DatabaseName;
            if (string.IsNullOrEmpty(databaseName)) return false;

            try
            {
                using (DbConnection connection = Helper.OpenServerConnection(
                    DBHelper.StripDatabase(ConnectionString)))
                {
                    using (DbCommand command = Helper.BuildOn(connection,
                        Provider.Key == "SqlServer"
                            ? "SELECT COUNT(*) FROM sys.databases WHERE name = @name"
                            : "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = @name",
                        new SqlArguments().Add("@name", databaseName), CommandType.Text))
                    {
                        object count = command.ExecuteScalar();
                        return count != null && Convert.ToInt32(count) > 0;
                    }
                }
            }
            catch (Exception error)
            {
                AppLog.Warn("Could not check whether the database exists: " + error.Message);
                return false;
            }
        }

        /// <summary>Creates the database itself (not the tables). Called by
        /// the initializer on the very first run.</summary>
        public bool CreateDatabase(out string message)
        {
            message = string.Empty;
            if (Provider == null || !Provider.SupportsDatabaseCreation) return true;

            string databaseName = AppConfig.DatabaseName;
            if (string.IsNullOrEmpty(databaseName))
            {
                message = "The connection string does not name a database, so I cannot create one.";
                return false;
            }

            if (DatabaseExists()) return true;

            try
            {
                using (DbConnection connection = Helper.OpenServerConnection(
                    DBHelper.StripDatabase(ConnectionString)))
                using (DbCommand command = Helper.BuildOn(connection,
                    Provider.CreateDatabaseStatement(databaseName), null, CommandType.Text))
                {
                    command.ExecuteNonQuery();
                }

                message = "Created the database " + databaseName + ".";
                AppLog.Info(message);
                return true;
            }
            catch (RepositoryException error)
            {
                message = error.Message;
                return false;
            }
        }

        /// <summary>Which of the stored procedures the report and dashboard
        /// queries need are actually installed. The admin screen lists this.</summary>
        public IList<string> GetInstalledProcedures()
        {
            if (Provider == null || !Provider.SupportsDatabaseCreation) return new List<string>();
            return Helper.ListStoredProcedures();
        }

        /// <summary>The slowest query so far this session, in milliseconds.
        /// The status bar shows it so a performance problem is never
        /// invisible.</summary>
        public int LastCommandMilliseconds
        {
            get { return (int)Helper.LastCommandDuration.TotalMilliseconds; }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
