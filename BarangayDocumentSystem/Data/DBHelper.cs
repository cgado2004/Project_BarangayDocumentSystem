// ---------------------------------------------------------------------------
//  DBHelper.cs - the small set of database chores every query needs.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;

namespace BarangayDocumentSystem.Data
{
    /// <summary>
    /// The worker that opens connections and runs commands.
    ///
    /// Why a separate class instead of writing "using (var conn = new
    /// MySqlConnection(...))" in fifty places: because then fifty places also
    /// need the parameter code, the error translation, the retry when MySQL is
    /// still starting up, and the timing. Here it happens once.
    ///
    /// What it gives the rest of the program:
    ///   * a connection that always closes, even when something throws;
    ///   * every value sent as a parameter (no SQL injection, ever);
    ///   * a database error turned into a sentence a clerk can act on;
    ///   * one retry when the very first connection fails, because a laptop
    ///     with XAMPP sometimes needs a second before MySQL is listening;
    ///   * how long the query took, which is how I answer the question "is it
    ///     working properly?" with a number instead of an opinion.
    /// </summary>
    public class DBHelper
    {
        private readonly IDbProvider _provider;
        private readonly string _connectionString;

        /// <summary>How long the last command took. The dashboard shows it in
        /// the status bar, because a slow screen should be visible, not a
        /// mystery.</summary>
        public TimeSpan LastCommandDuration { get; private set; }

        /// <summary>How many commands this helper has run since the program
        /// started. Handy when I want to prove the screens are not hammering
        /// the database.</summary>
        public int CommandCount { get; private set; }

        public IDbProvider Provider { get { return _provider; } }

        public DBHelper(IDbProvider provider, string connectionString)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            _provider = provider;
            _connectionString = connectionString == null ? string.Empty : connectionString;
        }

        // ==================================================================
        //  Connections
        // ==================================================================

        /// <summary>
        /// Opens a connection. The schema used by the initializer is passed in
        /// by the caller so that step can connect with no database selected.
        /// </summary>
        public DbConnection OpenConnection()
        {
            return OpenConnection(_connectionString, true);
        }

        public DbConnection OpenConnection(string connectionString, bool retryOnce)
        {
            try
            {
                DbConnection connection = _provider.CreateConnection(connectionString);
                connection.Open();
                return connection;
            }
            catch (Exception first)
            {
                if (!retryOnce)
                    throw RepositoryException.FromDatabase("opening the database connection", first);

                // The one retry. A cold XAMPP install answers a moment late,
                // and making the clerk click "try again" for that would be
                // silly.
                AppLog.Warn("First connection attempt failed (" + first.Message + "). Trying once more.");
                Thread.Sleep(700);
                return OpenConnection(connectionString, false);
            }
        }

        /// <summary>Opens a connection to the server with no database chosen,
        /// so a missing database can be created.</summary>
        public DbConnection OpenServerConnection(string connectionStringWithoutDatabase)
        {
            return OpenConnection(connectionStringWithoutDatabase, true);
        }

        // ==================================================================
        //  Running things
        // ==================================================================

        public int ExecuteNonQuery(string operation, string sql, SqlArguments arguments)
        {
            return Run(operation, delegate (DbConnection connection)
            {
                using (DbCommand command = Build(sql, connection, arguments, CommandType.Text))
                {
                    return command.ExecuteNonQuery();
                }
            });
        }

        public object ExecuteScalar(string operation, string sql, SqlArguments arguments)
        {
            return Run(operation, delegate (DbConnection connection)
            {
                using (DbCommand command = Build(sql, connection, arguments, CommandType.Text))
                {
                    object value = command.ExecuteScalar();
                    return value == DBNull.Value ? null : value;
                }
            });
        }

        public DataTable QueryTable(string operation, string sql, SqlArguments arguments)
        {
            return Run(operation, delegate (DbConnection connection)
            {
                using (DbCommand command = Build(sql, connection, arguments, CommandType.Text))
                using (DbDataAdapter adapter = CreateAdapter(command))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            });
        }

        public List<T> QueryList<T>(string operation, string sql, SqlArguments arguments, Func<DbDataReader, T> map)
        {
            return Run(operation, delegate (DbConnection connection)
            {
                using (DbCommand command = Build(sql, connection, arguments, CommandType.Text))
                using (DbDataReader reader = command.ExecuteReader())
                {
                    List<T> items = new List<T>();
                    while (reader.Read()) items.Add(map(reader));
                    return items;
                }
            });
        }

        /// <summary>The first row, or null when there is none. I use this for
        /// "give me the resident with id 12" style lookups.</summary>
        public T QuerySingle<T>(string operation, string sql, SqlArguments arguments, Func<DbDataReader, T> map) where T : class
        {
            return Run(operation, delegate (DbConnection connection)
            {
                using (DbCommand command = Build(sql, connection, arguments, CommandType.Text))
                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return map(reader);
                }
            });
        }

        public int ExecuteProcedure(string operation, string procedureName, SqlArguments arguments)
        {
            return Run(operation, delegate (DbConnection connection)
            {
                using (DbCommand command = BuildProcedure(procedureName, connection, arguments))
                {
                    return command.ExecuteNonQuery();
                }
            });
        }

        public DataTable QueryProcedureTable(string operation, string procedureName, SqlArguments arguments)
        {
            return Run(operation, delegate (DbConnection connection)
            {
                using (DbCommand command = BuildProcedure(procedureName, connection, arguments))
                using (DbDataAdapter adapter = CreateAdapter(command))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            });
        }

        /// <summary>
        /// Builds the command that calls a stored procedure.
        ///
        /// The two engines want this written differently and I learned it the
        /// hard way: SQL Server takes the procedure name plus named
        /// parameters, while MySQL's driver is happiest with an ordinary
        /// "CALL name(@a, @b)" statement. So MySQL gets the CALL text and SQL
        /// Server gets the named form, and the caller never has to care.
        ///
        /// The arguments must be added in the same order as the procedure
        /// parameters - that is the one rule to remember when adding a new
        /// procedure to either script.
        /// </summary>
        private DbCommand BuildProcedure(string procedureName, DbConnection connection, SqlArguments arguments)
        {
            if (_provider.Key == "SqlServer")
                return Build(procedureName, connection, arguments, CommandType.StoredProcedure);

            List<string> names = new List<string>();
            if (arguments != null)
            {
                foreach (KeyValuePair<string, object> item in arguments.Items)
                    names.Add(SqlArguments.Normalise(item.Key));
            }

            string call = "CALL " + procedureName + "(" + string.Join(", ", names.ToArray()) + ")";
            return Build(call, connection, arguments, CommandType.Text);
        }

        /// <summary>
        /// Runs several statements as one unit: either all of them happen or
        /// none of them do.
        ///
        /// I use this for the two steps that must never come apart - saving a
        /// request together with its status history, and taking a reference
        /// number together with the row that uses it. If the second step
        /// fails, the first is rolled back and the clerk sees one error
        /// instead of a half-saved record.
        /// </summary>
        public T InTransaction<T>(string operation, Func<DbTransaction, T> work)
        {
            Stopwatch clock = Stopwatch.StartNew();
            try
            {
                using (DbConnection connection = OpenConnection())
                using (DbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        T result = work(transaction);
                        transaction.Commit();
                        CountCommand(clock);
                        return result;
                    }
                    catch
                    {
                        try { transaction.Rollback(); }
                        catch (Exception) { /* the connection is already gone; nothing left to roll back */ }
                        throw;
                    }
                }
            }
            catch (RepositoryException)
            {
                throw;
            }
            catch (Exception error)
            {
                AppLog.Error("Failed while " + operation + ".", error);
                throw RepositoryException.FromDatabase(operation, error);
            }
        }

        /// <summary>Builds a command on an existing connection - the version
        /// the transaction method needs, since all the work in a transaction
        /// must share one connection.</summary>
        public DbCommand BuildOn(DbConnection connection, string sql, SqlArguments arguments, CommandType type)
        {
            return Build(sql, connection, arguments, type);
        }

        // ==================================================================
        //  Asking the server about itself
        // ==================================================================

        /// <summary>True when a query answers at all. The login screen runs
        /// this before it lets anybody type a password, so a database problem
        /// is reported as a database problem.</summary>
        public bool CanConnect()
        {
            try
            {
                object value = ExecuteScalar("checking the database connection", "SELECT 1", null);
                return value != null;
            }
            catch (RepositoryException)
            {
                return false;
            }
        }

        /// <summary>
        /// The names of the stored procedures that exist in the database.
        ///
        /// I added this so the admin can see, on screen, whether the procedure
        /// script was ever run on this machine. That is one of the questions
        /// the panel asked about this project and I would rather answer it with
        /// a list than with a claim.
        /// </summary>
        public IList<string> ListStoredProcedures()
        {
            string sql =
                "SELECT routine_name AS name FROM information_schema.routines " +
                "WHERE routine_schema = DATABASE() AND routine_type = 'PROCEDURE' ORDER BY routine_name";

            if (_provider.Key == "SqlServer")
                sql = "SELECT name FROM sys.objects WHERE type = 'P' AND is_ms_shipped = 0 ORDER BY name";

            List<string> names = new List<string>();
            try
            {
                DataTable table = QueryTable("listing the stored procedures", sql, null);
                foreach (DataRow row in table.Rows)
                    names.Add(Convert.ToString(row["name"]));
            }
            catch (RepositoryException error)
            {
                AppLog.Warn("Could not list stored procedures: " + error.Message);
            }
            return names;
        }

        // ==================================================================
        //  Internals
        // ==================================================================

        private T Run<T>(string operation, Func<DbConnection, T> work)
        {
            Stopwatch clock = Stopwatch.StartNew();
            try
            {
                using (DbConnection connection = OpenConnection())
                {
                    T result = work(connection);
                    CountCommand(clock);
                    return result;
                }
            }
            catch (RepositoryException)
            {
                throw;
            }
            catch (Exception error)
            {
                AppLog.Error("Failed while " + operation + ".", error);
                throw RepositoryException.FromDatabase(operation, error);
            }
        }

        private void CountCommand(Stopwatch clock)
        {
            clock.Stop();
            LastCommandDuration = clock.Elapsed;
            CommandCount++;
        }

        private DbCommand Build(string sql, DbConnection connection, SqlArguments arguments, CommandType type)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            DbCommand command = _provider.CreateCommand(sql, connection);
            command.CommandType = type;

            if (arguments != null)
            {
                foreach (KeyValuePair<string, object> item in arguments.Items)
                    command.Parameters.Add(_provider.CreateParameter(item.Key, item.Value));
            }

            return command;
        }

        /// <summary>Every engine has its own data adapter type, and both are
        /// reachable through the command's own connection, which saves me a
        /// second provider factory that does not exist on some machines.</summary>
        private static DbDataAdapter CreateAdapter(DbCommand command)
        {
            if (command is MySql.Data.MySqlClient.MySqlCommand)
            {
                MySql.Data.MySqlClient.MySqlDataAdapter adapter =
                    new MySql.Data.MySqlClient.MySqlDataAdapter(
                        (MySql.Data.MySqlClient.MySqlCommand)command);
                return adapter;
            }

            if (command is System.Data.SqlClient.SqlCommand)
            {
                System.Data.SqlClient.SqlDataAdapter adapter =
                    new System.Data.SqlClient.SqlDataAdapter(
                        (System.Data.SqlClient.SqlCommand)command);
                return adapter;
            }

            throw new NotSupportedException("I do not know how to read a table from this database engine.");
        }

        /// <summary>
        /// The same connection string with the database name taken out.
        ///
        /// I need it for one job only: creating the database when it does not
        /// exist yet. You cannot connect to a database that is not there.
        /// </summary>
        public static string StripDatabase(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return string.Empty;

            List<string> kept = new List<string>();
            foreach (string piece in connectionString.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(piece)) continue;

                string[] pair = piece.Split('=');
                if (pair.Length == 2)
                {
                    string key = pair[0].Trim();
                    if (string.Equals(key, "Database", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(key, "Initial Catalog", StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                kept.Add(piece.Trim());
            }

            return string.Join(";", kept.ToArray()) + ";";
        }
    }
}
