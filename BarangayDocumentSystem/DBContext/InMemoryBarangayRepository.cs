using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Service;

namespace BarangayDocumentSystem.DBContext;

/// <summary>
/// I hold everything in plain lists in memory.
///
/// This is what I run during a demo. It needs no database, no XAMPP and no
/// setup, and I pre-load it with sample residents and requests so there is
/// something on screen the moment the program starts. The data is lost when
/// the app closes, which is exactly why the MySQL version will implement
/// the same interface - the swap happens in Program.cs and nowhere else.
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

    public Resident? FindResident(int residentId) =>
        _residents.FirstOrDefault(r => r.ResidentId == residentId);

    public Resident AddResident(ResidentDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        var resident = new Resident(_nextResidentId++, details.FirstName, details.LastName);
        Apply(resident, details);
        _residents.Add(resident);
        return resident;
    }

    public void UpdateResident(Resident resident, ResidentDetails details)
    {
        ArgumentNullException.ThrowIfNull(resident);
        ArgumentNullException.ThrowIfNull(details);
        Apply(resident, details);
    }

    public void RemoveResident(Resident resident)
    {
        ArgumentNullException.ThrowIfNull(resident);

        // I delete a resident's requests along with them. If I did not, the
        // request list would be left pointing at somebody who no longer
        // exists, and the app would crash the next time it drew that row.
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

    /// <summary>
    /// I file a request AND price it in the same breath. The assessment is
    /// applied here rather than on any screen, so no screen can forget to
    /// run the fee rules or apply them twice.
    /// </summary>
    public DocumentRequest CreateRequest(
        Resident resident, DocumentType type, string purpose, RequestInput? input = null)
    {
        ArgumentNullException.ThrowIfNull(resident);

        var request = new DocumentRequest(_nextRequestId++, resident, type, purpose, input);
        request.ApplyAssessment(_feeSchedule.Assess(resident, type, request.Input));

        _requests.Add(request);
        resident.AddRequest(request);
        return request;
    }

    public IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status) =>
        status is null ? _requests : _requests.Where(r => r.Status == status.Value);

    /// <summary>
    /// I have almost nothing to do here, and that is on purpose.
    ///
    /// The objects in my lists ARE the storage, so by the time a screen calls
    /// this the change has already happened. I still provide the method so the
    /// MySQL version can implement the same interface, and so the screens can
    /// call it without knowing or caring which store is running underneath.
    ///
    /// v3.1.1: the one thing I DO guard is the uniqueness of an official
    /// receipt number across paid requests, because that is a store
    /// invariant - the kind of rule that must hold no matter who calls.
    /// A receipt number that points at two requests is money I cannot
    /// account for, here or in the database.
    /// </summary>
    public void SaveRequest(DocumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IsPaid && ReceiptNumberExists(request.OfficialReceiptNo, request))
            throw new InvalidOperationException(
                $"Official receipt number {request.OfficialReceiptNo} is already " +
                "recorded on another request. A receipt number identifies exactly one payment.");
    }

    /// <summary>
    /// v3.1.1, adapted from Jonathan Del Rosario's Draft branch (his schema
    /// enforced this with a unique filtered index; in memory I scan). The
    /// comparison ignores case, the way the SQL collation would, so
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

    /// <summary>
    /// My sample residents and requests, using the real purok names of
    /// Magugpo Poblacion.
    ///
    /// I gave each person a different situation on purpose, so that during
    /// the demo I can show every branch of the fee rules - the flat rates,
    /// the two clearance scopes, all four waivers, and every one of the
    /// v3.1 variable-fee documents - without inventing data on the spot in
    /// front of the panel.
    /// </summary>
    private void SeedSampleData()
    {
        // He pays full price. I gave him no exemptions at all.
        var juan = AddResident(new ResidentDetails(
            "Juan", "Perez", "Dela Cruz", "", new DateTime(1985, 4, 12),
            Gender.Male, CivilStatus.Married, "Purok Tandang Sora",
            "123 Rizal Street", "09171234567", "Tricycle Driver",
            new DateTime(2010, 6, 1), true, ResidentClassification.None));

        // A senior citizen, so I can show the RA 9994 waiver.
        var maria = AddResident(new ResidentDetails(
            "Maria", "Santos", "Reyes", "", new DateTime(1955, 9, 3),
            Gender.Female, CivilStatus.Widowed, "Purok Orchids",
            "45 Bonifacio Avenue", "09181234567", "Retired",
            new DateTime(1998, 1, 15), true, ResidentClassification.SeniorCitizen));

        // 14 months resident, so he PASSES my RA 11261 six-month test.
        var jose = AddResident(new ResidentDetails(
            "Jose", "Cruz", "Bautista", "Jr.", new DateTime(2004, 2, 20),
            Gender.Male, CivilStatus.Single, "Purok Sampaguita",
            "78 Mabini Street", "09191234567", "Fresh Graduate",
            DateTime.Today.AddMonths(-14), true, ResidentClassification.None));

        // A solo parent who runs a business. I use her to prove that personal
        // exemptions do not apply to a business clearance.
        var ana = AddResident(new ResidentDetails(
            "Ana", "Lopez", "Villanueva", "", new DateTime(1992, 11, 8),
            Gender.Female, CivilStatus.Single, "Purok Sunflower",
            "12 Quezon Street", "09201234567", "Sari-sari Store Owner",
            new DateTime(2015, 3, 20), true, ResidentClassification.SoloParent));

        // An indigent resident, so I can show that waiver too.
        var pedro = AddResident(new ResidentDetails(
            "Pedro", "Ramos", "Mendoza", "", new DateTime(1978, 7, 25),
            Gender.Male, CivilStatus.Married, "Purok Cristo Rey",
            "90 Magsaysay Street", "09211234567", "Carpenter",
            new DateTime(2005, 8, 10), false, ResidentClassification.Indigent));

        // A hyphenated surname, and TWO classifications at once - this is the
        // resident who justifies my junction table in the database.
        var liza = AddResident(new ResidentDetails(
            "Liza", "Garcia", "Santos-Reyes", "", new DateTime(1999, 5, 30),
            Gender.Female, CivilStatus.Single, "Purok Orchids",
            "56 Del Pilar Street", "09221234567", "Student",
            new DateTime(2019, 6, 1), true,
            ResidentClassification.Student | ResidentClassification.PWD));

        // A surname with ñ, a DITO number, and only 2 months of residency, so
        // he FAILS my RA 11261 test and the request is refused with a reason.
        var carlo = AddResident(new ResidentDetails(
            "Carlo", "Diaz", "Peña", "", new DateTime(2003, 12, 5),
            Gender.Male, CivilStatus.Single, "Purok Lapu-Lapu",
            "34 Luna Street", "08951234567", "Unemployed",
            DateTime.Today.AddMonths(-2), false, ResidentClassification.None));

        // ---- the requests ------------------------------------------------

        // A clearance I took all the way through to release.
        var r1 = CreateRequest(juan, DocumentType.BarangayClearance,
                               "Employment Requirement", ClearanceScope.Local);
        r1.StartProcessing();
        r1.MarkReadyForRelease();
        r1.RecordPayment("OR-2026-00101");
        r1.Release();

        // A senior citizen, waived, and released with no payment at all. I
        // include this row because it proves a free document does not get
        // stuck waiting for a payment that is never going to come.
        var r2 = CreateRequest(maria, DocumentType.CertificateOfResidency,
                               "Pension Claim", ClearanceScope.Local);
        r2.StartProcessing();
        r2.MarkReadyForRelease();
        r2.Release();

        // Free under RA 11261, and I left it still being processed.
        var r3 = CreateRequest(jose, DocumentType.FirstTimeJobseekerCertificate,
                               "NBI Clearance Application", ClearanceScope.Local);
        r3.StartProcessing();

        // Free, and I left it sitting ready to collect.
        var r4 = CreateRequest(pedro, DocumentType.CertificateOfIndigency,
                               "Medical Assistance at Davao Regional Medical Center",
                               ClearanceScope.Local);
        r4.StartProcessing();
        r4.MarkReadyForRelease();

        // ₱500 - the business clearance whose amount VARIES with the law
        // violated, which is the whole reason the v3.1 fee schedule takes
        // an assessed amount. I charge the full ₱500 despite her solo-parent
        // tag, for the reason I gave above.
        CreateRequest(ana, DocumentType.BarangayBusinessClearance,
                      "Sari-sari Store Renewal",
                      new RequestInput(Amount: 500m,
                          Detail: "Barangay Ordinance No. 12-2024, operating beyond the approved business line"));

        // A PWD, so I waive it under RA 10754.
        CreateRequest(liza, DocumentType.CertificateOfGoodMoralCharacter,
                      "Scholarship Application", ClearanceScope.Local);

        // ₱200, because this one is for work abroad - the charter's higher
        // rate, and the reason I needed the scope field at all.
        CreateRequest(carlo, DocumentType.BarangayClearance,
                      "Overseas Employment Requirement", ClearanceScope.Abroad);

        // ---- the v3.1 variable-fee documents ------------------------------

        // The cedula: ₱5 basic + ₱150 additional on ₱150,000 of sworn gross
        // income = ₱155. I paid it and released it, so the printed certificate
        // shows a completed computation.
        var r8 = CreateRequest(juan, DocumentType.CommunityTaxCertificate,
                               "Annual community tax, CY " + DateTime.Now.Year,
                               new RequestInput(GrossAnnualIncome: 150_000m));
        r8.StartProcessing();
        r8.MarkReadyForRelease();
        r8.RecordPayment("OR-2026-00102");
        r8.Release();

        // A Katarungang Pambarangay filing at the flat ₱150.
        CreateRequest(pedro, DocumentType.LuponCaseFiling,
                      "Boundary dispute with the adjacent lot owner");

        // Barangay covered court at ₱200 per hour; two and a half hours is
        // billed as three, so the fee is ₱600.
        CreateRequest(ana, DocumentType.BarangayFacilityRental,
                      "Barangay covered court - birthday program",
                      new RequestInput(Hours: 2.5m,
                          Detail: "Barangay covered court"));

        // An "other processing fee" the Barangay Taripa prices, assessed by
        // the clerk at ₱50 for certified copies.
        CreateRequest(maria, DocumentType.OtherTarifaProcessingFee,
                      "Certified copies of a barangay resolution",
                      new RequestInput(Amount: 50m,
                          Detail: "Certified true copies - 10 pages at ₱5.00"));

        // A first-time jobseeker claiming RA 11261 on the CLEARANCE itself -
        // the law covers both documents - left pending so the demo can walk
        // it through.
        CreateRequest(jose, DocumentType.BarangayClearance,
                      "First local employment application",
                      new RequestInput(Scope: ClearanceScope.Local,
                          ApplyJobseekerWaiver: true));

        // An ordinary certification left ageing in the queue, so the RA 11032
        // highlight in the request list has something to point at on demo day.
        SeedAgedPending(liza, DocumentType.OtherCertification,
                        "Certification for a school requirement",
                        DateTime.Today.AddDays(-6));
    }

    /// <summary>
    /// One request backdated for the demo, so the RA 11032 aging in the
    /// request queue is visible without waiting three working days.
    ///
    /// I rebuild it through Rehydrate rather than fiddling with the filing
    /// date after creation, because DateRequested has a private setter for
    /// a reason: history must not be editable from a screen.
    /// </summary>
    private void SeedAgedPending(Resident resident, DocumentType type,
                                 string purpose, DateTime filedOn)
    {
        var assessment = _feeSchedule.Assess(resident, type, RequestInput.Default);

        var aged = DocumentRequest.Rehydrate(
            _nextRequestId++, resident, type, purpose,
            filedOn, null, RequestStatus.Pending,
            assessment.FinalFee, assessment.Basis, false, "", "");

        _requests.Add(aged);
        resident.AddRequest(aged);
    }
}
