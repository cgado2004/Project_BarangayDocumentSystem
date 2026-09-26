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
            foreach (string statement in SplitStatements(ReadSchema()))
                Run(conn, statement);
        }
        catch (MySqlException ex)
        {
            throw new RepositoryException(
                "Could not set up the database: " + ex.Message, ex);
        }
    }

    private static void Run(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string ReadSchema()
    {
        var assembly = Assembly.GetExecutingAssembly();
        string name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("schema.sql", StringComparison.OrdinalIgnoreCase))
            ?? throw new RepositoryException("schema.sql is missing from the application.");

        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
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
