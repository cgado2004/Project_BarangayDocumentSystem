using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data.Common;
using System.Reflection;
using MySql.Data.MySqlClient;

using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Database;





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


            builder.Database = string.Empty;
            using (var server = new MySqlConnection(builder.ConnectionString))
            {
                server.Open();
                string quoted = "`" + database.Replace("`", "``") + "`";
                Run(server, $"CREATE DATABASE IF NOT EXISTS {quoted} " +
                            "CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
            }


            using var conn = new MySqlConnection(connectionString);
            conn.Open();
            MySqlBarangayRepository.NormalizeSession(conn);
            foreach (string statement in SplitStatements(ReadResource("schema.sql")))
                Run(conn, statement);



            EnsureColumn(conn, database, "residents", "is_archived", "TINYINT(1) NOT NULL DEFAULT 0");
            EnsureColumn(conn, database, "residents", "archived_on", "DATETIME NULL");
            EnsureColumn(conn, database, "residents", "transfer_destination", "VARCHAR(255) NOT NULL DEFAULT ''");
            EnsureColumn(conn, database, "residents", "transfer_reason", "VARCHAR(255) NOT NULL DEFAULT ''");
            EnsureColumn(conn, database, "residents", "house_number", "VARCHAR(30) NOT NULL DEFAULT ''");
            EnsureColumn(conn, database, "document_requests", "date_paid", "DATETIME NULL");
            EnsureColumn(conn, database, "document_requests", "received_by", "VARCHAR(100) NOT NULL DEFAULT ''");
            EnsureColumn(conn, database, "users", "role", "VARCHAR(20) NOT NULL DEFAULT 'Staff'");
            EnsureColumn(conn, database, "users", "is_archived", "TINYINT(1) NOT NULL DEFAULT 0");


            Run(conn, "UPDATE users SET role = 'Admin' " +
                      "WHERE user_id = (SELECT m FROM (SELECT MIN(user_id) AS m FROM users) AS t) " +
                      "AND NOT EXISTS (SELECT 1 FROM (SELECT role FROM users) AS r " +
                      "WHERE role COLLATE utf8mb4_unicode_ci = 'Admin' COLLATE utf8mb4_unicode_ci)");
            Run(conn, "UPDATE residents SET house_number = LEFT(address_line, 30) " +
                      "WHERE CHAR_LENGTH(house_number) = 0 AND CHAR_LENGTH(address_line) > 0");



            foreach (string statement in SplitProcedures(ReadResource("procedures.sql")))
                Run(conn, statement);
        }
        catch (MySqlException ex)
        {
            throw new RepositoryException(
                "Could not set up the database: " + ex.Message, ex);
        }
    }

    public static void EnsureDefaultAdmin(string connectionString)
    {
        var users = new MySqlUserRepository(connectionString);
        var existing = users.Find(DefaultAdmin.Username);
        if (existing is null)
        {
            users.Create(DefaultAdmin.Username, DefaultAdmin.FullName,
                PasswordHasher.Hash(DefaultAdmin.Password), UserRole.Admin);
            return;
        }

        if (existing.Role != UserRole.Admin || existing.IsArchived
            || existing.FullName != DefaultAdmin.FullName)
        {
            users.Update(existing.UserId, existing.Username, DefaultAdmin.FullName, UserRole.Admin);
            if (existing.IsArchived) users.SetArchived(existing.UserId, false);
        }
    }


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









































































































































public static class DefaultAdmin
{
    public const string Username = "Trinity";
    public const string FullName = "Joene Trinidad";
    public const string Password = "Password123456";
}
