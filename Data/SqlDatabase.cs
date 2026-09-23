using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BarangayDocumentSystem.Data
{
    public static class SqlDatabase
    {
        public static void Initialize(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            string databaseName = builder.InitialCatalog;
            if (!Regex.IsMatch(databaseName, @"^[A-Za-z][A-Za-z0-9_]{0,127}$") ||
                string.Equals(databaseName, "master", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(databaseName, "model", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(databaseName, "msdb", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(databaseName, "tempdb", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Use a separate application database name containing letters, numbers, and underscores.");
            if (!string.IsNullOrEmpty(builder.AttachDBFilename))
                throw new ArgumentException("Use Initial Catalog instead of attaching a machine-specific database file.");

            builder.InitialCatalog = "master";
            using (var connection = new SqlConnection(builder.ConnectionString))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "IF DB_ID(@name) IS NULL BEGIN " +
                    "DECLARE @sql nvarchar(300) = N'CREATE DATABASE ' + QUOTENAME(@name); EXEC(@sql); END";
                command.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = databaseName;
                try { command.ExecuteNonQuery(); }
                catch (SqlException error) when (error.Number == 1801)
                {
                    // Another app already created the database.
                }
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                using (var command = new SqlCommand("DECLARE @result int; " +
                    "EXEC @result = sys.sp_getapplock @Resource=N'BarangaySchema', @LockMode='Exclusive', @LockOwner='Transaction'; " +
                    "IF @result < 0 THROW 50001, 'Could not lock database setup.', 1;", connection, transaction))
                {
                    command.ExecuteNonQuery();
                    command.CommandText = "SELECT OBJECT_ID(N'dbo.AppState', N'U')";
                    if (command.ExecuteScalar() == DBNull.Value)
                    {
                        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BarangayDocumentSystem.Data.Schema.sql"))
                        using (var reader = new StreamReader(stream))
                            command.CommandText = reader.ReadToEnd();
                        command.ExecuteNonQuery();
                    }
                    command.CommandText = "SELECT SchemaVersion FROM dbo.AppState WHERE Id = 1";
                    if (Convert.ToInt32(command.ExecuteScalar()) != 1)
                        throw new InvalidOperationException("This database uses a different schema version. Use the matching application version.");
                    transaction.Commit();
                }
            }
        }
    }
}
