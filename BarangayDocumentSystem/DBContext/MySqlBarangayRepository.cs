using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Service;

namespace BarangayDocumentSystem.DBContext;

/// <summary>
/// The real database store — the class Program.cs has been promising since
/// v3.1, built exactly in the slot <c>CreateRepository()</c> left open.
///
/// v3.2.0. Adapted for our MySQL schema from the reviewed persistence design
/// in docs/05 §9 (itself a review of Jonathan F. Del Rosario's SQL work on
/// the Draft branch, and of the MySQL implementation Clint Gado landed on
/// Draft2). The contract comes from that design, unchanged:
///
///   * the app ALWAYS starts. If MySQL cannot be reached, my constructor
///     throws once, Program explains it and falls back to the in-memory
///     store — the "mandatory connection string at startup" behaviour of
///     the Draft branch was deliberately rejected by the review;
///   * enum columns store their NAMES (Gender, CivilStatus, Status, Scope,
///     DocumentType), so a reordered C# enum can never silently re-mean an
///     old row — that is decision 2 in docs/05 §3 and DBContext/db/01-schema.sql;
///   * a fresh database seeds itself with the SAME sample residents and
///     requests the in-memory store seeds, guarded by an app_state flag
///     taken under GET_LOCK(), so two laptops provisioning at once cannot
///     double-seed (the one provision lock idea kept from the Draft design);
///   * the screens still only ever see IBarangayRepository.
///
/// HOW I KEEP IT CORRECT (read this before "simplifying" it)
///
/// Reads come from two in-memory lists that mirror the tables; every write
/// goes to MySQL first and then updates those lists. At classroom scale
/// (dozens of residents) this is instantaneous, and it means the search,
/// the statistics and every other read behave IDENTICALLY to the in-memory
/// store — same code, same ordering, same rules. What MySQL buys us is
/// persistence: close the app tonight, reopen tomorrow, everything is still
/// there. The trade-off, written down on purpose: this is a single-
/// workstation store. Two clerks on two PCs writing at once would each see
/// their own copy until restart. That is a V3.3 conversation, not a V3.2
/// bug — the barangay hall runs one window today.
/// </summary>
public sealed class MySqlBarangayRepository : IBarangayRepository
{
    private readonly string _connectionString;
    private readonly FeeSchedule _feeSchedule;
    private readonly List<Resident> _residents = new();
    private readonly List<DocumentRequest> _requests = new();

    public MySqlBarangayRepository(string connectionString, FeeSchedule feeSchedule)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("The BarangayDb connection string is empty.", nameof(connectionString));
        _feeSchedule = feeSchedule ?? throw new ArgumentNullException(nameof(feeSchedule));
        _connectionString = connectionString;

        ProvisionAndLoad();
    }

    // ------------------------------------------------------------------
    //  STARTUP — provision, seed once, load
    // ------------------------------------------------------------------

    /// <summary>
    /// One connection, in order: make sure the app_state table exists, take
    /// the provision lock, seed a fresh database exactly once, then load
    /// everything into my lists. Any failure in here propagates — Program
    /// catches it, explains it, and falls back to the in-memory store, which
    /// is the docs/05 §9 contract. I would rather fail loudly here than
    /// half-start against a database I cannot actually read.
    /// </summary>
    private void ProvisionAndLoad()
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();   // throws here = "database offline" for Program

        EnsureAppStateTable(connection);

        // The provisioning lock from the reviewed design: two machines
        // starting at the same time must not both seed. Ten seconds is a
        // generous wait for a local XAMPP server.
        long gotLock;
        using (var lockCommand = new MySqlCommand(
            "SELECT GET_LOCK('barangay_magugpo_provision', 10);", connection))
        {
            gotLock = Convert.ToInt64(lockCommand.ExecuteScalar());
        }
        if (gotLock != 1)
        {
            // Somebody else holds it. Wait it out by simply loading — if
            // their seed is still running my read may see an empty registry,
            // which the restart fixes. Losing this race is rare and benign.
            LoadAll(connection);
            return;
        }

        try
        {
            if (!SamplesAlreadyLoaded(connection))
            {
                using var transaction = connection.BeginTransaction();
                SeedSampleData(connection, transaction);
                MarkSamplesLoaded(connection, transaction);
                transaction.Commit();
            }

            LoadAll(connection);
        }
        finally
        {
            using var release = new MySqlCommand(
                "SELECT RELEASE_LOCK('barangay_magugpo_provision');", connection);
            release.ExecuteNonQuery();
        }
    }

    private static void EnsureAppStateTable(MySqlConnection connection)
    {
        using var command = new MySqlCommand(@"
                CREATE TABLE IF NOT EXISTS app_state (
                    state_key    VARCHAR(40)  NOT NULL,
                    state_value  VARCHAR(200) NOT NULL,
                    updated_at   TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP
                                              ON UPDATE CURRENT_TIMESTAMP,
                    PRIMARY KEY (state_key)
                ) ENGINE = InnoDB;", connection);
        command.ExecuteNonQuery();
    }

    private static bool SamplesAlreadyLoaded(MySqlConnection connection)
    {
        using var command = new MySqlCommand(
            "SELECT state_value FROM app_state WHERE state_key = 'samples_loaded';", connection);
        return command.ExecuteScalar() is not null;
    }

    private static void MarkSamplesLoaded(MySqlConnection connection, MySqlTransaction transaction)
    {
        using var command = new MySqlCommand(
            "INSERT INTO app_state (state_key, state_value) VALUES ('samples_loaded', '1');",
            connection, transaction);
        command.ExecuteNonQuery();
    }

    // ------------------------------------------------------------------
    //  RESIDENTS — the reads come from the cache, the writes go to MySQL
    // ------------------------------------------------------------------

    public IReadOnlyList<Resident> Residents => _residents.AsReadOnly();

    public IReadOnlyList<DocumentRequest> Requests => _requests.AsReadOnly();

    public Resident? FindResident(int residentId) =>
        _residents.FirstOrDefault(r => r.ResidentId == residentId);

    public Resident AddResident(ResidentDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = new MySqlCommand(@"
                INSERT INTO residents
                    (first_name, middle_name, last_name, suffix,
                     date_of_birth, gender, civil_status, purok, address_line,
                     contact_number, occupation, date_of_residency,
                     is_registered_voter, has_availed_jobseeker)
                VALUES
                    (@first, @middle, @last, @suffix,
                     @birth, @gender, @civil, @purok, @address,
                     @contact, @occupation, @residency,
                     @voter, @availed);
                SELECT LAST_INSERT_ID();", connection);

        BindResidentColumns(command, details.FirstName, details.LastName, details,
            hasAvailedJobseeker: false);   // a NEW resident has not availed it
        int id = Convert.ToInt32(command.ExecuteScalar());

        var resident = new Resident(id, details.FirstName, details.LastName);
        Apply(resident, details);
        WriteClassifications(connection, id, details.Classification);
        _residents.Add(resident);
        return resident;
    }

    public void UpdateResident(Resident resident, ResidentDetails details)
    {
        ArgumentNullException.ThrowIfNull(resident);
        ArgumentNullException.ThrowIfNull(details);

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = new MySqlCommand(@"
                UPDATE residents SET
                    first_name = @first, middle_name = @middle, last_name = @last,
                    suffix = @suffix, date_of_birth = @birth, gender = @gender,
                    civil_status = @civil, purok = @purok, address_line = @address,
                    contact_number = @contact, occupation = @occupation,
                    date_of_residency = @residency, is_registered_voter = @voter,
                    has_availed_jobseeker = @availed
                WHERE resident_id = @id;", connection);

        BindResidentColumns(command, details.FirstName, details.LastName, details,
            resident.HasAvailedFirstTimeJobseeker);   // edits must not erase the RA 11261 record
        command.Parameters.AddWithValue("@id", resident.ResidentId);
        command.ExecuteNonQuery();

        Apply(resident, details);
        WriteClassifications(connection, resident.ResidentId, details.Classification);
    }

    public void RemoveResident(Resident resident)
    {
        ArgumentNullException.ThrowIfNull(resident);

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        // The schema's fk_request_resident is ON DELETE CASCADE, so the
        // resident's requests are removed by the database itself — the same
        // rule RemoveResident() has always enforced in memory.
        using var command = new MySqlCommand(
            "DELETE FROM residents WHERE resident_id = @id;", connection);
        command.Parameters.AddWithValue("@id", resident.ResidentId);
        command.ExecuteNonQuery();

        _requests.RemoveAll(r => r.Resident.ResidentId == resident.ResidentId);
        _residents.Remove(resident);
    }

    public IEnumerable<Resident> SearchResidents(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return _residents;

        term = term.Trim();
        return _residents.Where(r =>
            r.GetFullName().Contains(term, StringComparison.OrdinalIgnoreCase) ||
            r.GetSortableName().Contains(term, StringComparison.OrdinalIgnoreCase) ||
            r.Purok.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            r.ContactNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            r.Occupation.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<Resident> ResidentsOfPurok(string purok) =>
        _residents.Where(r => r.Purok.Equals(purok, StringComparison.OrdinalIgnoreCase));

    // ------------------------------------------------------------------
    //  REQUESTS
    // ------------------------------------------------------------------

    /// <summary>
    /// Files a request AND prices it in the same breath, exactly like the
    /// in-memory store — the assessment is applied here so no screen can
    /// forget the fee rules or apply them twice. The difference is that the
    /// row goes to MySQL in the same move, so the request survives a restart.
    /// </summary>
    public DocumentRequest CreateRequest(
        Resident resident, DocumentType type, string purpose, RequestInput? input = null)
    {
        ArgumentNullException.ThrowIfNull(resident);

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        return CreateRequestCore(connection, null, resident, type, purpose, input);
    }

    /// <summary>
    /// The shared filing path. The seeder calls me inside its provisioning
    /// transaction (transaction not null); the screens call the public
    /// CreateRequest, which runs outside any transaction.
    /// </summary>
    private DocumentRequest CreateRequestCore(
        MySqlConnection connection, MySqlTransaction? transaction,
        Resident resident, DocumentType type, string purpose, RequestInput? input)
    {
        var effectiveInput = input ?? RequestInput.Default;

        // I build the C# object first so the fee rules run on it exactly as
        // they do in memory, then I write the row with the assessed values —
        // the database receives the SAME fee and basis the screen will show.
        var drafted = new DocumentRequest(0, resident, type, purpose, effectiveInput);
        drafted.ApplyAssessment(_feeSchedule.Assess(resident, type, drafted.Input));

        using var command = new MySqlCommand(@"
                INSERT INTO document_requests
                    (resident_id, document_type, scope, purpose,
                     date_requested, status, fee, fee_basis,
                     assessed_amount, hours_of_use, declared_income, fee_detail,
                     availed_jobseeker_act)
                VALUES
                    (@resident, @type, @scope, @purpose,
                     @requested, 'Pending', @fee, @basis,
                     @assessed, @hours, @income, @detail,
                     @availed);
                SELECT LAST_INSERT_ID();", connection, transaction);

        command.Parameters.AddWithValue("@resident", resident.ResidentId);
        command.Parameters.AddWithValue("@type", type.ToString());
        command.Parameters.AddWithValue("@scope", drafted.Scope.ToString());
        command.Parameters.AddWithValue("@purpose", purpose);
        command.Parameters.AddWithValue("@requested", drafted.DateRequested);
        command.Parameters.AddWithValue("@fee", drafted.Fee);
        command.Parameters.AddWithValue("@basis", NullIfEmpty(drafted.FeeBasis));
        BindOptionalMoney(command, "@assessed", effectiveInput.Amount);
        BindOptionalMoney(command, "@hours", effectiveInput.Hours);
        BindOptionalMoney(command, "@income", effectiveInput.GrossAnnualIncome);
        command.Parameters.AddWithValue("@detail", NullIfEmpty(effectiveInput.Detail));
        command.Parameters.AddWithValue("@availed", drafted.AvailedUnderJobseekerAct);

        int id = Convert.ToInt32(command.ExecuteScalar());

        // Rehydrate() is the loading door on DocumentRequest: it stamps the
        // real database id onto a request that has already been priced,
        // without replaying any status rules against a brand-new row.
        var request = DocumentRequest.Rehydrate(
            id, resident, type, purpose, drafted.DateRequested, null,
            RequestStatus.Pending, drafted.Fee, drafted.FeeBasis,
            isPaid: false, officialReceiptNo: string.Empty, remarks: string.Empty,
            input: effectiveInput, availedUnderJobseekerAct: drafted.AvailedUnderJobseekerAct);

        _requests.Add(request);
        resident.AddRequest(request);
        return request;
    }

    public IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status) =>
        status is null ? _requests : _requests.Where(r => r.Status == status.Value);

    /// <summary>
    /// Writes a request's state down after a screen changed it — the UPDATE
    /// the interface comment promised. The receipt-uniqueness guard matches
    /// the in-memory store exactly: a receipt number identifies exactly one
    /// payment, and I refuse the write before it happens.
    /// </summary>
    public void SaveRequest(DocumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IsPaid && ReceiptNumberExists(request.OfficialReceiptNo, request))
            throw new InvalidOperationException(
                $"Official receipt number {request.OfficialReceiptNo} is already " +
                "recorded on another request. A receipt number identifies exactly one payment.");

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = new MySqlCommand(@"
                UPDATE document_requests SET
                    status = @status,
                    date_released = @released,
                    fee = @fee,
                    fee_basis = @basis,
                    is_paid = @paid,
                    official_receipt_no = @receipt,
                    remarks = @remarks,
                    availed_jobseeker_act = @availed
                WHERE request_id = @id;", connection);

        command.Parameters.AddWithValue("@status", request.Status.ToString());
        command.Parameters.AddWithValue("@released",
            request.DateReleased ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@fee", request.Fee);
        command.Parameters.AddWithValue("@basis", NullIfEmpty(request.FeeBasis));
        command.Parameters.AddWithValue("@paid", request.IsPaid);
        command.Parameters.AddWithValue("@receipt", NullIfEmpty(request.OfficialReceiptNo));
        command.Parameters.AddWithValue("@remarks", NullIfEmpty(request.Remarks));
        command.Parameters.AddWithValue("@availed", request.AvailedUnderJobseekerAct);
        command.Parameters.AddWithValue("@id", request.RequestId);
        command.ExecuteNonQuery();

        // Release() marks the resident when a request consumed the once-only
        // RA 11261 benefit. That flag lives on the residents table, so I
        // write it down here as well — this is what stops a second
        // certificate surviving an app restart.
        using var residentCommand = new MySqlCommand(
            "UPDATE residents SET has_availed_jobseeker = @availed WHERE resident_id = @id;",
            connection);
        residentCommand.Parameters.AddWithValue("@availed",
            request.Resident.HasAvailedFirstTimeJobseeker);
        residentCommand.Parameters.AddWithValue("@id", request.Resident.ResidentId);
        residentCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// v3.1.1's rule, unchanged: one receipt number, one payment. The
    /// comparison ignores case the way the SQL collation would, so
    /// "OR-101" and "or-101" count as the same receipt.
    /// </summary>
    public bool ReceiptNumberExists(string officialReceiptNo, DocumentRequest? excluding = null)
    {
        if (string.IsNullOrWhiteSpace(officialReceiptNo)) return false;

        string candidate = officialReceiptNo.Trim();
        return _requests.Any(other =>
            !ReferenceEquals(other, excluding) &&
            other.IsPaid &&
            other.OfficialReceiptNo.Equals(candidate, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The same figures as the in-memory store, from the same code shape —
    /// which is the whole point of the cached design: my dashboard can never
    /// disagree with itself across the two stores.
    /// </summary>
    public BarangayStatistics GetStatistics()
    {
        var byType = _requests
            .GroupBy(r => FeeSchedule.NameOf(r.DocumentType))
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        var byPurok = _residents
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Purok) ? "(unassigned)" : r.Purok)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        return new BarangayStatistics(
            _residents.Count,
            _residents.Count(r => r.IsRegisteredVoter),
            _residents.Count(r => r.HasClassification(ResidentClassification.SeniorCitizen)),
            _requests.Count,
            _requests.Count(r => r.Status == RequestStatus.Pending),
            _requests.Count(r => r.Status == RequestStatus.Processing),
            _requests.Count(r => r.Status == RequestStatus.ReadyForRelease),
            _requests.Count(r => r.Status == RequestStatus.Released),
            _requests.Where(r => r.IsPaid).Sum(r => r.Fee),
            _requests.Count(r => r.Fee == 0 && r.Status == RequestStatus.Released),
            byType,
            byPurok);
    }

    // ------------------------------------------------------------------
    //  LOADING — tables into the cache
    // ------------------------------------------------------------------

    private void LoadAll(MySqlConnection connection)
    {
        _residents.Clear();
        _requests.Clear();

        var classifications = new Dictionary<int, ResidentClassification>();

        using (var command = new MySqlCommand(@"
                SELECT resident_id, first_name, middle_name, last_name, suffix,
                       date_of_birth, gender, civil_status, purok, address_line,
                       contact_number, occupation, date_of_residency,
                       is_registered_voter, has_availed_jobseeker
                FROM residents
                ORDER BY resident_id;", connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                int id = reader.GetInt32("resident_id");
                var resident = new Resident(
                    id, reader.GetString("first_name"), reader.GetString("last_name"))
                {
                    MiddleName                = ReadText(reader, "middle_name"),
                    Suffix                    = ReadText(reader, "suffix"),
                    DateOfBirth               = reader.GetDateTime("date_of_birth"),
                    Gender                    = ParseEnum<Gender>(reader.GetString("gender")),
                    CivilStatus               = ParseEnum<CivilStatus>(reader.GetString("civil_status")),
                    Purok                     = reader.GetString("purok"),
                    AddressLine               = ReadText(reader, "address_line"),
                    ContactNumber             = ReadText(reader, "contact_number"),
                    Occupation                = ReadText(reader, "occupation"),
                    DateOfResidency           = reader.GetDateTime("date_of_residency"),
                    IsRegisteredVoter         = reader.GetBoolean("is_registered_voter"),
                    HasAvailedFirstTimeJobseeker = reader.GetBoolean("has_availed_jobseeker"),
                    Classification            = ResidentClassification.None
                };
                _residents.Add(resident);
            }
        }

        // The junction table fills the classification flags — the exact
        // reverse of what WriteClassifications writes, one row per tag.
        using (var command = new MySqlCommand(
            "SELECT resident_id, classification_code FROM resident_classifications;", connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                int id = reader.GetInt32("resident_id");
                classifications[id] =
                    classifications.GetValueOrDefault(id) | ClassificationFlag(reader.GetString("classification_code"));
            }
        }
        foreach (Resident resident in _residents)
            resident.Classification = classifications.GetValueOrDefault(resident.ResidentId);

        using (var command = new MySqlCommand(@"
                SELECT request_id, resident_id, document_type, scope, purpose,
                       date_requested, date_released, status, fee, fee_basis,
                       assessed_amount, hours_of_use, declared_income, fee_detail,
                       availed_jobseeker_act, is_paid, official_receipt_no, remarks
                FROM document_requests
                ORDER BY request_id;", connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var resident = _residents.FirstOrDefault(r =>
                    r.ResidentId == reader.GetInt32("resident_id"));
                if (resident is null) continue;   // an orphan row must not kill the app

                var input = new RequestInput(
                    Scope:            ParseEnum<ClearanceScope>(reader.GetString("scope")),
                    Amount:           ReadOptionalMoney(reader, "assessed_amount"),
                    Hours:            ReadOptionalMoney(reader, "hours_of_use"),
                    GrossAnnualIncome: ReadOptionalMoney(reader, "declared_income"),
                    Detail:           ReadText(reader, "fee_detail"));

                var request = DocumentRequest.Rehydrate(
                    reader.GetInt32("request_id"),
                    resident,
                    ParseEnum<DocumentType>(reader.GetString("document_type")),
                    reader.GetString("purpose"),
                    reader.GetDateTime("date_requested"),
                    reader.IsDBNull(reader.GetOrdinal("date_released"))
                        ? null : reader.GetDateTime("date_released"),
                    ParseEnum<RequestStatus>(reader.GetString("status")),
                    reader.GetDecimal("fee"),
                    ReadText(reader, "fee_basis"),
                    reader.GetBoolean("is_paid"),
                    ReadText(reader, "official_receipt_no"),
                    ReadText(reader, "remarks"),
                    input,
                    reader.GetBoolean("availed_jobseeker_act"));

                _requests.Add(request);
                resident.AddRequest(request);
            }
        }
    }

    // ------------------------------------------------------------------
    //  SEEDING — the same demo the in-memory store plants
    // ------------------------------------------------------------------

    /// <summary>
    /// The seed you already know: the same seven residents of Magugpo
    /// Poblacion and the same fee-rule walkthrough as the in-memory store,
    /// written into MySQL instead of into lists. Once, under the provision
    /// lock, flagged in app_state — so re-running never duplicates it.
    /// </summary>
    private void SeedSampleData(MySqlConnection connection, MySqlTransaction transaction)
    {
        Resident NewResident(ResidentDetails details) =>
            AddResidentCore(connection, transaction, details);

        var juan = NewResident(new ResidentDetails(
            "Juan", "Perez", "Dela Cruz", "", new DateTime(1985, 4, 12),
            Gender.Male, CivilStatus.Married, "Purok Tandang Sora",
            "123 Rizal Street", "09171234567", "Tricycle Driver",
            new DateTime(2010, 6, 1), true, ResidentClassification.None));

        var maria = NewResident(new ResidentDetails(
            "Maria", "Santos", "Reyes", "", new DateTime(1955, 9, 3),
            Gender.Female, CivilStatus.Widowed, "Purok Orchids",
            "45 Bonifacio Avenue", "09181234567", "Retired",
            new DateTime(1998, 1, 15), true, ResidentClassification.SeniorCitizen));

        var jose = NewResident(new ResidentDetails(
            "Jose", "Cruz", "Bautista", "Jr.", new DateTime(2004, 2, 20),
            Gender.Male, CivilStatus.Single, "Purok Sampaguita",
            "78 Mabini Street", "09191234567", "Fresh Graduate",
            DateTime.Today.AddMonths(-14), true, ResidentClassification.None));

        var ana = NewResident(new ResidentDetails(
            "Ana", "Lopez", "Villanueva", "", new DateTime(1992, 11, 8),
            Gender.Female, CivilStatus.Single, "Purok Sunflower",
            "12 Quezon Street", "09201234567", "Sari-sari Store Owner",
            new DateTime(2015, 3, 20), true, ResidentClassification.SoloParent));

        var pedro = NewResident(new ResidentDetails(
            "Pedro", "Ramos", "Mendoza", "", new DateTime(1978, 7, 25),
            Gender.Male, CivilStatus.Married, "Purok Cristo Rey",
            "90 Magsaysay Street", "09211234567", "Carpenter",
            new DateTime(2005, 8, 10), false, ResidentClassification.Indigent));

        var liza = NewResident(new ResidentDetails(
            "Liza", "Garcia", "Santos-Reyes", "", new DateTime(1999, 5, 30),
            Gender.Female, CivilStatus.Single, "Purok Orchids",
            "56 Del Pilar Street", "09221234567", "Student",
            new DateTime(2019, 6, 1), true,
            ResidentClassification.Student | ResidentClassification.PWD));

        var carlo = NewResident(new ResidentDetails(
            "Carlo", "Diaz", "Peña", "", new DateTime(2003, 12, 5),
            Gender.Male, CivilStatus.Single, "Purok Lapu-Lapu",
            "34 Luna Street", "08951234567", "Unemployed",
            DateTime.Today.AddMonths(-2), false, ResidentClassification.None));

        // The requests, filed through the same CreateRequestCore the screens
        // use, walked through the same transitions, and written down with the
        // same SaveRequestCore — so a seeded database and a used database are
        // indistinguishable, which is exactly what the demo needs.
        var r1 = CreateRequestCore(connection, transaction, juan,
            DocumentType.BarangayClearance, "Employment Requirement", null);
        r1.StartProcessing();
        r1.MarkReadyForRelease();
        r1.RecordPayment("OR-2026-00101");
        r1.Release();
        SaveRequestCore(connection, transaction, r1);

        var r2 = CreateRequestCore(connection, transaction, maria,
            DocumentType.CertificateOfResidency, "Pension Claim", null);
        r2.StartProcessing();
        r2.MarkReadyForRelease();
        r2.Release();
        SaveRequestCore(connection, transaction, r2);

        var r3 = CreateRequestCore(connection, transaction, jose,
            DocumentType.FirstTimeJobseekerCertificate, "NBI Clearance Application", null);
        r3.StartProcessing();
        SaveRequestCore(connection, transaction, r3);

        var r4 = CreateRequestCore(connection, transaction, pedro,
            DocumentType.CertificateOfIndigency,
            "Medical Assistance at Davao Regional Medical Center", null);
        r4.StartProcessing();
        r4.MarkReadyForRelease();
        SaveRequestCore(connection, transaction, r4);

        CreateRequestCore(connection, transaction, ana,
            DocumentType.BarangayBusinessClearance, "Sari-sari Store Renewal",
            new RequestInput(Amount: 500m,
                Detail: "Barangay Ordinance No. 12-2024, operating beyond the approved business line"));

        CreateRequestCore(connection, transaction, liza,
            DocumentType.CertificateOfGoodMoralCharacter, "Scholarship Application", null);

        CreateRequestCore(connection, transaction, carlo,
            DocumentType.BarangayClearance, "Overseas Employment Requirement",
            new RequestInput(Scope: ClearanceScope.Abroad));

        var r8 = CreateRequestCore(connection, transaction, juan,
            DocumentType.CommunityTaxCertificate,
            "Annual community tax, CY " + DateTime.Now.Year,
            new RequestInput(GrossAnnualIncome: 150_000m));
        r8.StartProcessing();
        r8.MarkReadyForRelease();
        r8.RecordPayment("OR-2026-00102");
        r8.Release();
        SaveRequestCore(connection, transaction, r8);

        CreateRequestCore(connection, transaction, pedro,
            DocumentType.LuponCaseFiling, "Boundary dispute with the adjacent lot owner", null);

        CreateRequestCore(connection, transaction, ana,
            DocumentType.BarangayFacilityRental, "Barangay covered court - birthday program",
            new RequestInput(Hours: 2.5m, Detail: "Barangay covered court"));

        CreateRequestCore(connection, transaction, maria,
            DocumentType.OtherTarifaProcessingFee, "Certified copies of a barangay resolution",
            new RequestInput(Amount: 50m, Detail: "Certified true copies - 10 pages at ₱5.00"));

        CreateRequestCore(connection, transaction, jose,
            DocumentType.BarangayClearance, "First local employment application",
            new RequestInput(Scope: ClearanceScope.Local, ApplyJobseekerWaiver: true));
    }

    /// <summary>
    /// AddResident's seeder twin: same INSERT, but it joins the provisioning
    /// transaction and updates the cache afterwards. The public AddResident
    /// is this without the transaction.
    /// </summary>
    private Resident AddResidentCore(
        MySqlConnection connection, MySqlTransaction transaction, ResidentDetails details)
    {
        using var command = new MySqlCommand(@"
                INSERT INTO residents
                    (first_name, middle_name, last_name, suffix,
                     date_of_birth, gender, civil_status, purok, address_line,
                     contact_number, occupation, date_of_residency,
                     is_registered_voter, has_availed_jobseeker)
                VALUES
                    (@first, @middle, @last, @suffix,
                     @birth, @gender, @civil, @purok, @address,
                     @contact, @occupation, @residency,
                     @voter, @availed);
                SELECT LAST_INSERT_ID();", connection, transaction);

        BindResidentColumns(command, details.FirstName, details.LastName, details,
            hasAvailedJobseeker: false);
        int id = Convert.ToInt32(command.ExecuteScalar());

        var resident = new Resident(id, details.FirstName, details.LastName);
        Apply(resident, details);
        WriteClassifications(connection, id, details.Classification, transaction);
        _residents.Add(resident);
        return resident;
    }

    /// <summary>
    /// SaveRequest's seeder twin: same UPDATE, inside the provisioning
    /// transaction, so a seeded row that was released-and-paid lands in one
    /// commit or not at all.
    /// </summary>
    private static void SaveRequestCore(
        MySqlConnection connection, MySqlTransaction transaction, DocumentRequest request)
    {
        using var command = new MySqlCommand(@"
                UPDATE document_requests SET
                    status = @status,
                    date_released = @released,
                    fee = @fee,
                    fee_basis = @basis,
                    is_paid = @paid,
                    official_receipt_no = @receipt,
                    remarks = @remarks,
                    availed_jobseeker_act = @availed
                WHERE request_id = @id;", connection, transaction);

        command.Parameters.AddWithValue("@status", request.Status.ToString());
        command.Parameters.AddWithValue("@released",
            request.DateReleased ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@fee", request.Fee);
        command.Parameters.AddWithValue("@basis", NullIfEmpty(request.FeeBasis));
        command.Parameters.AddWithValue("@paid", request.IsPaid);
        command.Parameters.AddWithValue("@receipt", NullIfEmpty(request.OfficialReceiptNo));
        command.Parameters.AddWithValue("@remarks", NullIfEmpty(request.Remarks));
        command.Parameters.AddWithValue("@availed", request.AvailedUnderJobseekerAct);
        command.Parameters.AddWithValue("@id", request.RequestId);
        command.ExecuteNonQuery();
    }

    // ------------------------------------------------------------------
    //  SMALL HELPERS
    // ------------------------------------------------------------------

    /// <summary>The editable fields, applied — the in-memory store's private
    /// Apply, kept identical on purpose so both stores shape a resident the
    /// same way.</summary>
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

    /// <summary>The residents columns every INSERT and UPDATE shares. The
    /// name and id parameters are passed separately because the INSERT
    /// writes them from the details while the UPDATE also needs the id.
    /// The jobseeker flag is a parameter too, and this is important: a NEW
    /// resident has not availed the benefit, but an EDITED one already
    /// might have — writing a blind false here would erase RA 11261's
    /// once-only record every time somebody fixed a typo in their name.</summary>
    private static void BindResidentColumns(
        MySqlCommand command, string firstName, string lastName,
        ResidentDetails details, bool hasAvailedJobseeker)
    {
        command.Parameters.AddWithValue("@first", firstName);
        command.Parameters.AddWithValue("@middle", NullIfEmpty(details.MiddleName));
        command.Parameters.AddWithValue("@last", lastName);
        command.Parameters.AddWithValue("@suffix", NullIfEmpty(details.Suffix));
        command.Parameters.AddWithValue("@birth", details.DateOfBirth);
        command.Parameters.AddWithValue("@gender", details.Gender.ToString());
        command.Parameters.AddWithValue("@civil", details.CivilStatus.ToString());
        command.Parameters.AddWithValue("@purok", details.Purok);
        command.Parameters.AddWithValue("@address", NullIfEmpty(details.AddressLine));
        command.Parameters.AddWithValue("@contact", NullIfEmpty(details.ContactNumber));
        command.Parameters.AddWithValue("@occupation", NullIfEmpty(details.Occupation));
        command.Parameters.AddWithValue("@residency", details.DateOfResidency);
        command.Parameters.AddWithValue("@voter", details.IsRegisteredVoter);
        command.Parameters.AddWithValue("@availed", hasAvailedJobseeker);
    }

    /// <summary>
    /// The classification junction, rewritten as one delete plus the current
    /// tags. Five tags at classroom scale — two statements beat a diff.
    /// </summary>
    private void WriteClassifications(
        MySqlConnection connection, int residentId,
        ResidentClassification classification, MySqlTransaction? transaction = null)
    {
        using (var delete = new MySqlCommand(
            "DELETE FROM resident_classifications WHERE resident_id = @id;",
            connection, transaction))
        {
            delete.Parameters.AddWithValue("@id", residentId);
            delete.ExecuteNonQuery();
        }

        foreach (ResidentClassification flag in new[]
                     {
                         ResidentClassification.SeniorCitizen,
                         ResidentClassification.PWD,
                         ResidentClassification.Indigent,
                         ResidentClassification.Student,
                         ResidentClassification.SoloParent
                     })
        {
            if (!classification.HasFlag(flag)) continue;

            using var insert = new MySqlCommand(
                "INSERT INTO resident_classifications (resident_id, classification_code) " +
                "VALUES (@id, @code);", connection, transaction);
            insert.Parameters.AddWithValue("@id", residentId);
            insert.Parameters.AddWithValue("@code", ClassificationCode(flag));
            insert.ExecuteNonQuery();
        }
    }

    /// <summary>C# flag to the schema's classification_code. The codes live
    /// in classification_types, seeded by DBContext/db/01-schema.sql itself.</summary>
    private static string ClassificationCode(ResidentClassification flag) => flag switch
    {
        ResidentClassification.SeniorCitizen => "SENIOR_CITIZEN",
        ResidentClassification.PWD           => "PWD",
        ResidentClassification.Indigent      => "INDIGENT",
        ResidentClassification.Student       => "STUDENT",
        ResidentClassification.SoloParent    => "SOLO_PARENT",
        _ => throw new ArgumentException("No database code for the classification " + flag)
    };

    /// <summary>The reverse mapping, for the rows coming back out.</summary>
    private static ResidentClassification ClassificationFlag(string code) => code switch
    {
        "SENIOR_CITIZEN" => ResidentClassification.SeniorCitizen,
        "PWD"            => ResidentClassification.PWD,
        "INDIGENT"       => ResidentClassification.Indigent,
        "STUDENT"        => ResidentClassification.Student,
        "SOLO_PARENT"    => ResidentClassification.SoloParent,
        _ => ResidentClassification.None   // an unknown code rides along harmlessly
    };

    /// <summary>Enum columns store NAMES, so I parse by name — case-blind,
    /// because the database is not the place to enforce capitalisation.</summary>
    private static T ParseEnum<T>(string name) where T : struct, Enum =>
        Enum.TryParse<T>(name, ignoreCase: true, out var value)
            ? value
            : throw new InvalidOperationException(
                $"The database holds '{name}', which is not a {typeof(T).Name} this build knows. " +
                "Was the schema edited without updating the app?");

    private static string ReadText(MySqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column))
            ? string.Empty
            : reader.GetString(column);

    /// <summary>The v3.1 variable-fee columns are NULL for flat-rate
    /// documents; a NULL becomes zero, which is what RequestInput means by
    /// "not specified".</summary>
    private static decimal ReadOptionalMoney(MySqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? 0m : reader.GetDecimal(column);

    private static void BindOptionalMoney(
        MySqlCommand command, string name, decimal value)
    {
        command.Parameters.AddWithValue(name,
            value == 0m ? DBNull.Value : value);   // zero and "not stated" are the same thing to the fee rules
    }

    /// <summary>Empty strings store as NULL, the way the schema's own
    /// comments prefer it, and NULLs read back as empty strings.</summary>
    private static object NullIfEmpty(string text) =>
        string.IsNullOrEmpty(text) ? DBNull.Value : text;
}
