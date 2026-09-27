// =====================================================================
//  PART:    Database - the MySQL store (the real one)
//  ORIGIN:  Fdraft - Frent Dhieniel Raborar
//           (his SQL, his connection plumbing, his error translation, his
//            one-transaction SaveRequest - the persistence that works)
//  EDITS:   Clint Wood Gado - made it a RepositoryBase subclass so the shared
//           code exists once; added the seven columns my v3.1 request model
//           needs (scope, assessed amount, hours, income, detail, the two
//           RA 11261 flags); Rehydrate instead of his Restore; a status-bar
//           description; a clearer message for an unknown enum value
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Data.Common;
using MySql.Data.MySqlClient;
using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Database;

/// <summary>
/// The repository that keeps the barangay's records in MySQL.
///
/// This is Frent's persistence, and it is the reason the app is no longer a
/// demo. I kept his three design rules exactly as he wrote them:
///
///  1. Every query is PARAMETERISED (@name). Text a clerk typed is never
///     pasted into SQL, which is what prevents SQL injection - a surname
///     like O'Brien cannot break a statement, let alone alter one.
///  2. A connection is opened per operation and closed straight after.
///     MySql.Data pools connections, so this costs nothing, and it avoids a
///     stale connection dying quietly overnight.
///  3. Every MySQL failure becomes a <see cref="RepositoryException"/>
///     with a sentence a clerk can act on, and the view that called us
///     reloads, so the screen never shows a save that did not happen.
///
/// What changed from his file is what I took OUT: the working set, the
/// queries, Apply and the statistics are inherited from RepositoryBase now,
/// so this file is only the SQL and the mapping between rows and objects.
///
/// Known limit, inherited and accepted: this assumes ONE running copy of the
/// program. Two clerks on two machines will not see each other's changes
/// until one of them reloads.
/// </summary>
public sealed class MySqlBarangayRepository : RepositoryBase
{
    // ---------------------------------------------------------------- SQL

    private const string ResidentColumns =
        "resident_id, first_name, middle_name, last_name, suffix, date_of_birth, gender, " +
        "civil_status, purok, address_line, contact_number, occupation, date_of_residency, " +
        "is_registered_voter, classification, has_availed_jobseeker";

    private const string SqlSelectResidents =
        "SELECT " + ResidentColumns + " FROM residents ORDER BY resident_id";

    private const string SqlSelectRequests =
        "SELECT request_id, resident_id, document_type, purpose, date_requested, date_released, " +
        "status, fee, fee_basis, is_paid, official_receipt_no, remarks, " +
        "scope, assessed_amount, hours, gross_annual_income, detail, " +
        "apply_jobseeker_waiver, availed_under_jobseeker_act " +
        "FROM document_requests ORDER BY request_id";

    private const string SqlInsertResident =
        "INSERT INTO residents (first_name, middle_name, last_name, suffix, date_of_birth, gender, " +
        "civil_status, purok, address_line, contact_number, occupation, date_of_residency, " +
        "is_registered_voter, classification) " +
        "VALUES (@first_name, @middle_name, @last_name, @suffix, @date_of_birth, @gender, " +
        "@civil_status, @purok, @address_line, @contact_number, @occupation, @date_of_residency, " +
        "@is_registered_voter, @classification)";

    private const string SqlUpdateResident =
        "UPDATE residents SET first_name=@first_name, middle_name=@middle_name, last_name=@last_name, " +
        "suffix=@suffix, date_of_birth=@date_of_birth, gender=@gender, civil_status=@civil_status, " +
        "purok=@purok, address_line=@address_line, contact_number=@contact_number, " +
        "occupation=@occupation, date_of_residency=@date_of_residency, " +
        "is_registered_voter=@is_registered_voter, classification=@classification " +
        "WHERE resident_id=@resident_id";

    // The resident's requests are removed by the database itself
    // (ON DELETE CASCADE in schema.sql); the base class drops them from the
    // working set.
    private const string SqlDeleteResident =
        "DELETE FROM residents WHERE resident_id=@resident_id";

    private const string SqlInsertRequest =
        "INSERT INTO document_requests (resident_id, document_type, purpose, date_requested, status, " +
        "fee, fee_basis, is_paid, official_receipt_no, remarks, " +
        "scope, assessed_amount, hours, gross_annual_income, detail, " +
        "apply_jobseeker_waiver, availed_under_jobseeker_act) " +
        "VALUES (@resident_id, @document_type, @purpose, @date_requested, @status, " +
        "@fee, @fee_basis, 0, '', '', " +
        "@scope, @assessed_amount, @hours, @gross_annual_income, @detail, " +
        "@apply_jobseeker_waiver, @availed_under_jobseeker_act)";

    private const string SqlUpdateRequest =
        "UPDATE document_requests SET purpose=@purpose, status=@status, date_released=@date_released, " +
        "is_paid=@is_paid, official_receipt_no=@official_receipt_no, remarks=@remarks " +
        "WHERE request_id=@request_id";

    private const string SqlUpdateJobseekerFlag =
        "UPDATE residents SET has_availed_jobseeker=@has_availed WHERE resident_id=@resident_id";

    private readonly string _connectionString;
    private readonly string _description;

    /// <summary>
    /// The tables must already exist - Program.cs calls
    /// <see cref="DatabaseInitializer.EnsureCreated"/> first. The constructor
    /// loads everything straight away, so a wrong password fails here, at
    /// start-up, and not on the first click.
    /// </summary>
    public MySqlBarangayRepository(string connectionString, FeeSchedule fees) : base(fees)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("A connection string is required.", nameof(connectionString));

        _connectionString = connectionString;

        var builder = new MySqlConnectionStringBuilder(connectionString);
        _description = $"MySQL \u2014 {builder.Server}/{builder.Database}";

        Reload();
    }

    public override string StorageDescription => _description;

    // ------------------------------------------------------------ loading

    public override void Reload()
    {
        var residents = new List<Resident>();
        var requests = new List<DocumentRequest>();

        Run(conn =>
        {
            var byId = new Dictionary<int, Resident>();

            using (var cmd = CreateCommand(conn, SqlSelectResidents))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var resident = ReadResident(reader);
                    residents.Add(resident);
                    byId[resident.ResidentId] = resident;
                }
            }

            using (var cmd = CreateCommand(conn, SqlSelectRequests))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    // The foreign key guarantees the resident exists.
                    var resident = byId[GetInt(reader, "resident_id")];
                    var request = ReadRequest(reader, resident);
                    requests.Add(request);
                    resident.AddRequest(request);
                }
            }
        });

        // Swap only after BOTH queries succeeded. A failed reload leaves the
        // old data in place instead of an empty screen.
        ReplaceWorkingSet(residents, requests);
    }

    private static Resident ReadResident(DbDataReader r) =>
        new(GetInt(r, "resident_id"), GetString(r, "first_name"), GetString(r, "last_name"))
        {
            MiddleName        = GetString(r, "middle_name"),
            Suffix            = GetString(r, "suffix"),
            DateOfBirth       = GetDate(r, "date_of_birth"),
            Gender            = ParseEnum<Gender>(r, "gender"),
            CivilStatus       = ParseEnum<CivilStatus>(r, "civil_status"),
            Purok             = GetString(r, "purok"),
            AddressLine       = GetString(r, "address_line"),
            ContactNumber     = GetString(r, "contact_number"),
            Occupation        = GetString(r, "occupation"),
            DateOfResidency   = GetDate(r, "date_of_residency"),
            IsRegisteredVoter = GetBool(r, "is_registered_voter"),
            Classification    = (ResidentClassification)GetInt(r, "classification"),
            HasAvailedFirstTimeJobseeker = GetBool(r, "has_availed_jobseeker")
        };

    /// <summary>
    /// A row back into an object, through Rehydrate - the door I built for
    /// exactly this, so that loading a request released two years ago does
    /// not re-run today's rules against it.
    /// </summary>
    private static DocumentRequest ReadRequest(DbDataReader r, Resident resident)
    {
        var input = new RequestInput(
            Scope:                ParseEnum<ClearanceScope>(r, "scope"),
            Amount:               GetDecimal(r, "assessed_amount"),
            Hours:                GetDecimal(r, "hours"),
            GrossAnnualIncome:    GetDecimal(r, "gross_annual_income"),
            Detail:               GetString(r, "detail"),
            ApplyJobseekerWaiver: GetBool(r, "apply_jobseeker_waiver"));

        return DocumentRequest.Rehydrate(
            GetInt(r, "request_id"),
            resident,
            ParseEnum<DocumentType>(r, "document_type"),
            GetString(r, "purpose"),
            GetDate(r, "date_requested"),
            GetNullableDate(r, "date_released"),
            ParseEnum<RequestStatus>(r, "status"),
            GetDecimal(r, "fee"),
            GetString(r, "fee_basis"),
            GetBool(r, "is_paid"),
            GetString(r, "official_receipt_no"),
            GetString(r, "remarks"),
            input,
            GetBool(r, "availed_under_jobseeker_act"));
    }

    // ---------------------------------------------- the RepositoryBase hooks

    protected override int InsertResident(ResidentDetails details) =>
        Run(conn =>
        {
            using (var cmd = CreateCommand(conn, SqlInsertResident))
            {
                BindResident(cmd, details);
                cmd.ExecuteNonQuery();
            }
            return LastInsertId(conn);
        });

    protected override void UpdateResidentRow(Resident resident, ResidentDetails details) =>
        Run(conn =>
        {
            using var cmd = CreateCommand(conn, SqlUpdateResident);
            BindResident(cmd, details);
            AddParameter(cmd, "@resident_id", resident.ResidentId);
            cmd.ExecuteNonQuery();
        });

    protected override void DeleteResidentRow(Resident resident) =>
        Run(conn =>
        {
            using var cmd = CreateCommand(conn, SqlDeleteResident);
            AddParameter(cmd, "@resident_id", resident.ResidentId);
            cmd.ExecuteNonQuery();
        });

    protected override int InsertRequest(
        Resident resident, DocumentType type, string purpose, RequestInput input,
        FeeAssessment assessment, DateTime filedOn) =>
        Run(conn =>
        {
            using (var cmd = CreateCommand(conn, SqlInsertRequest))
            {
                AddParameter(cmd, "@resident_id",         resident.ResidentId);
                AddParameter(cmd, "@document_type",       type.ToString());
                AddParameter(cmd, "@purpose",             purpose);
                AddParameter(cmd, "@date_requested",      filedOn);
                AddParameter(cmd, "@status",              RequestStatus.Pending.ToString());
                AddParameter(cmd, "@fee",                 assessment.FinalFee);
                AddParameter(cmd, "@fee_basis",           assessment.Basis);
                AddParameter(cmd, "@scope",               input.Scope.ToString());
                AddParameter(cmd, "@assessed_amount",     input.Amount);
                AddParameter(cmd, "@hours",               input.Hours);
                AddParameter(cmd, "@gross_annual_income", input.GrossAnnualIncome);
                AddParameter(cmd, "@detail",              input.Detail ?? string.Empty);
                AddParameter(cmd, "@apply_jobseeker_waiver",      input.ApplyJobseekerWaiver);
                AddParameter(cmd, "@availed_under_jobseeker_act", assessment.MarksJobseekerAvailment);
                cmd.ExecuteNonQuery();
            }
            return LastInsertId(conn);
        });

    protected override void UpdateRequestRow(DocumentRequest request) =>
        Run(conn =>
        {
            // One transaction: the request row and the resident's RA 11261
            // once-only flag either both change or neither does. Without
            // this, a crash between the two statements could release the
            // certificate and forget that it was ever claimed.
            using var tx = conn.BeginTransaction();

            using (var cmd = CreateCommand(conn, SqlUpdateRequest, tx))
            {
                AddParameter(cmd, "@purpose",             request.Purpose ?? string.Empty);
                AddParameter(cmd, "@status",              request.Status.ToString());
                AddParameter(cmd, "@date_released",       request.DateReleased);
                AddParameter(cmd, "@is_paid",             request.IsPaid);
                AddParameter(cmd, "@official_receipt_no", request.OfficialReceiptNo ?? string.Empty);
                AddParameter(cmd, "@remarks",             request.Remarks ?? string.Empty);
                AddParameter(cmd, "@request_id",          request.RequestId);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = CreateCommand(conn, SqlUpdateJobseekerFlag, tx))
            {
                AddParameter(cmd, "@has_availed", request.Resident.HasAvailedFirstTimeJobseeker);
                AddParameter(cmd, "@resident_id", request.Resident.ResidentId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        });

    /// <summary>Bind the fourteen editable fields - shared by INSERT and
    /// UPDATE, so the two statements cannot drift apart.</summary>
    private static void BindResident(DbCommand cmd, ResidentDetails d)
    {
        AddParameter(cmd, "@first_name",          d.FirstName);
        AddParameter(cmd, "@middle_name",         d.MiddleName ?? string.Empty);
        AddParameter(cmd, "@last_name",           d.LastName);
        AddParameter(cmd, "@suffix",              d.Suffix ?? string.Empty);
        AddParameter(cmd, "@date_of_birth",       d.DateOfBirth.Date);
        AddParameter(cmd, "@gender",              d.Gender.ToString());
        AddParameter(cmd, "@civil_status",        d.CivilStatus.ToString());
        AddParameter(cmd, "@purok",               d.Purok ?? string.Empty);
        AddParameter(cmd, "@address_line",        d.AddressLine ?? string.Empty);
        AddParameter(cmd, "@contact_number",      d.ContactNumber ?? string.Empty);
        AddParameter(cmd, "@occupation",          d.Occupation ?? string.Empty);
        AddParameter(cmd, "@date_of_residency",   d.DateOfResidency.Date);
        AddParameter(cmd, "@is_registered_voter", d.IsRegisteredVoter);
        AddParameter(cmd, "@classification",      (int)d.Classification);
    }

    // ------------------------------------------- connection plumbing (ADO.NET)

    /// <summary>Open a connection, run the work, return its result. Any
    /// MySQL error comes out as a RepositoryException the views know how
    /// to show.</summary>
    private T Run<T>(Func<DbConnection, T> work)
    {
        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return work(conn);
        }
        catch (MySqlException ex)
        {
            throw new RepositoryException(Describe(ex), ex);
        }
    }

    /// <summary>Same as <see cref="Run{T}"/> for work that returns nothing.</summary>
    private void Run(Action<DbConnection> work) =>
        Run<object?>(conn => { work(conn); return null; });

    /// <summary>
    /// Turn a MySQL error number into a sentence a clerk can act on. The
    /// raw driver message ("Unable to connect to any of the specified MySQL
    /// hosts") means nothing to the person at the counter; "start MySQL"
    /// does.
    /// </summary>
    private static string Describe(MySqlException ex) => ex.Number switch
    {
        1042 or 2002 or 2003 or 2013 =>
            "Cannot reach the MySQL server. Check that MySQL is running (XAMPP: start " +
            "MySQL in the control panel) and that the server and port in " +
            "BarangayDocumentSystem.exe.config are correct.",
        1044 or 1045 =>
            "MySQL refused the user name or password. Check the BarangayDb connection " +
            "string in BarangayDocumentSystem.exe.config.",
        1049 =>
            "The database does not exist yet. Start the program again so it can create it.",
        _ => "The database reported a problem: " + ex.Message
    };

    private static DbCommand CreateCommand(DbConnection conn, string sql, DbTransaction? tx = null)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        return cmd;
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    /// <summary>The id generated by the INSERT just run on THIS connection.</summary>
    private static int LastInsertId(DbConnection conn)
    {
        using var cmd = CreateCommand(conn, "SELECT LAST_INSERT_ID()");
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // Small readers so the mapping code above stays one line per column.
    private static int GetInt(DbDataReader r, string col) => r.GetInt32(r.GetOrdinal(col));
    private static DateTime GetDate(DbDataReader r, string col) => r.GetDateTime(r.GetOrdinal(col));

    private static decimal GetDecimal(DbDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? 0m : r.GetDecimal(i);
    }

    private static string GetString(DbDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? string.Empty : r.GetString(i);
    }

    private static bool GetBool(DbDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return !r.IsDBNull(i) && Convert.ToBoolean(r.GetValue(i));
    }

    private static DateTime? GetNullableDate(DbDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetDateTime(i);
    }

    /// <summary>
    /// Enum columns hold the enum's NAME ('Female', 'ReadyForRelease'), so
    /// the table is readable in phpMyAdmin and reordering an enum in C# can
    /// never silently change what a row means. A blank cell falls back to
    /// the enum's default (which is why ClearanceScope.Local is its first
    /// member); a name this version has never heard of is reported plainly
    /// instead of as an ArgumentException from deep inside the driver.
    /// </summary>
    private static TEnum ParseEnum<TEnum>(DbDataReader r, string col) where TEnum : struct
    {
        string text = GetString(r, col);
        if (text.Length == 0) return default;

        if (Enum.TryParse(text, ignoreCase: true, out TEnum value)) return value;

        throw new RepositoryException(
            $"Column '{col}' holds the value '{text}', which this version of the program " +
            $"does not recognise as a {typeof(TEnum).Name}.");
    }
}
