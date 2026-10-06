// ---------------------------------------------------------------------------
//  IDbProvider.cs - the small set of differences between MySQL and SQL Server.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System.Data;
using System.Data.Common;

namespace BarangayDocumentSystem.Interfaces
{
    /// <summary>
    /// MySQL and SQL Server do the same job but disagree about a few small
    /// things: how you ask for the id of the row you just inserted, whether
    /// you write identifiers in backticks or square brackets, and how a
    /// boolean is stored.
    ///
    /// Instead of writing the whole repository twice - which is two places to
    /// fix every bug - I put those differences behind this interface and wrote
    /// the repository once. Adding a third engine later means writing one small
    /// class, not another repository.
    /// </summary>
    public interface IDbProvider
    {
        /// <summary>"MySql" or "SqlServer" - the value in App.config.</summary>
        string Key { get; }

        /// <summary>A name a person can read, for the status bar and the
        /// error messages.</summary>
        string DisplayName { get; }

        /// <summary>Opens a connection. The caller disposes it.</summary>
        DbConnection CreateConnection(string connectionString);

        DbCommand CreateCommand(string commandText, DbConnection connection);

        DbParameter CreateParameter(string name, object value);

        /// <summary>The statement that returns the id of the row I just
        /// inserted: LAST_INSERT_ID() on MySQL, SCOPE_IDENTITY() on SQL Server.</summary>
        string LastInsertIdStatement { get; }

        /// <summary>The bit of SQL that creates the database when it is
        /// missing. SQL Server can do it in one statement; MySQL needs the
        /// statement that creates the database separately, which is why the
        /// initializer runs this before anything else.</summary>
        string CreateDatabaseStatement(string databaseName);

        /// <summary>True when the server lets me run the "create the database
        /// if it is not there yet" step. The in-memory store says no.</summary>
        bool SupportsDatabaseCreation { get; }

        /// <summary>How this engine writes "the current date and time" - used
        /// only in the stored procedure scripts, never in C# code.</summary>
        string CurrentTimestampExpression { get; }
    }
}
