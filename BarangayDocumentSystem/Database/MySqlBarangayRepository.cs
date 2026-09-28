using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using MySql.Data.MySqlClient;

using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Database;

/// <summary>
/// MySQL implementation of <see cref="IBarangayRepository"/>.
///
/// HOW IT WORKS — "load once, write through"
///   • On start (and on <see cref="Reload"/>) every resident and request is read
///     into memory. The views read from that cache, so screens stay fast and
///     the object graph (a Resident holding its Requests) works as before.
///   • Every change is written to MySQL FIRST. Only when the database accepts
///     it is the in-memory copy updated, so memory never runs ahead of the DB.
///   • Workflow changes (StartProcessing, Release, RecordPayment, Reject) happen
///     on the request object itself, so callers must then call
///     <see cref="SaveRequest"/> to store them.
///
/// Every query is PARAMETERISED (@name) — user text is never pasted into SQL,
/// which is what prevents SQL injection.
///
/// A connection is opened per operation and closed straight after; MySql.Data
/// pools connections, so this is cheap and avoids a stale long-lived connection.
///
/// Limit of this design: it assumes ONE running copy of the app. Two clerks on
/// two machines would not see each other's changes until Reload/restart.
/// </summary>
public class MySqlBarangayRepository : IBarangayRepository
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
        "status, fee, fee_basis, is_paid, official_receipt_no, remarks " +
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

    // Requests are removed by the database itself (ON DELETE CASCADE).
    private const string SqlDeleteResident =
        "DELETE FROM residents WHERE resident_id=@resident_id";

    private const string SqlInsertRequest =
        "INSERT INTO document_requests (resident_id, document_type, purpose, date_requested, status, " +
        "fee, fee_basis, is_paid, official_receipt_no, remarks) " +
        "VALUES (@resident_id, @document_type, @purpose, @date_requested, @status, " +
        "@fee, @fee_basis, 0, '', '')";

    private const string SqlUpdateRequest =
        "UPDATE document_requests SET purpose=@purpose, status=@status, date_released=@date_released, " +
        "is_paid=@is_paid, official_receipt_no=@official_receipt_no, remarks=@remarks " +
        "WHERE request_id=@request_id";

    private const string SqlUpdateJobseekerFlag =
        "UPDATE residents SET has_availed_jobseeker=@has_availed WHERE resident_id=@resident_id";

    // -------------------------------------------------------------- state
    private readonly string _connectionString;
    private readonly FeeSchedule _feeSchedule;
    private readonly List<Resident> _residents = new();
    private readonly List<DocumentRequest> _requests = new();

    public IReadOnlyList<Resident> Residents => _residents.AsReadOnly();
    public IReadOnlyList<DocumentRequest> Requests => _requests.AsReadOnly();

    /// <summary>
    /// The tables must already exist — call
    /// <see cref="DatabaseInitializer.EnsureCreated"/> first.
    /// </summary>
    public MySqlBarangayRepository(string connectionString, FeeSchedule feeSchedule)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("A connection string is required.", nameof(connectionString));

        _connectionString = connectionString;
        _feeSchedule = feeSchedule ?? throw new ArgumentNullException(nameof(feeSchedule));
        Reload();
    }

    // ----------------------------------------------------------- loading
    public void Reload()
    {
        var residents = new List<Resident>();
        var requests = new List<DocumentRequest>();

        RunNoResult(conn =>
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

        // Swap only after BOTH queries succeeded, so a failed reload leaves the
        // old data in place instead of an empty screen.
        _residents.Clear();
        _residents.AddRange(residents);
        _requests.Clear();
        _requests.AddRange(requests);
    }

    private static Resident ReadResident(DbDataReader r) =>
        new(GetInt(r, "resident_id"), GetString(r, "first_name"), GetString(r, "last_name"))
        {
            MiddleName        = GetString(r, "middle_name"),
            Suffix            = GetString(r, "suffix"),
            DateOfBirth       = GetDate(r, "date_of_birth"),
            Gender            = (Gender)Enum.Parse(typeof(Gender), GetString(r, "gender")),
            CivilStatus       = (CivilStatus)Enum.Parse(typeof(CivilStatus), GetString(r, "civil_status")),
            Purok             = GetString(r, "purok"),
            AddressLine       = GetString(r, "address_line"),
            ContactNumber     = GetString(r, "contact_number"),
            Occupation        = GetString(r, "occupation"),
            DateOfResidency   = GetDate(r, "date_of_residency"),
            IsRegisteredVoter = GetBool(r, "is_registered_voter"),
            Classification    = (ResidentClassification)GetInt(r, "classification"),
            HasAvailedFirstTimeJobseeker = GetBool(r, "has_availed_jobseeker")
        };

    private static DocumentRequest ReadRequest(DbDataReader r, Resident resident) =>
        DocumentRequest.Restore(
            requestId:         GetInt(r, "request_id"),
            resident:          resident,
            documentType:      (DocumentType)Enum.Parse(typeof(DocumentType), GetString(r, "document_type")),
            purpose:           GetString(r, "purpose"),
            dateRequested:     GetDate(r, "date_requested"),
            dateReleased:      GetNullableDate(r, "date_released"),
            status:            (RequestStatus)Enum.Parse(typeof(RequestStatus), GetString(r, "status")),
            fee:               GetDecimal(r, "fee"),
            feeBasis:          GetString(r, "fee_basis"),
            isPaid:            GetBool(r, "is_paid"),
            officialReceiptNo: GetString(r, "official_receipt_no"),
            remarks:           GetString(r, "remarks"));

    // --------------------------------------------------------- residents
    public Resident AddResident(ResidentDetails d)
    {
        int id = Run(conn =>
        {
            using (var cmd = CreateCommand(conn, SqlInsertResident))
            {
                BindResident(cmd, d);
                cmd.ExecuteNonQuery();
            }
            return LastInsertId(conn);
        });

        // Database accepted it — now mirror it in memory.
        var resident = new Resident(id, d.FirstName, d.LastName);
        Apply(resident, d);
        _residents.Add(resident);
        return resident;
    }

    public void UpdateResident(Resident resident, ResidentDetails d)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));

        RunNoResult(conn =>
        {
            using var cmd = CreateCommand(conn, SqlUpdateResident);
            BindResident(cmd, d);
            AddParameter(cmd, "@resident_id", resident.ResidentId);
            cmd.ExecuteNonQuery();
        });

        Apply(resident, d);
    }

    public void RemoveResident(Resident resident)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));

        RunNoResult(conn =>
        {
            using var cmd = CreateCommand(conn, SqlDeleteResident);
            AddParameter(cmd, "@resident_id", resident.ResidentId);
            cmd.ExecuteNonQuery();
        });

        _requests.RemoveAll(r => r.Resident == resident);
        _residents.Remove(resident);
    }

    public IEnumerable<Resident> SearchResidents(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return _residents.OrderBy(r => r.LastName).ThenBy(r => r.FirstName);

        return _residents
            // .NET Framework's string.Contains has no StringComparison overload
            // (added in .NET Core 2.0), so this does the same case-insensitive
            // check with IndexOf instead.
            .Where(r => ContainsIgnoreCase(r.GetFullName(), term)
                     || ContainsIgnoreCase(r.Purok, term)
                     || ContainsIgnoreCase(r.ContactNumber, term))
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName);
    }

    /// <summary>Copies the editable fields onto the in-memory resident.</summary>
    private static void Apply(Resident r, ResidentDetails d)
    {
        r.FirstName         = d.FirstName;
        r.MiddleName        = d.MiddleName;
        r.LastName          = d.LastName;
        r.Suffix            = d.Suffix;
        r.DateOfBirth       = d.DateOfBirth;
        r.Gender            = d.Gender;
        r.CivilStatus       = d.CivilStatus;
        r.Purok             = d.Purok;
        r.AddressLine       = d.AddressLine;
        r.ContactNumber     = d.ContactNumber;
        r.Occupation        = d.Occupation;
        r.DateOfResidency   = d.DateOfResidency;
        r.IsRegisteredVoter = d.IsRegisteredVoter;
        r.Classification    = d.Classification;
    }

    /// <summary>Binds the fourteen editable fields — shared by INSERT and UPDATE.</summary>
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

    // ---------------------------------------------------------- requests
    public DocumentRequest CreateRequest(Resident resident, DocumentType type, string purpose)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));

        var assessment = _feeSchedule.Assess(resident, type);

        // MySQL DATETIME has whole-second precision; trim now so the value in
        // memory is exactly the value that was stored.
        var now = DateTime.Now;
        now = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond);

        int id = Run(conn =>
        {
            using (var cmd = CreateCommand(conn, SqlInsertRequest))
            {
                AddParameter(cmd, "@resident_id",    resident.ResidentId);
                AddParameter(cmd, "@document_type",  type.ToString());
                AddParameter(cmd, "@purpose",        purpose ?? string.Empty);
                AddParameter(cmd, "@date_requested", now);
                AddParameter(cmd, "@status",         RequestStatus.Pending.ToString());
                AddParameter(cmd, "@fee",            assessment.FinalFee);
                AddParameter(cmd, "@fee_basis",      assessment.Basis);
                cmd.ExecuteNonQuery();
            }
            return LastInsertId(conn);
        });

        var request = DocumentRequest.Restore(
            id, resident, type, purpose ?? string.Empty, now, dateReleased: null,
            RequestStatus.Pending, assessment.FinalFee, assessment.Basis,
            isPaid: false, officialReceiptNo: string.Empty, remarks: string.Empty);

        _requests.Add(request);
        resident.AddRequest(request);
        return request;
    }

    public void SaveRequest(DocumentRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        RunNoResult(conn =>
        {
            // One transaction: the request row and the resident's RA 11261
            // flag either both change or neither does.
            using var tx = conn.BeginTransaction();

            using (var cmd = CreateCommand(conn, SqlUpdateRequest, tx))
            {
                AddParameter(cmd, "@purpose",             request.Purpose ?? string.Empty);
                AddParameter(cmd, "@status",              request.Status.ToString());
                AddParameter(cmd, "@date_released",       request.DateReleased);
                AddParameter(cmd, "@is_paid",             request.IsPaid);
                AddParameter(cmd, "@official_receipt_no", request.OfficialReceiptNo);
                AddParameter(cmd, "@remarks",             request.Remarks ?? string.Empty);
                AddParameter(cmd, "@request_id",          request.RequestId);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = CreateCommand(conn, SqlUpdateJobseekerFlag, tx))
            {
                AddParameter(cmd, "@has_availed",  request.Resident.HasAvailedFirstTimeJobseeker);
                AddParameter(cmd, "@resident_id",  request.Resident.ResidentId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        });
    }

    private static bool ContainsIgnoreCase(string haystack, string needle) =>
        haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    public IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status) =>
        status is null
            ? _requests.OrderByDescending(r => r.DateRequested)
            : _requests.Where(r => r.Status == status).OrderByDescending(r => r.DateRequested);

    // -------------------------------------------------------- statistics
    public BarangayStatistics GetStatistics() => new(
        TotalResidents:   _residents.Count,
        RegisteredVoters: _residents.Count(r => r.IsRegisteredVoter),
        SeniorCitizens:   _residents.Count(r => r.HasClassification(ResidentClassification.SeniorCitizen)),
        TotalRequests:    _requests.Count,
        Pending:          _requests.Count(r => r.Status == RequestStatus.Pending),
        Processing:       _requests.Count(r => r.Status == RequestStatus.Processing),
        ReadyForRelease:  _requests.Count(r => r.Status == RequestStatus.ReadyForRelease),
        Released:         _requests.Count(r => r.Status == RequestStatus.Released),
        TotalCollected:   _requests.Where(r => r.IsPaid).Sum(r => r.Fee),
        IssuedFreeOfCharge: _requests.Count(r => r.Fee == 0 && r.Status == RequestStatus.Released),
        RequestsByDocumentType: _requests.GroupBy(r => r.GetDocumentName())
                                         .ToDictionary(g => g.Key, g => g.Count()),
        ResidentsByPurok: _residents.GroupBy(r => r.Purok)
                                    .OrderBy(g => g.Key)
                                    .ToDictionary(g => g.Key, g => g.Count()));

    // ------------------------------------------- connection plumbing (ADO.NET)

    /// <summary>Opens a connection, runs the work, returns its result.</summary>
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
    private void RunNoResult(Action<DbConnection> work) =>
        Run<object?>(conn => { work(conn); return null; });

    /// <summary>Turns a MySQL error into a sentence a clerk can act on.</summary>
    private static string Describe(MySqlException ex) => ex.Number switch
    {
        1042 or 2002 or 2003 or 2013 =>
            "Cannot reach the MySQL server. Check that MySQL is running and that the " +
            $"server and port in {"BarangayDocumentSystem.exe.config"} are correct.",
        1044 or 1045 =>
            $"MySQL refused the user name or password. Check {"BarangayDocumentSystem.exe.config"}.",
        1049 =>
            "The database does not exist yet.",
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

    /// <summary>Id generated by the INSERT just run on THIS connection.</summary>
    private static int LastInsertId(DbConnection conn)
    {
        using var cmd = CreateCommand(conn, "SELECT LAST_INSERT_ID()");
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // Small readers so the mapping code above stays one line per column.
    private static int GetInt(DbDataReader r, string col) => r.GetInt32(r.GetOrdinal(col));
    private static decimal GetDecimal(DbDataReader r, string col) => r.GetDecimal(r.GetOrdinal(col));
    private static DateTime GetDate(DbDataReader r, string col) => r.GetDateTime(r.GetOrdinal(col));

    private static string GetString(DbDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? string.Empty : r.GetString(i);
    }

    private static bool GetBool(DbDataReader r, string col) =>
        Convert.ToBoolean(r.GetValue(r.GetOrdinal(col)));

    private static DateTime? GetNullableDate(DbDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetDateTime(i);
    }
}
