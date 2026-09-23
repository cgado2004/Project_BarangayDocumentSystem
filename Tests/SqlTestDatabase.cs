using System;
using System.Data.SqlClient;
using BarangayDocumentSystem.Data;

namespace BarangayDocumentSystem.Tests
{
    internal sealed class SqlTestDatabase : IDisposable
    {
        private readonly string databaseName = "BarangayTests_" + Guid.NewGuid().ToString("N");
        private readonly string connectionString;

        public SqlTestDatabase()
        {
            connectionString = new SqlConnectionStringBuilder
            {
                DataSource = @"(LocalDB)\MSSQLLocalDB",
                InitialCatalog = databaseName,
                IntegratedSecurity = true,
                ConnectTimeout = 30
            }.ConnectionString;
            try { SqlDatabase.Initialize(connectionString); }
            catch { Dispose(); throw; }
        }

        public SqlBarangayRepository OpenRepository()
        {
            return new SqlBarangayRepository(connectionString);
        }

        public void Dispose()
        {
            // Removes only this test database.
            SqlConnection.ClearAllPools();
            var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
            using (var connection = new SqlConnection(builder.ConnectionString))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "IF DB_ID(@name) IS NOT NULL BEGIN " +
                    "ALTER DATABASE [" + databaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                    "DROP DATABASE [" + databaseName + "]; END";
                command.Parameters.AddWithValue("@name", databaseName);
                command.ExecuteNonQuery();
            }
        }

        public void ExpectConstraint(string sql, int id)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = sql;
                command.Parameters.AddWithValue("@id", id);
                try { command.ExecuteNonQuery(); }
                catch (SqlException error) when (error.Number == 2601 || error.Number == 2627 || error.Number == 547) { return; }
                throw new Exception("Expected SQL Server to reject conflicting data.");
            }
        }
    }
}
