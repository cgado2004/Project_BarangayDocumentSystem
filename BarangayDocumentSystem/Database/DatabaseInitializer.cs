// =====================================================================
//  PART:    Database - creates the database and tables on first run
//  ORIGIN:  Fdraft - Frent Dhieniel Raborar
//           (create the database, then run the embedded schema.sql)
//  EDITS:   Clint Wood Gado - EnsureColumns, which adds the columns my v3.1
//           request model needs to a database Frent's earlier build created
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using MySql.Data.MySqlClient;
using BarangayDocumentSystem.Interfaces;

namespace BarangayDocumentSystem.Database;

/// <summary>
/// Makes sure the database and its tables exist before the repository
/// touches them, so a group-mate with a fresh XAMPP install needs no manual
/// step at all: start MySQL, start the program.
///
/// The table definitions live in schema.sql, embedded in the .exe - one
/// copy only, so this class and the SQL file can never disagree. That was
/// Frent's design and it is the reason I retired my own hand-run scripts
/// (01-schema.sql and 02-seed-data.sql from v3.1): two schemas for one
/// program is exactly the duplication DRY warns about.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Columns that a database created by the earlier Fdraft build does not
    /// have. CREATE TABLE IF NOT EXISTS leaves an existing table alone, and
    /// MySQL has no ADD COLUMN IF NOT EXISTS, so each one is checked against
    /// information_schema and added only when missing. For a fresh database
    /// the definitions in schema.sql already include all of these; this list
    /// only exists so nobody has to drop a database that already has real
    /// residents in it.
    /// </summary>
    private static readonly (string Column, string Definition)[] RequestColumnUpgrades =
    {
        ("scope",                       "VARCHAR(10)   NOT NULL DEFAULT 'Local'"),
        ("assessed_amount",             "DECIMAL(10,2) NOT NULL DEFAULT 0"),
        ("hours",                       "DECIMAL(6,2)  NOT NULL DEFAULT 0"),
        ("gross_annual_income",         "DECIMAL(14,2) NOT NULL DEFAULT 0"),
        ("detail",                      "VARCHAR(255)  NOT NULL DEFAULT ''"),
        ("apply_jobseeker_waiver",      "TINYINT(1)    NOT NULL DEFAULT 0"),
        ("availed_under_jobseeker_act", "TINYINT(1)    NOT NULL DEFAULT 0"),
    };

    public static void EnsureCreated(string connectionString)
    {
        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            string database = builder.Database;

            if (string.IsNullOrWhiteSpace(database))
                throw new RepositoryException(
                    "The connection string must name a database, e.g. \"Database=barangay_db\".");

            // 1. Connect to the SERVER (no database selected) and create ours
            //    if it is not there yet.
            builder.Database = string.Empty;
            using (var server = new MySqlConnection(builder.ConnectionString))
            {
                server.Open();
                string quoted = "`" + database.Replace("`", "``") + "`";
                Run(server, $"CREATE DATABASE IF NOT EXISTS {quoted} " +
                            "CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
            }

            // 2. Connect to the database, create any missing tables, then
            //    bring an older table up to date.
            using var conn = new MySqlConnection(connectionString);
            conn.Open();
            foreach (string statement in SplitStatements(ReadSchema()))
                Run(conn, statement);

            EnsureColumns(conn);
        }
        catch (MySqlException ex)
        {
            throw new RepositoryException(
                "Could not set up the database: " + ex.Message, ex);
        }
    }

    /// <summary>
    /// Add each upgrade column that the live document_requests table lacks,
    /// and widen fee_basis if it is still the 255 characters of the first
    /// build - my legal-basis sentences run longer than that. Safe to run on
    /// every start-up: a table that is already current changes nothing.
    /// </summary>
    private static void EnsureColumns(DbConnection conn)
    {
        var existing = ExistingColumns(conn, "document_requests");
        if (existing.Count == 0) return;   // no table yet - schema.sql failed and already threw

        foreach (var (column, definition) in RequestColumnUpgrades)
        {
            if (existing.ContainsKey(column)) continue;
            Run(conn, $"ALTER TABLE document_requests ADD COLUMN {column} {definition}");
        }

        if (existing.TryGetValue("fee_basis", out long width) && width < 500)
            Run(conn, "ALTER TABLE document_requests MODIFY fee_basis VARCHAR(500) NOT NULL DEFAULT ''");
    }

    /// <summary>Column name to declared character width (0 for non-text
    /// columns) for one table in the CURRENT database.</summary>
    private static Dictionary<string, long> ExistingColumns(DbConnection conn, string table)
    {
        var columns = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT COLUMN_NAME, COALESCE(CHARACTER_MAXIMUM_LENGTH, 0) " +
            "FROM information_schema.COLUMNS " +
            "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table";

        var p = cmd.CreateParameter();
        p.ParameterName = "@table";
        p.Value = table;
        cmd.Parameters.Add(p);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            columns[reader.GetString(0)] = Convert.ToInt64(reader.GetValue(1));

        return columns;
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

    /// <summary>Drop "-- comment" lines, then split on ';' into statements.
    /// Simple on purpose, which is why schema.sql must not put a semicolon
    /// inside a comment or a string.</summary>
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
