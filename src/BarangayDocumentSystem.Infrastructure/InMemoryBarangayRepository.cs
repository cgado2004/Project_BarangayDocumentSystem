using BarangayDocumentSystem.Domain.Abstractions;
using BarangayDocumentSystem.Domain.Entities;
using BarangayDocumentSystem.Domain.Services;
namespace BarangayDocumentSystem.Infrastructure;

/// <summary>
/// In-memory implementation of <see cref="IBarangayRepository"/>.
///
/// ── DEPENDENCY INVERSION ────────────────────────────────────────────────
/// This class lives in Infrastructure and implements an interface owned by
/// Domain. The dependency arrow points INWARD: Infrastructure → Domain.
/// Domain knows nothing about it.
///
/// A MySqlBarangayRepository would sit beside this file, implement the same
/// interface, and be selected by changing one line in Program.cs.
/// </summary>
public class InMemoryBarangayRepository : IBarangayRepository
{
    private readonly List<Resident> _residents = new();
    private readonly List<DocumentRequest> _requests = new();
    private readonly FeeSchedule _feeSchedule;

    private int _nextResidentId = 1;
    private int _nextRequestId = 1;

    public IReadOnlyList<Resident> Residents => _residents.AsReadOnly();
    public IReadOnlyList<DocumentRequest> Requests => _requests.AsReadOnly();

    public InMemoryBarangayRepository(FeeSchedule feeSchedule)
    {
        _feeSchedule = feeSchedule ?? throw new ArgumentNullException(nameof(feeSchedule));
        SeedSampleData();
    }


    public class InvalidNameCharactersException : Exception
    {
        public string FieldName { get; }
        public string Value { get; }

        public InvalidNameCharactersException(string fieldName, string value)
            : base($"'{fieldName}' contains a character that isn't allowed in a name: \"{value}\".")
        {
            FieldName = fieldName;
            Value = value;
        }
    }

    // -----------------------------------------------------------------
    //  Residents
    // -----------------------------------------------------------------
    public Resident AddResident(ResidentDetails d)
    {
        var resident = new Resident(_nextResidentId++, d.FirstName, d.LastName);
        Apply(resident, d);
        _residents.Add(resident);
        return resident;
    }

    public void UpdateResident(Resident resident, ResidentDetails d)
    {
        ArgumentNullException.ThrowIfNull(resident);
        Apply(resident, d);
    }

    /// <summary>
    /// ── DRY ─────────────────────────────────────────────────────────────
    /// Add and Update both funnel through this. Previously the fourteen field
    /// assignments were written out twice — once in the repository, once again
    /// in MainForm's edit branch — and had to be kept in sync by hand.
    /// </summary>
    private static void Apply(Resident r, ResidentDetails d)
    {

        ValidateNameCharacters(nameof(d.FirstName), d.FirstName);
        ValidateNameCharacters(nameof(d.MiddleName), d.MiddleName);
        ValidateNameCharacters(nameof(d.LastName), d.LastName);

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

    /// <summary>
    /// ── VALIDATION ──────────────────────────────────────────────────────
    /// Letters, spaces, hyphens, and apostrophes are allowed since they show
    /// up in real names; anything else (digits, symbols, etc.) raises
    /// InvalidNameCharactersException as a warning to the caller.
    /// </summary>
    private static void ValidateNameCharacters(string fieldName, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        bool hasSpecialCharacter = value.Any(c =>
            !char.IsLetter(c) && !char.IsWhiteSpace(c) && c != '-' && c != '\'');

        if (hasSpecialCharacter)
            throw new InvalidNameCharactersException(fieldName, value);
    }

    public void RemoveResident(Resident resident)
    {
        _requests.RemoveAll(r => r.Resident == resident);
        _residents.Remove(resident);
    }

    public IEnumerable<Resident> SearchResidents(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return _residents.OrderBy(r => r.LastName).ThenBy(r => r.FirstName);

        return _residents
            .Where(r => r.GetFullName().Contains(term, StringComparison.OrdinalIgnoreCase)
                     || r.Purok.Contains(term, StringComparison.OrdinalIgnoreCase)
                     || r.ContactNumber.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName);
    }

    // -----------------------------------------------------------------
    //  Requests
    // -----------------------------------------------------------------
    public DocumentRequest CreateRequest(Resident resident, DocumentType type, string purpose)
    {
        ArgumentNullException.ThrowIfNull(resident);

        var request = new DocumentRequest(_nextRequestId++, resident, type, purpose);

        var assessment = _feeSchedule.Assess(resident, type);
        request.Fee = assessment.FinalFee;
        request.FeeBasis = assessment.Basis;

        _requests.Add(request);
        resident.AddRequest(request);
        return request;
    }

    public IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status) =>
        status is null
            ? _requests.OrderByDescending(r => r.DateRequested)
            : _requests.Where(r => r.Status == status).OrderByDescending(r => r.DateRequested);

    // -----------------------------------------------------------------
    //  Statistics — computed in one pass, returned as one object (DRY)
    // -----------------------------------------------------------------
    public BarangayStatistics GetStatistics() => new(
        TotalResidents:  _residents.Count,
        RegisteredVoters: _residents.Count(r => r.IsRegisteredVoter),
        SeniorCitizens:  _residents.Count(r => r.HasClassification(ResidentClassification.SeniorCitizen)),
        TotalRequests:   _requests.Count,
        Pending:         _requests.Count(r => r.Status == RequestStatus.Pending),
        Processing:      _requests.Count(r => r.Status == RequestStatus.Processing),
        ReadyForRelease: _requests.Count(r => r.Status == RequestStatus.ReadyForRelease),
        Released:        _requests.Count(r => r.Status == RequestStatus.Released),
        TotalCollected:  _requests.Where(r => r.IsPaid).Sum(r => r.Fee),
        IssuedFreeOfCharge: _requests.Count(r => r.Fee == 0 && r.Status == RequestStatus.Released),
        RequestsByDocumentType: _requests.GroupBy(r => r.GetDocumentName())
                                         .ToDictionary(g => g.Key, g => g.Count()),
        ResidentsByPurok: _residents.GroupBy(r => r.Purok)
                                    .OrderBy(g => g.Key)
                                    .ToDictionary(g => g.Key, g => g.Count()));

    // -----------------------------------------------------------------
        private void SeedSampleData()
    {
        var today = DateTime.Today;

        Resident Add(string fn, string mn, string ln, string sfx, DateTime dob, Gender g,
                     CivilStatus cs, string purok, string addr, string contact, string occ,
                     DateTime since, bool voter, ResidentClassification cls) =>
            AddResident(new ResidentDetails(fn, mn, ln, sfx, dob, g, cs, purok, addr,
                                            contact, occ, since, voter, cls));

        var juan = Add("Juan", "Perez", "Dela Cruz", "", new DateTime(1985, 4, 12),
            Gender.Male, CivilStatus.Married, "Purok 1", "123 Rizal Street",
            "09171234567", "Tricycle Driver", new DateTime(2010, 6, 1), true,
            ResidentClassification.None);

        var maria = Add("Maria", "Santos", "Reyes", "", new DateTime(1955, 9, 3),
            Gender.Female, CivilStatus.Widowed, "Purok 2", "45 Bonifacio Avenue",
            "09181234567", "Retired", new DateTime(1998, 1, 15), true,
            ResidentClassification.SeniorCitizen);

        // 14 months' residency, never availed — qualifies under RA 11261.
        var jose = Add("Jose", "Cruz", "Bautista", "Jr.", new DateTime(2004, 2, 20),
            Gender.Male, CivilStatus.Single, "Purok 3", "78 Mabini Street",
            "09191234567", "Fresh Graduate", today.AddMonths(-14), true,
            ResidentClassification.None);

        var ana = Add("Ana", "Lopez", "Villanueva", "", new DateTime(1992, 11, 8),
            Gender.Female, CivilStatus.Single, "Purok 4", "12 Quezon Street",
            "09201234567", "Sari-sari Store Owner", new DateTime(2015, 3, 20), true,
            ResidentClassification.SoloParent);

        var pedro = Add("Pedro", "Ramos", "Mendoza", "", new DateTime(1978, 7, 25),
            Gender.Male, CivilStatus.Married, "Purok 5", "90 Magsaysay Street",
            "09211234567", "Carpenter", new DateTime(2005, 8, 10), false,
            ResidentClassification.Indigent);

        var liza = Add("Liza", "Garcia", "Torres", "", new DateTime(1999, 5, 30),
            Gender.Female, CivilStatus.Single, "Purok 2", "56 Del Pilar Street",
            "09221234567", "Student", new DateTime(2019, 6, 1), true,
            ResidentClassification.Student | ResidentClassification.PWD);

        // Only 2 months — deliberately FAILS the RA 11261 six-month test.
        Add("Carlo", "Diaz", "Aquino", "", new DateTime(2003, 12, 5),
            Gender.Male, CivilStatus.Single, "Purok 1", "34 Luna Street",
            "09231234567", "Unemployed", today.AddMonths(-2), false,
            ResidentClassification.None);

        var r1 = CreateRequest(juan, DocumentType.BarangayClearance, "Employment Requirement");
        r1.StartProcessing(); r1.MarkReadyForRelease();
        r1.RecordPayment("OR-2026-00101"); r1.Release();

        var r2 = CreateRequest(maria, DocumentType.CertificateOfResidency, "Pension Claim");
        r2.StartProcessing(); r2.MarkReadyForRelease(); r2.Release();

        var r3 = CreateRequest(jose, DocumentType.FirstTimeJobseekerCertificate,
            "NBI Clearance Application");
        r3.StartProcessing();

        var r4 = CreateRequest(pedro, DocumentType.CertificateOfIndigency,
            "Medical Assistance at Davao Regional Medical Center");
        r4.StartProcessing(); r4.MarkReadyForRelease();

        CreateRequest(ana, DocumentType.BarangayBusinessClearance, "Sari-sari Store Renewal");
        CreateRequest(liza, DocumentType.CertificateOfGoodMoralCharacter, "Scholarship Application");
    }
}
