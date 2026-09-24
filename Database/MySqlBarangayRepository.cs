using System;
using System.Collections.Generic;
using System.Data;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using MySql.Data.MySqlClient;

namespace BarangayDocumentSystem.Database
{
    /// <summary>
    /// The MySQL store — the revamp's seam, and a deliberate merger of two
    /// teammates' work.
    ///
    /// From Jonathan F. Del Rosario's Draft branch comes every SEMANTIC: the
    /// IBarangayRepository contract, detached copies on every read, the
    /// Version column with its stale-edit rejection, the resident snapshot
    /// written with the request in one transaction, the AppState seeding
    /// flag, and the exact user-facing error messages. His repository spoke
    /// T-SQL; this file translates those statements to MySQL.
    ///
    /// From Frent Raborar's Draft2 branch comes the PLUMBING around it: the
    /// MySql.Data driver, the connection-string settings, the schema
    /// initializer that creates the database and runs the embedded
    /// schema.sql, and the database itself (barangay_db on XAMPP).
    ///
    /// Column names are snake_case (Frent's dialect); enum values are stored
    /// as INTs (Jonathan's mapping); unpaid receipts are NULL in the table
    /// and "" in the model, which is what lets ux_requests_receipt enforce
    /// one-receipt-per-payment without MySQL's missing filtered indexes.
    /// </summary>
    public class MySqlBarangayRepository : IBarangayRepository
    {
        private readonly string connectionString;
        private MySqlConnection transactionConnection;
        private MySqlTransaction currentTransaction;

        public MySqlBarangayRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A database connection is required.");
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Creates the database, its tables and the Citizen's Charter fee
        /// schedule (Frent's initializer running the embedded schema.sql).
        /// Sample seeding is gone by design: the production database starts
        /// empty. Schema statements are idempotent, so this is safe to run
        /// on every start and tops up older installs.
        /// </summary>
        public void Initialize()
        {
            DatabaseInitializer.EnsureCreated(connectionString);
        }

        public IReadOnlyList<Resident> GetResidents()
        {
            return ReadResidents("SELECT * FROM residents ORDER BY resident_id", null).AsReadOnly();
        }

        public Resident GetResident(int residentId)
        {
            var records = ReadResidents("SELECT * FROM residents WHERE resident_id = @id", residentId);
            if (records.Count == 0) throw new InvalidOperationException("The resident record no longer exists.");
            return records[0];
        }

        private List<Resident> ReadResidents(string sql, int? id)
        {
            return Execute((connection, transaction) =>
            {
                using (var command = CreateCommand(sql, connection, transaction))
                {
                    if (id.HasValue) command.Parameters.Add("@id", MySqlDbType.Int32).Value = id.Value;
                    var records = new List<Resident>();
                    using (var reader = command.ExecuteReader())
                        while (reader.Read()) records.Add(ReadResident(reader));
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
                    ? "INSERT INTO residents (" + ResidentColumns + ") VALUES (" + ResidentParameters + ")"
                    : "UPDATE residents SET " + ResidentAssignments + ", version = version + 1 " +
                      "WHERE resident_id = @id AND version = @version";
                using (var command = CreateCommand(sql, connection, transaction))
                {
                    AddResidentParameters(command, resident);
                    if (!adding)
                    {
                        command.Parameters.Add("@id", MySqlDbType.Int32).Value = resident.ResidentId;
                        command.Parameters.Add("@version", MySqlDbType.Int32).Value = resident.Version;
                    }
                    if (adding)
                    {
                        command.ExecuteNonQuery();
                        resident.ResidentId = (int)command.LastInsertedId;
                        resident.Version = 1;
                    }
                    else
                    {
                        // Zero rows means another action changed or deleted the record.
                        if (command.ExecuteNonQuery() == 0)
                            throw new InvalidOperationException(
                                "This resident was changed or deleted by another action. Refresh and reopen the record.");
                        resident.Version++;
                    }
                    return 0;
                }
            });
        }

        public void DeleteResident(int residentId)
        {
            Execute((connection, transaction) =>
            {
                using (var command = CreateCommand(
                    "DELETE FROM residents WHERE resident_id = @id", connection, transaction))
                {
                    command.Parameters.Add("@id", MySqlDbType.Int32).Value = residentId;
                    if (command.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException("The resident record no longer exists.");
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
                string snapshotColumns = "s." + ResidentColumns.Replace(", ", ", s.");
                string sql = "SELECT r.*, " + snapshotColumns + " FROM document_requests r " +
                    "INNER JOIN resident_snapshots s ON s.request_id = r.request_id" +
                    (id.HasValue ? " WHERE r.request_id = @id" : " ORDER BY r.request_id");
                using (var command = CreateCommand(sql, connection, transaction))
                {
                    if (id.HasValue) command.Parameters.Add("@id", MySqlDbType.Int32).Value = id.Value;
                    var records = new List<DocumentRequest>();
                    using (var reader = command.ExecuteReader())
                        while (reader.Read()) records.Add(ReadRequest(reader));
                    return records;
                }
            });
        }

        public void SaveRequest(DocumentRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            // Saves the request and its resident snapshot together.
            ExecuteInTransaction(() =>
            {
                bool adding = request.RequestId == 0;
                string sql = adding
                    ? "INSERT INTO document_requests (" + RequestColumns + ") VALUES (" + RequestParameters + ")"
                    : "UPDATE document_requests SET " + RequestAssignments + ", version = version + 1 " +
                      "WHERE request_id = @id AND version = @version";
                using (var command = CreateCommand(sql, transactionConnection, currentTransaction))
                {
                    AddRequestParameters(command, request);
                    if (!adding)
                    {
                        command.Parameters.Add("@id", MySqlDbType.Int32).Value = request.RequestId;
                        command.Parameters.Add("@version", MySqlDbType.Int32).Value = request.Version;
                    }
                    if (adding)
                    {
                        command.ExecuteNonQuery();
                        request.RequestId = (int)command.LastInsertedId;
                        request.Version = 1;
                    }
                    else
                    {
                        if (command.ExecuteNonQuery() == 0)
                            throw new InvalidOperationException(
                                "This request was changed or deleted by another action. Refresh and try again.");
                        request.Version++;
                    }
                }
                if (adding)
                {
                    using (var command = CreateCommand(
                        "INSERT INTO resident_snapshots (request_id, " + ResidentColumns + ") " +
                        "VALUES (@requestId, " + ResidentParameters + ")",
                        transactionConnection, currentTransaction))
                    {
                        command.Parameters.Add("@requestId", MySqlDbType.Int32).Value = request.RequestId;
                        AddResidentParameters(command, request.ResidentSnapshot);
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
                }
                return 0;
            });
        }

        // ------------------------------------------------------------------
        //  Connection plumbing and the database's side of the rules
        // ------------------------------------------------------------------

        private T Execute<T>(Func<MySqlConnection, MySqlTransaction, T> action)
        {
            try
            {
                if (currentTransaction != null) return action(transactionConnection, currentTransaction);
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    return action(connection, null);
                }
            }
            catch (MySqlException error) when (error.Number == 1062)
            {
                // Duplicate key: whichever unique index refused tells us which rule.
                string text = error.Message ?? "";
                if (text.Contains("ux_requests_receipt"))
                    throw new InvalidOperationException(
                        "That official receipt number is already used by another request.", error);
                if (text.Contains("ux_requests_jobseeker"))
                    throw new InvalidOperationException(
                        "This resident already has an active or released first-time jobseeker request.", error);
                throw;
            }
            catch (MySqlException error) when (error.Number == 1451 || error.Number == 1452)
            {
                throw new InvalidOperationException(
                    "This change conflicts with saved records. Residents with request history cannot be deleted; " +
                    "request and payment details must remain valid.", error);
            }
        }

        private static MySqlCommand CreateCommand(string sql, MySqlConnection connection, MySqlTransaction transaction)
        {
            return new MySqlCommand(sql, connection, transaction);
        }

        // ------------------------------------------------------------------
        //  Resident mapping — Jonathan's columns in Frent's naming
        // ------------------------------------------------------------------

        private const string ResidentColumns =
            "first_name, middle_name, last_name, " +
            "suffix, date_of_birth, gender, " +
            "civil_status, purok, address, " +
            "contact_number, occupation, date_of_residency, " +
            "is_registered_voter, is_senior_citizen, is_person_with_disability, " +
            "is_indigent, is_student, is_solo_parent, " +
            "has_used_jobseeker_benefit";
        private const string ResidentParameters =
            "@first_name, @middle_name, @last_name, " +
            "@suffix, @date_of_birth, @gender, " +
            "@civil_status, @purok, @address, " +
            "@contact_number, @occupation, @date_of_residency, " +
            "@is_registered_voter, @is_senior_citizen, @is_person_with_disability, " +
            "@is_indigent, @is_student, @is_solo_parent, " +
            "@has_used_jobseeker_benefit";
        private const string ResidentAssignments =
            "first_name = @first_name, middle_name = @middle_name, last_name = @last_name, " +
            "suffix = @suffix, date_of_birth = @date_of_birth, gender = @gender, " +
            "civil_status = @civil_status, purok = @purok, address = @address, " +
            "contact_number = @contact_number, occupation = @occupation, date_of_residency = @date_of_residency, " +
            "is_registered_voter = @is_registered_voter, is_senior_citizen = @is_senior_citizen, " +
            "is_person_with_disability = @is_person_with_disability, is_indigent = @is_indigent, " +
            "is_student = @is_student, is_solo_parent = @is_solo_parent, " +
            "has_used_jobseeker_benefit = @has_used_jobseeker_benefit";

        private static void AddResidentParameters(MySqlCommand command, Resident record)
        {
            command.Parameters.Add("@first_name", MySqlDbType.VarChar, 80).Value = record.FirstName ?? "";
            command.Parameters.Add("@middle_name", MySqlDbType.VarChar, 80).Value = record.MiddleName ?? "";
            command.Parameters.Add("@last_name", MySqlDbType.VarChar, 80).Value = record.LastName ?? "";
            command.Parameters.Add("@suffix", MySqlDbType.VarChar, 20).Value = record.Suffix ?? "";
            command.Parameters.Add("@date_of_birth", MySqlDbType.Date).Value = record.DateOfBirth;
            command.Parameters.Add("@gender", MySqlDbType.Int32).Value = (int)record.Gender;
            command.Parameters.Add("@civil_status", MySqlDbType.Int32).Value = (int)record.CivilStatus;
            command.Parameters.Add("@purok", MySqlDbType.VarChar, 60).Value = record.Purok ?? "";
            command.Parameters.Add("@address", MySqlDbType.VarChar, 250).Value = record.Address ?? "";
            command.Parameters.Add("@contact_number", MySqlDbType.VarChar, 15).Value = record.ContactNumber ?? "";
            command.Parameters.Add("@occupation", MySqlDbType.VarChar, 100).Value = record.Occupation ?? "";
            command.Parameters.Add("@date_of_residency", MySqlDbType.Date).Value = record.DateOfResidency;
            command.Parameters.Add("@is_registered_voter", MySqlDbType.Byte).Value = record.IsRegisteredVoter;
            command.Parameters.Add("@is_senior_citizen", MySqlDbType.Byte).Value = record.IsSeniorCitizen;
            command.Parameters.Add("@is_person_with_disability", MySqlDbType.Byte).Value = record.IsPersonWithDisability;
            command.Parameters.Add("@is_indigent", MySqlDbType.Byte).Value = record.IsIndigent;
            command.Parameters.Add("@is_student", MySqlDbType.Byte).Value = record.IsStudent;
            command.Parameters.Add("@is_solo_parent", MySqlDbType.Byte).Value = record.IsSoloParent;
            command.Parameters.Add("@has_used_jobseeker_benefit", MySqlDbType.Byte).Value = record.HasUsedJobseekerBenefit;
        }

        private static Resident ReadResident(MySqlDataReader reader)
        {
            return new Resident
            {
                ResidentId = (int)reader["resident_id"],
                Version = (int)reader["version"],
                FirstName = (string)reader["first_name"],
                MiddleName = (string)reader["middle_name"],
                LastName = (string)reader["last_name"],
                Suffix = (string)reader["suffix"],
                DateOfBirth = (DateTime)reader["date_of_birth"],
                Gender = (Gender)Convert.ToInt32(reader["gender"]),
                CivilStatus = (CivilStatus)Convert.ToInt32(reader["civil_status"]),
                Purok = (string)reader["purok"],
                Address = (string)reader["address"],
                ContactNumber = (string)reader["contact_number"],
                Occupation = (string)reader["occupation"],
                DateOfResidency = (DateTime)reader["date_of_residency"],
                IsRegisteredVoter = Convert.ToBoolean(reader["is_registered_voter"]),
                IsSeniorCitizen = Convert.ToBoolean(reader["is_senior_citizen"]),
                IsPersonWithDisability = Convert.ToBoolean(reader["is_person_with_disability"]),
                IsIndigent = Convert.ToBoolean(reader["is_indigent"]),
                IsStudent = Convert.ToBoolean(reader["is_student"]),
                IsSoloParent = Convert.ToBoolean(reader["is_solo_parent"]),
                HasUsedJobseekerBenefit = Convert.ToBoolean(reader["has_used_jobseeker_benefit"])
            };
        }

        // ------------------------------------------------------------------
        //  Request mapping
        // ------------------------------------------------------------------

        private const string RequestColumns =
            "resident_id, document_type, scope, document_name, purpose, " +
            "business_name, business_address, business_nature, " +
            "date_requested, date_released, status, " +
            "fee, fee_basis, is_paid, " +
            "official_receipt_no, date_paid, rejection_reason, " +
            "released_document_text";
        private const string RequestParameters =
            "@resident_id, @document_type, @scope, @document_name, @purpose, " +
            "@business_name, @business_address, @business_nature, " +
            "@date_requested, @date_released, @status, " +
            "@fee, @fee_basis, @is_paid, " +
            "@official_receipt_no, @date_paid, @rejection_reason, " +
            "@released_document_text";
        private const string RequestAssignments =
            "document_type = @document_type, scope = @scope, document_name = @document_name, purpose = @purpose, " +
            "business_name = @business_name, business_address = @business_address, business_nature = @business_nature, " +
            "date_requested = @date_requested, date_released = @date_released, status = @status, " +
            "fee = @fee, fee_basis = @fee_basis, is_paid = @is_paid, " +
            "official_receipt_no = @official_receipt_no, date_paid = @date_paid, rejection_reason = @rejection_reason, " +
            "released_document_text = @released_document_text";

        private static void AddRequestParameters(MySqlCommand command, DocumentRequest record)
        {
            command.Parameters.Add("@resident_id", MySqlDbType.Int32).Value = record.ResidentId;
            command.Parameters.Add("@document_type", MySqlDbType.Int32).Value = (int)record.DocumentType;
            command.Parameters.Add("@scope", MySqlDbType.Byte).Value = (int)record.Scope;
            command.Parameters.Add("@document_name", MySqlDbType.VarChar, 120).Value = record.DocumentName ?? "";
            command.Parameters.Add("@purpose", MySqlDbType.VarChar, 300).Value = record.Purpose ?? "";
            command.Parameters.Add("@business_name", MySqlDbType.VarChar, 120).Value = record.BusinessName ?? "";
            command.Parameters.Add("@business_address", MySqlDbType.VarChar, 250).Value = record.BusinessAddress ?? "";
            command.Parameters.Add("@business_nature", MySqlDbType.VarChar, 150).Value = record.BusinessNature ?? "";
            command.Parameters.Add("@date_requested", MySqlDbType.DateTime).Value = record.DateRequested;
            command.Parameters.Add("@date_released", MySqlDbType.DateTime).Value =
                (object)record.DateReleased ?? DBNull.Value;
            command.Parameters.Add("@status", MySqlDbType.Int32).Value = (int)record.Status;
            var fee = command.Parameters.Add("@fee", MySqlDbType.Decimal);
            fee.Precision = 12;
            fee.Scale = 2;
            fee.Value = record.Fee;
            command.Parameters.Add("@fee_basis", MySqlDbType.VarChar, 1000).Value = record.FeeBasis ?? "";
            command.Parameters.Add("@is_paid", MySqlDbType.Byte).Value = record.IsPaid;
            // Unpaid receipts are NULL so ux_requests_receipt can do its job.
            command.Parameters.Add("@official_receipt_no", MySqlDbType.VarChar, 50).Value =
                (object)(string.IsNullOrWhiteSpace(record.OfficialReceiptNumber) ? null : record.OfficialReceiptNumber)
                ?? DBNull.Value;
            command.Parameters.Add("@date_paid", MySqlDbType.DateTime).Value =
                (object)record.DatePaid ?? DBNull.Value;
            command.Parameters.Add("@rejection_reason", MySqlDbType.VarChar, 300).Value = record.RejectionReason ?? "";
            command.Parameters.Add("@released_document_text", MySqlDbType.MediumText).Value =
                record.ReleasedDocumentText ?? "";
        }

        private static DocumentRequest ReadRequest(MySqlDataReader reader)
        {
            return new DocumentRequest
            {
                RequestId = (int)reader["request_id"],
                Version = (int)reader["version"],
                ResidentSnapshot = ReadResidentSnapshot(reader),
                ResidentId = (int)reader["resident_id"],
                DocumentType = (DocumentType)Convert.ToInt32(reader["document_type"]),
                Scope = (ClearanceScope)Convert.ToInt32(reader["scope"]),
                DocumentName = (string)reader["document_name"],
                Purpose = (string)reader["purpose"],
                BusinessName = (string)reader["business_name"],
                BusinessAddress = (string)reader["business_address"],
                BusinessNature = (string)reader["business_nature"],
                DateRequested = (DateTime)reader["date_requested"],
                DateReleased = reader["date_released"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["date_released"],
                Status = (RequestStatus)Convert.ToInt32(reader["status"]),
                Fee = (decimal)reader["fee"],
                FeeBasis = (string)reader["fee_basis"],
                IsPaid = Convert.ToBoolean(reader["is_paid"]),
                OfficialReceiptNumber = reader["official_receipt_no"] == DBNull.Value
                    ? "" : (string)reader["official_receipt_no"],
                DatePaid = reader["date_paid"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["date_paid"],
                RejectionReason = (string)reader["rejection_reason"],
                ReleasedDocumentText = (string)reader["released_document_text"]
            };
        }

        /// <summary>The snapshot columns ride the same row (aliased s.* in the
        /// SELECT); column labels arrive without the prefix, so one reader
        /// serves both tables.</summary>
        private static Resident ReadResidentSnapshot(MySqlDataReader reader)
        {
            var snapshot = ReadResident(reader);
            snapshot.ResidentId = 0;   // a snapshot is history, not a live registry row
            snapshot.Version = 1;
            return snapshot;
        }
    }
}
