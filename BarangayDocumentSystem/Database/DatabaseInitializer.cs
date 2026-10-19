using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data.Common;
using System.Reflection;
using MySql.Data.MySqlClient;

using BarangayDocumentSystem.Interfaces;

namespace BarangayDocumentSystem.Database;


/// The table definitions live in schema.sql (embedded in the .exe) — one copy
/// only, so this class and the SQL file can never disagree.

public static class DatabaseInitializer
{
    public static void EnsureCreated(string connectionString)
    {
        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            string database = builder.Database;

            if (string.IsNullOrWhiteSpace(database))
                throw new RepositoryException(
                    "The connection string must name a database, e.g. \"Database=barangay_db\".");

            // 1. Connect to the SERVER (no database selected) and create ours if needed.
            builder.Database = string.Empty;
            using (var server = new MySqlConnection(builder.ConnectionString))
            {
                server.Open();
                string quoted = "`" + database.Replace("`", "``") + "`";
                Run(server, $"CREATE DATABASE IF NOT EXISTS {quoted} " +
                            "CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
            }

            // 2. Connect to the database and create any missing tables.
            using var conn = new MySqlConnection(connectionString);
            conn.Open();
            foreach (string statement in SplitStatements(ReadResource("schema.sql")))
                Run(conn, statement);

            // 3. Databases created by an older version already have a residents
            //    table, and CREATE TABLE IF NOT EXISTS will not alter it.
            EnsureColumn(conn, database, "residents", "is_archived", "TINYINT(1) NOT NULL DEFAULT 0");
            EnsureColumn(conn, database, "residents", "archived_on", "DATETIME NULL");
            EnsureColumn(conn, database, "residents", "transfer_destination", "VARCHAR(255) NOT NULL DEFAULT ''");
            EnsureColumn(conn, database, "residents", "transfer_reason", "VARCHAR(255) NOT NULL DEFAULT ''");

            // 4. (Re)create every stored procedure. They are dropped and created
            //    again on each start, so editing procedures.sql is enough to update them.
            foreach (string statement in SplitProcedures(ReadResource("procedures.sql")))
                Run(conn, statement);
        }
        catch (MySqlException ex)
        {
            throw new RepositoryException(
                "Could not set up the database: " + ex.Message, ex);
        }
    }

    /// Adds a column only when it is missing (MySQL has no ADD COLUMN IF NOT EXISTS).
    private static void EnsureColumn(MySqlConnection conn, string database,
                                     string table, string column, string definition)
    {
        using (var check = new MySqlCommand(
            "SELECT COUNT(*) FROM information_schema.COLUMNS " +
            "WHERE TABLE_SCHEMA=@db AND TABLE_NAME=@t AND COLUMN_NAME=@c", conn))
        {
            check.Parameters.AddWithValue("@db", database);
            check.Parameters.AddWithValue("@t", table);
            check.Parameters.AddWithValue("@c", column);
            if (Convert.ToInt32(check.ExecuteScalar()) > 0) return;
        }

        Run(conn, $"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition}");
    }

    private static void Run(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string ReadResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new RepositoryException(fileName + " is missing from the application.");

        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// Procedure bodies contain semicolons, so procedures.sql is split on lines
    /// that hold only "$$" instead. "-- comment" lines are dropped first.
    private static IEnumerable<string> SplitProcedures(string script)
    {
        var lines = script.Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !l.TrimStart().StartsWith("--", StringComparison.Ordinal));

        var statement = new System.Text.StringBuilder();
        foreach (string line in lines)
        {
            if (line.Trim() == "$$")
            {
                if (statement.ToString().Trim().Length > 0)
                    yield return statement.ToString().Trim();
                statement.Clear();
            }
            else
            {
                statement.AppendLine(line);
            }
        }

        if (statement.ToString().Trim().Length > 0)
            yield return statement.ToString().Trim();
    }

    /// Drops "-- comment" lines, then splits on ';' into statements.
    private static IEnumerable<string> SplitStatements(string script)
    {
        var lines = script.Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !l.TrimStart().StartsWith("--", StringComparison.Ordinal));

        return string.Join("\n", lines)
            .Split(';')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0);
    }
}
