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

public class MySqlBarangayRepository : IBarangayRepository
{
    private const string SpGetResidents    = "sp_residents_get_all";
    private const string SpGetRequests     = "sp_requests_get_all";
    private const string SpAddResident     = "sp_resident_add";
    private const string SpUpdateResident  = "sp_resident_update";
    private const string SpArchiveResident = "sp_resident_archive";
    private const string SpRestoreResident = "sp_resident_restore";
    private const string SpAddRequest      = "sp_request_add";
    private const string SpSaveRequest     = "sp_request_save";
    private const string SpDependentsGetAll = "sp_dependents_get_all";
    private const string SpDependentsClear  = "sp_dependents_clear";
    private const string SpDependentAdd     = "sp_dependent_add";
    private const string SpActivityAdd      = "sp_activity_add";
    private const string SpActivityByDate   = "sp_activity_get_by_date";


    private readonly string _connectionString;
    private readonly FeeSchedule _feeSchedule;
    private readonly List<Resident> _residents = new();
    private readonly List<DocumentRequest> _requests = new();
    private string _currentUser = string.Empty;

    public IReadOnlyList<Resident> Residents => _residents.AsReadOnly();
    public IReadOnlyList<DocumentRequest> Requests => _requests.AsReadOnly();

    public MySqlBarangayRepository(string connectionString, FeeSchedule feeSchedule)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("A connection string is required.", nameof(connectionString));

        _connectionString = connectionString;
        _feeSchedule = feeSchedule ?? throw new ArgumentNullException(nameof(feeSchedule));
        Reload();
    }

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

                    var resident = byId[GetInt(reader, "resident_id")];
                    var request = ReadRequest(reader, resident);
                    requests.Add(request);
                    resident.AddRequest(request);
                }
            }

            var dependentsByResident = new Dictionary<int, List<Dependent>>();
            using (var cmd = CreateCommand(conn, SpDependentsGetAll))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int residentId = GetInt(reader, "resident_id");
                    if (!byId.ContainsKey(residentId)) continue;
                    if (!dependentsByResident.TryGetValue(residentId, out var list))
                        dependentsByResident[residentId] = list = new List<Dependent>();
                    list.Add(new Dependent(
                        GetString(reader, "full_name"),
                        GetString(reader, "relationship"),
                        GetNullableDate(reader, "date_of_birth")));
                }
            }
            foreach (var pair in dependentsByResident)
                byId[pair.Key].SetDependents(pair.Value);
        });



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
            HouseNumber       = GetString(r, "house_number"),
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
            remarks:           GetString(r, "remarks"),
            datePaid:          GetNullableDate(r, "date_paid"),
            receivedBy:        GetString(r, "received_by"));


    public Resident AddResident(ResidentDetails d)
    {
        int id = Run(conn =>
        {
            using var cmd = CreateCommand(conn, SpAddResident);
            BindResident(cmd, d);
            return Convert.ToInt32(cmd.ExecuteScalar());
        });


        var resident = new Resident(id, d.FirstName, d.LastName);
        Apply(resident, d);
        SaveDependents(id, d.Dependents);
        resident.SetDependents(d.Dependents);
        _residents.Add(resident);
        LogActivity("Resident added", resident.GetFullName());
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

        SaveDependents(resident.ResidentId, d.Dependents);
        Apply(resident, d);
        resident.SetDependents(d.Dependents);
        LogActivity("Resident updated", resident.GetFullName());
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
        LogActivity("Resident archived", resident.GetFullName());
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
        LogActivity("Resident restored", resident.GetFullName());
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



            .Where(r => ContainsIgnoreCase(r.GetFullName(), term)
                     || ContainsIgnoreCase(r.Purok, term)
                     || ContainsIgnoreCase(r.ContactNumber, term))
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName);
    }


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
        r.HouseNumber       = d.HouseNumber;
        r.ContactNumber     = d.ContactNumber;
        r.Occupation        = d.Occupation;
        r.DateOfResidency   = d.DateOfResidency;
        r.IsRegisteredVoter = d.IsRegisteredVoter;
        r.Classification    = d.Classification;
    }


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
        AddParameter(cmd, "p_house_number",        d.HouseNumber ?? string.Empty);
        AddParameter(cmd, "p_contact_number",      d.ContactNumber ?? string.Empty);
        AddParameter(cmd, "p_occupation",          d.Occupation ?? string.Empty);
        AddParameter(cmd, "p_date_of_residency",   d.DateOfResidency.Date);
        AddParameter(cmd, "p_is_registered_voter", d.IsRegisteredVoter);
        AddParameter(cmd, "p_classification",      (int)d.Classification);
    }


    private void SaveDependents(int residentId, IReadOnlyList<Dependent>? dependents)
    {
        if (dependents is null) return;

        RunNoResult(conn =>
        {
            using (var clear = CreateCommand(conn, SpDependentsClear))
            {
                AddParameter(clear, "p_resident_id", residentId);
                clear.ExecuteNonQuery();
            }

            foreach (var dep in dependents)
            {
                using var cmd = CreateCommand(conn, SpDependentAdd);
                AddParameter(cmd, "p_full_name", dep.FullName);
                AddParameter(cmd, "p_relationship", dep.Relationship ?? string.Empty);
                AddParameter(cmd, "p_date_of_birth", dep.DateOfBirth);
                AddParameter(cmd, "p_resident_id", residentId);
                cmd.ExecuteNonQuery();
            }
        });
    }


    public DocumentRequest CreateRequest(Resident resident, DocumentType type, string purpose, bool isStudent = false)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));

        var assessment = _feeSchedule.Assess(resident, type, isStudent);



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
            return Convert.ToInt32(cmd.ExecuteScalar());
        });

        var request = DocumentRequest.Restore(
            id, resident, type, purpose ?? string.Empty, now, dateReleased: null,
            RequestStatus.Pending, assessment.FinalFee, assessment.Basis,
            isPaid: false, officialReceiptNo: string.Empty, remarks: string.Empty);

        _requests.Add(request);
        resident.AddRequest(request);
        LogActivity("Request filed", $"{request.GetDocumentName()} for {resident.GetFullName()}");
        return request;
    }

    public void SaveRequest(DocumentRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));



        RunNoResult(conn =>
        {
            using var cmd = CreateCommand(conn, SpSaveRequest);
            AddParameter(cmd, "p_purpose",             request.Purpose ?? string.Empty);
            AddParameter(cmd, "p_status",              request.Status.ToString());
            AddParameter(cmd, "p_date_released",       request.DateReleased);
            AddParameter(cmd, "p_is_paid",             request.IsPaid);
            AddParameter(cmd, "p_official_receipt_no", request.OfficialReceiptNo);
            AddParameter(cmd, "p_date_paid",           request.DatePaid);
            AddParameter(cmd, "p_received_by",         request.ReceivedBy);
            AddParameter(cmd, "p_remarks",             request.Remarks ?? string.Empty);
            AddParameter(cmd, "p_request_id",          request.RequestId);
            AddParameter(cmd, "p_has_availed",         request.Resident.HasAvailedFirstTimeJobseeker);
            AddParameter(cmd, "p_resident_id",         request.Resident.ResidentId);
            cmd.ExecuteNonQuery();
        });
        LogActivity("Request updated", $"{request.GetReferenceNumber()} — {request.Status}");
    }

    private static bool ContainsIgnoreCase(string haystack, string needle) =>
        haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    public string GetNextOfficialReceiptNo() => OfficialReceiptNumber.Next(_requests, DateTime.Today);

    public IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status) =>
        status is null
            ? _requests.OrderByDescending(r => r.DateRequested)
            : _requests.Where(r => r.Status == status).OrderByDescending(r => r.DateRequested);


    public string CurrentUser
    {
        get => _currentUser;
        set => _currentUser = value ?? string.Empty;
    }

    public void LogActivity(string action, string details)
    {

        if (string.IsNullOrEmpty(_currentUser)) return;



        try
        {
            var now = DateTime.Now;
            now = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond);
            RunNoResult(conn =>
            {
                using var cmd = CreateCommand(conn, SpActivityAdd);
                AddParameter(cmd, "p_logged_on", now);
                AddParameter(cmd, "p_username", _currentUser);
                AddParameter(cmd, "p_action", action ?? string.Empty);
                AddParameter(cmd, "p_details", details ?? string.Empty);
                cmd.ExecuteNonQuery();
            });
        }
        catch (RepositoryException)
        {

        }
    }

    public IReadOnlyList<ActivityEntry> GetActivity(DateTime day)
    {
        var entries = new List<ActivityEntry>();
        RunNoResult(conn =>
        {
            using var cmd = CreateCommand(conn, SpActivityByDate);
            AddParameter(cmd, "p_day", day.Date);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                entries.Add(new ActivityEntry(
                    GetDate(reader, "logged_on"),
                    GetString(reader, "username"),
                    GetString(reader, "action"),
                    GetString(reader, "details")));
        });
        return entries;
    }


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




    private T Run<T>(Func<DbConnection, T> work)
    {
        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            NormalizeSession(conn);
            return work(conn);
        }
        catch (MySqlException ex)
        {
            throw new RepositoryException(Describe(ex), ex);
        }
    }


    private void RunNoResult(Action<DbConnection> work) =>
        Run<object?>(conn => { work(conn); return null; });


    internal static void NormalizeSession(MySqlConnection conn)
    {
        using var cmd = new MySqlCommand("SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci", conn);
        cmd.ExecuteNonQuery();
    }


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
            reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            (UserRole)Enum.Parse(typeof(UserRole), reader.GetString(4)), reader.GetBoolean(5));
    });

    public UserAccount? GetById(int userId) => Run(conn =>
    {
        using var cmd = Procedure(conn, "sp_user_get");
        cmd.Parameters.AddWithValue("p_user_id", userId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new UserAccount(
            reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            (UserRole)Enum.Parse(typeof(UserRole), reader.GetString(4)), reader.GetBoolean(5));
    });

    public void Create(string username, string fullName, string passwordHash, UserRole role) => Run<object?>(conn =>
    {
        using var cmd = Procedure(conn, "sp_user_add");
        cmd.Parameters.AddWithValue("p_username", username);
        cmd.Parameters.AddWithValue("p_full_name", fullName ?? string.Empty);
        cmd.Parameters.AddWithValue("p_password_hash", passwordHash);
        cmd.Parameters.AddWithValue("p_created_on", DateTime.Now);
        cmd.Parameters.AddWithValue("p_role", role.ToString());
        cmd.ExecuteNonQuery();
        return null;
    });

    public IReadOnlyList<UserAccount> ListUsers() => Run(conn =>
    {
        var list = new List<UserAccount>();
        using var cmd = Procedure(conn, "sp_user_list");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(new UserAccount(
                reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                (UserRole)Enum.Parse(typeof(UserRole), reader.GetString(4)), reader.GetBoolean(5)));
        return list;
    });

    public void Update(int userId, string username, string fullName, UserRole role) => Run<object?>(conn =>
    {
        using var cmd = Procedure(conn, "sp_user_update");
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_username", username);
        cmd.Parameters.AddWithValue("p_full_name", fullName ?? string.Empty);
        cmd.Parameters.AddWithValue("p_role", role.ToString());
        cmd.ExecuteNonQuery();
        return null;
    });

    public void ChangePassword(int userId, string passwordHash) => Run<object?>(conn =>
    {
        using var cmd = Procedure(conn, "sp_user_change_password");
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_password_hash", passwordHash);
        cmd.ExecuteNonQuery();
        return null;
    });

    public void SetArchived(int userId, bool archived) => Run<object?>(conn =>
    {
        using var cmd = Procedure(conn, "sp_user_set_archived");
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_archived", archived ? 1 : 0);
        cmd.ExecuteNonQuery();
        return null;
    });

    public void Delete(int userId) => Run<object?>(conn =>
    {
        using var cmd = Procedure(conn, "sp_user_delete");
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.ExecuteNonQuery();
        return null;
    });

    public LoginThrottle GetLoginThrottle(string username) => Run(conn =>
    {
        using var cmd = Procedure(conn, "sp_login_state");
        cmd.Parameters.AddWithValue("p_username", username);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return new LoginThrottle(0, null);
        return new LoginThrottle(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetDateTime(1));
    });

    public LoginThrottle RecordLoginFailure(string username) => Run(conn =>
    {
        using var cmd = Procedure(conn, "sp_login_fail");
        cmd.Parameters.AddWithValue("p_username", username);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return new LoginThrottle(0, null);
        return new LoginThrottle(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetDateTime(1));
    });

    public void ClearLoginFailures(string username) => Run<object?>(conn =>
    {
        using var cmd = Procedure(conn, "sp_login_clear");
        cmd.Parameters.AddWithValue("p_username", username);
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
            MySqlBarangayRepository.NormalizeSession(conn);
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








public static class PasswordHasher
{
    private const int Iterations = 600_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private static readonly string DummyHash = BuildDummy();

    private static string BuildDummy()
    {
        var salt = new byte[SaltSize];
        var hash = new byte[HashSize];
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

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
        if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations)
            || iterations < 100_000 || iterations > 2_000_000)
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expected = Convert.FromBase64String(parts[2]);
            if (salt.Length != SaltSize || expected.Length != HashSize) return false;
            byte[] actual = Derive(password, salt, iterations);


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

    public static bool VerifyDummy(string password)
    {
        Verify(password, DummyHash);
        return false;
    }

    private static byte[] Derive(string password, byte[] salt, int iterations)
    {
        using var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return kdf.GetBytes(HashSize);
    }
}
