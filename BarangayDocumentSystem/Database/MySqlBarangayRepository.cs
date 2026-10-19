using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using MySql.Data.MySqlClient;

using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using System.Security.Cryptography;

namespace BarangayDocumentSystem.Database;


/// MySQL implementation of <see cref="IBarangayRepository"/>.
///
/// Every query is PARAMETERISED (@name) — user text is never pasted into SQL,
/// which is what prevents SQL injection.
///
/// A connection is opened per operation and closed straight after; MySql.Data
/// pools connections, so this is cheap and avoids a stale long-lived connection.
///
/// Limit of this design: it assumes ONE running copy of the app. Two clerks on
/// two machines would not see each other's changes until Reload/restart.
public class MySqlBarangayRepository : IBarangayRepository
{
    // ------------------------------------------------- stored procedures
    // The SQL itself lives in procedures.sql (created in the database at start-up).
    // This class only CALLs them by name.
    private const string SpGetResidents    = "sp_residents_get_all";
    private const string SpGetRequests     = "sp_requests_get_all";
    private const string SpAddResident     = "sp_resident_add";
    private const string SpUpdateResident  = "sp_resident_update";
    private const string SpArchiveResident = "sp_resident_archive";
    private const string SpRestoreResident = "sp_resident_restore";
    private const string SpAddRequest      = "sp_request_add";
    private const string SpSaveRequest     = "sp_request_save";

    // state
    private readonly string _connectionString;
    private readonly FeeSchedule _feeSchedule;
    private readonly List<Resident> _residents = new();
    private readonly List<DocumentRequest> _requests = new();

    public IReadOnlyList<Resident> Residents => _residents.AsReadOnly();
    public IReadOnlyList<DocumentRequest> Requests => _requests.AsReadOnly();

    
    /// The tables must already exist — call
    /// <see cref="DatabaseInitializer.EnsureCreated"/> first.
   
    public MySqlBarangayRepository(string connectionString, FeeSchedule feeSchedule)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("A connection string is required.", nameof(connectionString));

        _connectionString = connectionString;
        _feeSchedule = feeSchedule ?? throw new ArgumentNullException(nameof(feeSchedule));
        Reload();
    }

    // loading
    public void Reload()
    {
        var residents = new List<Resident>();
        var requests = new List<DocumentRequest>();

        RunNoResult(conn =>
        {
            var byId = new Dictionary<int, Resident>();

            using (var cmd = CreateCommand(conn, SpGetResidents))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var resident = ReadResident(reader);
                    residents.Add(resident);
                    byId[resident.ResidentId] = resident;
                }
            }

            using (var cmd = CreateCommand(conn, SpGetRequests))
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
            HasAvailedFirstTimeJobseeker = GetBool(r, "has_availed_jobseeker"),
            IsArchived          = GetBool(r, "is_archived"),
            ArchivedOn          = GetNullableDate(r, "archived_on"),
            TransferDestination = GetString(r, "transfer_destination"),
            TransferReason      = GetString(r, "transfer_reason")
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

    // residents
    public Resident AddResident(ResidentDetails d)
    {
        int id = Run(conn =>
        {
            using var cmd = CreateCommand(conn, SpAddResident);
            BindResident(cmd, d);
            return Convert.ToInt32(cmd.ExecuteScalar());   // the procedure returns the new id
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
            using var cmd = CreateCommand(conn, SpUpdateResident);
            BindResident(cmd, d);
            AddParameter(cmd, "p_resident_id", resident.ResidentId);
            cmd.ExecuteNonQuery();
        });

        Apply(resident, d);
    }

    public void ArchiveResident(Resident resident, string destination, string reason)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));

        var now = DateTime.Now;
        now = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond);

        RunNoResult(conn =>
        {
            using var cmd = CreateCommand(conn, SpArchiveResident);
            AddParameter(cmd, "p_archived_on", now);
            AddParameter(cmd, "p_destination", destination ?? string.Empty);
            AddParameter(cmd, "p_reason",      reason ?? string.Empty);
            AddParameter(cmd, "p_resident_id", resident.ResidentId);
            cmd.ExecuteNonQuery();
        });

        resident.IsArchived = true;
        resident.ArchivedOn = now;
        resident.TransferDestination = destination ?? string.Empty;
        resident.TransferReason = reason ?? string.Empty;
    }

    public void RestoreResident(Resident resident)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));

        RunNoResult(conn =>
        {
            using var cmd = CreateCommand(conn, SpRestoreResident);
            AddParameter(cmd, "p_resident_id", resident.ResidentId);
            cmd.ExecuteNonQuery();
        });

        resident.IsArchived = false;
        resident.ArchivedOn = null;
        resident.TransferDestination = string.Empty;
        resident.TransferReason = string.Empty;
    }

    public IEnumerable<Resident> SearchResidents(string term) =>
        Search(_residents.Where(r => !r.IsArchived), term);

    public IEnumerable<Resident> SearchArchivedResidents(string term) =>
        Search(_residents.Where(r => r.IsArchived), term);

    private static IEnumerable<Resident> Search(IEnumerable<Resident> source, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return source.OrderBy(r => r.LastName).ThenBy(r => r.FirstName);

        return source
            // .NET Framework's string.Contains has no StringComparison overload
            // (added in .NET Core 2.0), so this does the same case-insensitive
            // check with IndexOf instead.
            .Where(r => ContainsIgnoreCase(r.GetFullName(), term)
                     || ContainsIgnoreCase(r.Purok, term)
                     || ContainsIgnoreCase(r.ContactNumber, term))
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName);
    }

    /// Copies the editable fields onto the in-memory resident.
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

    /// Binds the fourteen editable fields — shared by INSERT and UPDATE.
    private static void BindResident(DbCommand cmd, ResidentDetails d)
    {
        AddParameter(cmd, "p_first_name",          d.FirstName);
        AddParameter(cmd, "p_middle_name",         d.MiddleName ?? string.Empty);
        AddParameter(cmd, "p_last_name",           d.LastName);
        AddParameter(cmd, "p_suffix",              d.Suffix ?? string.Empty);
        AddParameter(cmd, "p_date_of_birth",       d.DateOfBirth.Date);
        AddParameter(cmd, "p_gender",              d.Gender.ToString());
        AddParameter(cmd, "p_civil_status",        d.CivilStatus.ToString());
        AddParameter(cmd, "p_purok",               d.Purok ?? string.Empty);
        AddParameter(cmd, "p_address_line",        d.AddressLine ?? string.Empty);
        AddParameter(cmd, "p_contact_number",      d.ContactNumber ?? string.Empty);
        AddParameter(cmd, "p_occupation",          d.Occupation ?? string.Empty);
        AddParameter(cmd, "p_date_of_residency",   d.DateOfResidency.Date);
        AddParameter(cmd, "p_is_registered_voter", d.IsRegisteredVoter);
        AddParameter(cmd, "p_classification",      (int)d.Classification);
    }

    // requests
    public DocumentRequest CreateRequest(Resident resident, DocumentType type, string purpose, bool isStudent = false)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));

        var assessment = _feeSchedule.Assess(resident, type, isStudent);

        // MySQL DATETIME has whole-second precision; trim now so the value in
        // memory is exactly the value that was stored.
        var now = DateTime.Now;
        now = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond);

        int id = Run(conn =>
        {
            using var cmd = CreateCommand(conn, SpAddRequest);
            AddParameter(cmd, "p_resident_id",    resident.ResidentId);
            AddParameter(cmd, "p_document_type",  type.ToString());
            AddParameter(cmd, "p_purpose",        purpose ?? string.Empty);
            AddParameter(cmd, "p_date_requested", now);
            AddParameter(cmd, "p_status",         RequestStatus.Pending.ToString());
            AddParameter(cmd, "p_fee",            assessment.FinalFee);
            AddParameter(cmd, "p_fee_basis",      assessment.Basis);
            return Convert.ToInt32(cmd.ExecuteScalar());   // the procedure returns the new id
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

        // One procedure saves the request AND the resident's RA 11261 flag in a
        // single transaction (see sp_request_save in procedures.sql).
        RunNoResult(conn =>
        {
            using var cmd = CreateCommand(conn, SpSaveRequest);
            AddParameter(cmd, "p_purpose",             request.Purpose ?? string.Empty);
            AddParameter(cmd, "p_status",              request.Status.ToString());
            AddParameter(cmd, "p_date_released",       request.DateReleased);
            AddParameter(cmd, "p_is_paid",             request.IsPaid);
            AddParameter(cmd, "p_official_receipt_no", request.OfficialReceiptNo);
            AddParameter(cmd, "p_remarks",             request.Remarks ?? string.Empty);
            AddParameter(cmd, "p_request_id",          request.RequestId);
            AddParameter(cmd, "p_has_availed",         request.Resident.HasAvailedFirstTimeJobseeker);
            AddParameter(cmd, "p_resident_id",         request.Resident.ResidentId);
            cmd.ExecuteNonQuery();
        });
    }

    private static bool ContainsIgnoreCase(string haystack, string needle) =>
        haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    public IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status) =>
        status is null
            ? _requests.OrderByDescending(r => r.DateRequested)
            : _requests.Where(r => r.Status == status).OrderByDescending(r => r.DateRequested);

    // statistics
    public BarangayStatistics GetStatistics() => new(
        TotalResidents:   _residents.Count(r => !r.IsArchived),
        RegisteredVoters: _residents.Count(r => !r.IsArchived && r.IsRegisteredVoter),
        SeniorCitizens:   _residents.Count(r => !r.IsArchived && r.HasClassification(ResidentClassification.SeniorCitizen)),
        TotalRequests:    _requests.Count,
        Pending:          _requests.Count(r => r.Status == RequestStatus.Pending),
        Processing:       _requests.Count(r => r.Status == RequestStatus.Processing),
        ReadyForRelease:  _requests.Count(r => r.Status == RequestStatus.ReadyForRelease),
        Released:         _requests.Count(r => r.Status == RequestStatus.Released),
        TotalCollected:   _requests.Where(r => r.IsPaid).Sum(r => r.Fee),
        IssuedFreeOfCharge: _requests.Count(r => r.Fee == 0 && r.Status == RequestStatus.Released),
        RequestsByDocumentType: _requests.GroupBy(r => r.GetDocumentName())
                                         .ToDictionary(g => g.Key, g => g.Count()),
        ResidentsByPurok: _residents.Where(r => !r.IsArchived).GroupBy(r => r.Purok)
                                    .OrderBy(g => g.Key)
                                    .ToDictionary(g => g.Key, g => g.Count()));

    // connection plumbing (ADO.NET)

    /// Opens a connection, runs the work, returns its result.
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

    /// Same as <see cref="Run{T}"/> for work that returns nothing.
    private void RunNoResult(Action<DbConnection> work) =>
        Run<object?>(conn => { work(conn); return null; });

    /// Turns a MySQL error into a sentence a clerk can act on.
    private static string Describe(MySqlException ex) => ex.Number switch
    {
        1042 or 2002 or 2003 or 2013 =>
            "Cannot reach the MySQL server. Check that MySQL is running and that the " +
            $"server and port in {"BarangayDocumentSystem.exe.config"} are correct.",
        1044 or 1045 =>
            $"MySQL refused the user name or password. Check {"BarangayDocumentSystem.exe.config"}.",
        1049 =>
            "The database does not exist yet.",
        1305 =>
            "A stored procedure is missing. Close and reopen the app so it can be created again.",
        _ => "The database reported a problem: " + ex.Message
    };

    /// A command that CALLs the named stored procedure.
    private static DbCommand CreateCommand(DbConnection conn, string procedure)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = procedure;
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        return cmd;
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
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

/// <summary>
/// MySQL implementation of <see cref="IUserRepository"/>. Every query is
/// parameterised, and only the password HASH is ever stored or read.
/// </summary>
public sealed class MySqlUserRepository : IUserRepository
{
    private readonly string _connectionString;

    public MySqlUserRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("A connection string is required.", nameof(connectionString));
        _connectionString = connectionString;
    }

    public bool AnyUsers() => Run(conn =>
    {
        using var cmd = Procedure(conn, "sp_user_count");
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    });

    public UserAccount? Find(string username) => Run(conn =>
    {
        using var cmd = Procedure(conn, "sp_user_find");
        cmd.Parameters.AddWithValue("p_username", username);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new UserAccount(
            reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
    });

    public void Create(string username, string fullName, string passwordHash) => Run<object?>(conn =>
    {
        using var cmd = Procedure(conn, "sp_user_add");
        cmd.Parameters.AddWithValue("p_username", username);
        cmd.Parameters.AddWithValue("p_full_name", fullName ?? string.Empty);
        cmd.Parameters.AddWithValue("p_password_hash", passwordHash);
        cmd.Parameters.AddWithValue("p_created_on", DateTime.Now);
        cmd.ExecuteNonQuery();
        return null;
    });

    private static MySqlCommand Procedure(MySqlConnection conn, string name) =>
        new MySqlCommand(name, conn) { CommandType = System.Data.CommandType.StoredProcedure };

    private T Run<T>(Func<MySqlConnection, T> work)
    {
        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return work(conn);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new RepositoryException("That username is already taken.", ex);
        }
        catch (MySqlException ex)
        {
            throw new RepositoryException("The database reported a problem: " + ex.Message, ex);
        }
    }
}

/// <summary>
/// One-way password hashing (PBKDF2-SHA256 with a random salt per password).
///
/// The database stores "iterations.salt.hash" (Base64) — never the password.
/// The iteration count is stored with each hash, so it can be raised later
/// without locking out existing accounts.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        if (password is null) throw new ArgumentNullException(nameof(password));

        var salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(salt);

        byte[] hash = Derive(password, salt, Iterations);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        if (password is null || string.IsNullOrEmpty(stored)) return false;

        string[] parts = stored.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations) || iterations <= 0)
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expected = Convert.FromBase64String(parts[2]);
            byte[] actual = Derive(password, salt, iterations);

            // Compare every byte so the time taken does not reveal where they differ.
            int diff = expected.Length ^ actual.Length;
            for (int i = 0; i < expected.Length && i < actual.Length; i++)
                diff |= expected[i] ^ actual[i];
            return diff == 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Derive(string password, byte[] salt, int iterations)
    {
        using var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return kdf.GetBytes(HashSize);
    }
}
