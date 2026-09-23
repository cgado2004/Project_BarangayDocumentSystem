using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Data
{
    public class SqlBarangayRepository : IBarangayRepository
    {
        private readonly string connectionString;
        private SqlConnection transactionConnection;
        private SqlTransaction currentTransaction;

        public SqlBarangayRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("A database connection is required.");
            this.connectionString = connectionString;
        }

        public void Initialize(bool loadSampleData, Action loadSamples)
        {
            SqlDatabase.Initialize(connectionString);
            ExecuteInTransaction(() =>
            {
                using (var command = CreateCommand("SELECT SampleDataInitialized FROM dbo.AppState WITH (UPDLOCK, HOLDLOCK) WHERE Id = 1",
                    transactionConnection, currentTransaction))
                {
                    if ((bool)command.ExecuteScalar()) return;
                    if (loadSampleData && GetResidents().Count == 0 && GetRequests().Count == 0)
                    {
                        if (loadSamples == null) throw new ArgumentNullException(nameof(loadSamples));
                        loadSamples();
                    }
                    command.CommandText = "UPDATE dbo.AppState SET SampleDataInitialized = 1 WHERE Id = 1";
                    command.ExecuteNonQuery();
                }
            });
        }

        public IReadOnlyList<Resident> GetResidents()
        {
            return ReadResidents("SELECT * FROM dbo.Residents ORDER BY ResidentId", null).AsReadOnly();
        }

        public Resident GetResident(int residentId)
        {
            var records = ReadResidents("SELECT * FROM dbo.Residents WHERE ResidentId = @id", residentId);
            if (records.Count == 0) throw new InvalidOperationException("The resident record no longer exists.");
            return records[0];
        }

        private List<Resident> ReadResidents(string sql, int? id)
        {
            return Execute((connection, transaction) =>
            {
                using (var command = CreateCommand(sql, connection, transaction))
                {
                    if (id.HasValue) command.Parameters.Add("@id", SqlDbType.Int).Value = id.Value;
                    var records = new List<Resident>();
                    using (var reader = command.ExecuteReader())
                        while (reader.Read()) records.Add(SqlResidentMapping.Read(reader));
                    return records;
                }
            });
        }

        public void SaveResident(Resident resident)
        {
            if (resident == null) throw new ArgumentNullException(nameof(resident));
            Execute((connection, transaction) =>
            {
                bool adding = resident.ResidentId == 0;
                string sql = adding
                    ? "INSERT INTO dbo.Residents (" + SqlResidentMapping.Columns + ") OUTPUT INSERTED.ResidentId VALUES (" + SqlResidentMapping.Parameters + ")"
                    : "UPDATE dbo.Residents SET " + SqlResidentMapping.Assignments + ", Version = Version + 1 " +
                        "OUTPUT INSERTED.Version WHERE ResidentId = @id AND Version = @version";
                using (var command = CreateCommand(sql, connection, transaction))
                {
                    SqlResidentMapping.AddParameters(command, resident);
                    command.Parameters.Add("@id", SqlDbType.Int).Value = resident.ResidentId;
                    command.Parameters.Add("@version", SqlDbType.Int).Value = resident.Version;
                    object result = command.ExecuteScalar();
                    if (result == null) throw new InvalidOperationException("This resident was changed or deleted by another action. Refresh and reopen the record.");
                    if (adding) resident.ResidentId = (int)result;
                    resident.Version = adding ? 1 : (int)result;
                    return 0;
                }
            });
        }

        public void DeleteResident(int residentId)
        {
            Execute((connection, transaction) =>
            {
                using (var command = CreateCommand("DELETE FROM dbo.Residents WHERE ResidentId = @id", connection, transaction))
                {
                    command.Parameters.Add("@id", SqlDbType.Int).Value = residentId;
                    if (command.ExecuteNonQuery() == 0) throw new InvalidOperationException("The resident record no longer exists.");
                    return 0;
                }
            });
        }

        public IReadOnlyList<DocumentRequest> GetRequests()
        {
            return ReadRequests(null).AsReadOnly();
        }

        public DocumentRequest GetRequest(int requestId)
        {
            var records = ReadRequests(requestId);
            if (records.Count == 0) throw new InvalidOperationException("The document request no longer exists.");
            return records[0];
        }

        private List<DocumentRequest> ReadRequests(int? id)
        {
            return Execute((connection, transaction) =>
            {
                string snapshotColumns = "s." + SqlResidentMapping.Columns.Replace(", ", ", s.");
                string sql = "SELECT r.*, " + snapshotColumns + " FROM dbo.DocumentRequests r " +
                    "INNER JOIN dbo.RequestResidentSnapshots s ON s.RequestId = r.RequestId" +
                    (id.HasValue ? " WHERE r.RequestId = @id" : " ORDER BY r.RequestId");
                using (var command = CreateCommand(sql, connection, transaction))
                {
                    if (id.HasValue) command.Parameters.Add("@id", SqlDbType.Int).Value = id.Value;
                    var records = new List<DocumentRequest>();
                    using (var reader = command.ExecuteReader())
                        while (reader.Read()) records.Add(SqlRequestMapping.Read(reader));
                    return records;
                }
            });
        }

        public void SaveRequest(DocumentRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            // Saves the request and snapshot together.
            ExecuteInTransaction(() =>
            {
                bool adding = request.RequestId == 0;
                string sql = adding
                    ? "INSERT INTO dbo.DocumentRequests (" + SqlRequestMapping.Columns + ") OUTPUT INSERTED.RequestId VALUES (" + SqlRequestMapping.Parameters + ")"
                    : "UPDATE dbo.DocumentRequests SET " + SqlRequestMapping.Assignments + ", Version = Version + 1 " +
                        "OUTPUT INSERTED.Version WHERE RequestId = @id AND Version = @version";
                using (var command = CreateCommand(sql, transactionConnection, currentTransaction))
                {
                    SqlRequestMapping.AddParameters(command, request);
                    command.Parameters.Add("@id", SqlDbType.Int).Value = request.RequestId;
                    command.Parameters.Add("@version", SqlDbType.Int).Value = request.Version;
                    object result = command.ExecuteScalar();
                    if (result == null) throw new InvalidOperationException("This request was changed or deleted by another action. Refresh and try again.");
                    if (adding) request.RequestId = (int)result;
                    request.Version = adding ? 1 : (int)result;
                }
                if (adding)
                {
                    using (var command = CreateCommand("INSERT INTO dbo.RequestResidentSnapshots (RequestId, " +
                        SqlResidentMapping.Columns + ") VALUES (@requestId, " + SqlResidentMapping.Parameters + ")",
                        transactionConnection, currentTransaction))
                    {
                        command.Parameters.Add("@requestId", SqlDbType.Int).Value = request.RequestId;
                        SqlResidentMapping.AddParameters(command, request.ResidentSnapshot);
                        command.ExecuteNonQuery();
                    }
                }
            });
        }

        public void ExecuteInTransaction(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (currentTransaction != null)
            {
                action();
                return;
            }
            Execute((connection, unused) =>
            {
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    transactionConnection = connection;
                    currentTransaction = transaction;
                    try
                    {
                        action();
                        transaction.Commit();
                    }
                    finally
                    {
                        currentTransaction = null;
                        transactionConnection = null;
                    }
                    // Undoes changes if saving fails.
                }
                return 0;
            });
        }

        private T Execute<T>(Func<SqlConnection, SqlTransaction, T> action)
        {
            try
            {
                if (currentTransaction != null) return action(transactionConnection, currentTransaction);
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    return action(connection, null);
                }
            }
            catch (SqlException error) when (error.Number == 2601 || error.Number == 2627)
            {
                if (!error.Message.Contains("UX_Requests_Receipt") && !error.Message.Contains("UX_Requests_Jobseeker")) throw;
                string message = error.Message.Contains("UX_Requests_Receipt")
                    ? "That official receipt number is already used by another request."
                    : "This resident already has an active or released first-time jobseeker request.";
                throw new InvalidOperationException(message, error);
            }
            catch (SqlException error) when (error.Number == 547)
            {
                throw new InvalidOperationException("This change conflicts with saved records. Residents with request history cannot be deleted; request and payment details must remain valid.", error);
            }
        }

        private static SqlCommand CreateCommand(string sql, SqlConnection connection, SqlTransaction transaction)
        {
            return new SqlCommand(sql, connection, transaction);
        }
    }
}
