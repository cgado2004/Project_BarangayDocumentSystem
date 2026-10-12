// ---------------------------------------------------------------------------
//  DbProviders.cs - the MySQL and SQL Server connection makers.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Data;
using System.Data.Common;
using BarangayDocumentSystem.Interfaces;
using MySql.Data.MySqlClient;

namespace BarangayDocumentSystem.Data
{
    /// <summary>
    /// The MySQL connection maker.
    ///
    /// I talk to MySQL through the driver in the one NuGet package this
    /// project needs (MySql.Data). Everything above this class only ever sees
    /// the standard DbConnection, DbCommand and DbParameter types from the
    /// framework, so no screen and no service knows which engine is under it.
    /// </summary>
    public class MySqlDbProvider : IDbProvider
    {
        public string Key { get { return "MySql"; } }

        public string DisplayName { get { return "MySQL / MariaDB"; } }

        public DbConnection CreateConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }

        public DbCommand CreateCommand(string commandText, DbConnection connection)
        {
            MySqlCommand command = new MySqlCommand(commandText, (MySqlConnection)connection);
            command.CommandTimeout = 60;
            return command;
        }

        public DbParameter CreateParameter(string name, object value)
        {
            MySqlParameter parameter = new MySqlParameter(SqlArguments.Normalise(name), value ?? DBNull.Value);
            if (parameter.Value == DBNull.Value) parameter.Value = DBNull.Value;
            return parameter;
        }

        public string LastInsertIdStatement { get { return "SELECT LAST_INSERT_ID();"; } }

        public bool SupportsDatabaseCreation { get { return true; } }

        public string CurrentTimestampExpression { get { return "NOW()"; } }

        /// <summary>
        /// MySQL has no "create database if missing" that can be combined with
        /// a connection to that same database, so the initializer calls this
        /// while connected to the server with no database selected, then
        /// reconnects. The collation is the one that stores Filipino names
        /// (Peña, Cañete) correctly.
        /// </summary>
        public string CreateDatabaseStatement(string databaseName)
        {
            return "CREATE DATABASE IF NOT EXISTS `" + databaseName + "` "
                 + "CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
        }
    }

    /// <summary>
    /// The SQL Server connection maker.
    ///
    /// This one needs no package at all: System.Data.SqlClient ships with the
    /// .NET Framework, which is the main reason the barangay can run this
    /// system on a machine where only SQL Server Express (or LocalDB) is
    /// installed and nobody wants to install anything else.
    /// </summary>
    public class SqlServerDbProvider : IDbProvider
    {
        public string Key { get { return "SqlServer"; } }

        public string DisplayName { get { return "Microsoft SQL Server"; } }

        public DbConnection CreateConnection(string connectionString)
        {
            return new System.Data.SqlClient.SqlConnection(connectionString);
        }

        public DbCommand CreateCommand(string commandText, DbConnection connection)
        {
            System.Data.SqlClient.SqlCommand command =
                new System.Data.SqlClient.SqlCommand(commandText,
                    (System.Data.SqlClient.SqlConnection)connection);
            command.CommandTimeout = 60;
            return command;
        }

        public DbParameter CreateParameter(string name, object value)
        {
            return new System.Data.SqlClient.SqlParameter(SqlArguments.Normalise(name), value ?? DBNull.Value);
        }

        public string LastInsertIdStatement { get { return "SELECT CAST(SCOPE_IDENTITY() AS INT);"; } }

        /// <summary>SQL Server can check and create in one batch, but creating
        /// a database still needs its own connection, so the initializer does
        /// the same two steps as it does for MySQL.</summary>
        public bool SupportsDatabaseCreation { get { return true; } }

        public string CurrentTimestampExpression { get { return "GETDATE()"; } }

        public string CreateDatabaseStatement(string databaseName)
        {
            return "IF DB_ID('" + databaseName + "') IS NULL CREATE DATABASE [" + databaseName + "];";
        }
    }

    /// <summary>
    /// The demo store. It has no server, so it cannot create a database; the
    /// methods that need a connection are never reached because the in-memory
    /// repository does not use them at all. I still implement them, so the
    /// program cannot crash on an unexpected call.
    /// </summary>
    public class InMemoryDbProvider : IDbProvider
    {
        public string Key { get { return "Memory"; } }

        public string DisplayName { get { return "In-memory sample data (nothing is saved)"; } }

        public DbConnection CreateConnection(string connectionString)
        {
            throw new NotSupportedException(
                "The in-memory store keeps its data in a list, not in a database, so there is no connection to open.");
        }

        public DbCommand CreateCommand(string commandText, DbConnection connection)
        {
            throw new NotSupportedException("The in-memory store does not run SQL.");
        }

        public DbParameter CreateParameter(string name, object value)
        {
            throw new NotSupportedException("The in-memory store does not use parameters.");
        }

        public string LastInsertIdStatement
        {
            get { throw new NotSupportedException("The in-memory store hands out its own ids."); }
        }

        public bool SupportsDatabaseCreation { get { return false; } }

        public string CurrentTimestampExpression { get { return "CURRENT_TIMESTAMP"; } }

        public string CreateDatabaseStatement(string databaseName)
        {
            throw new NotSupportedException("The in-memory store has no database to create.");
        }
    }

    /// <summary>
    /// Decides which provider to build from the setting in App.config.
    ///
    /// This is the only place in the program that reads the "Storage" setting
    /// and turns it into an object, which is exactly how I want it: if I add
    /// SQLite tomorrow, this method gains two lines and nothing else changes.
    /// </summary>
    public static class DbProviderFactory
    {
        public static IDbProvider Create(string providerKey)
        {
            if (string.IsNullOrWhiteSpace(providerKey)) return new InMemoryDbProvider();

            switch (providerKey.Trim().ToLowerInvariant())
            {
                case "mysql":
                case "mariadb":
                    return new MySqlDbProvider();

                case "sqlserver":
                case "mssql":
                    return new SqlServerDbProvider();

                default:
                    return new InMemoryDbProvider();
            }
        }
    }
}
