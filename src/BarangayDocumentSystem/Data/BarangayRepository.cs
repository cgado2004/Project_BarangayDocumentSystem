using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Data;

/// <summary>
/// In-memory data store for the barangay.
///
/// Everything goes through the model's own constructors and methods, so the
/// business rules hold regardless of what the UI does. Swap this for a MySQL
/// data access layer later and no form needs to change.
/// </summary>
public class BarangayRepository
{
    private readonly List<Resident> _residents = new();
    private readonly List<DocumentRequest> _requests = new();
    private readonly FeeSchedule _feeSchedule = new();

    private int _nextResidentId = 1;
    private int _nextRequestId = 1;

    public IReadOnlyList<Resident> Residents => _residents.AsReadOnly();
    public IReadOnlyList<DocumentRequest> Requests => _requests.AsReadOnly();
    public FeeSchedule FeeSchedule => _feeSchedule;

    public BarangayRepository() => SeedSampleData();

    // -----------------------------------------------------------------
    //  Residents
    // -----------------------------------------------------------------
    public Resident AddResident(string firstName, string middleName, string lastName,
                                string suffix, DateTime dob, Gender gender,
                                CivilStatus civilStatus, string purok, string addressLine,
                                string contactNumber, string occupation,
                                DateTime dateOfResidency, bool isVoter,
                                ResidentClassification classification)
    {
        var resident = new Resident(_nextResidentId++, firstName, lastName)
        {
            MiddleName      = middleName,
            Suffix          = suffix,
            DateOfBirth     = dob,
            Gender          = gender,
            CivilStatus     = civilStatus,
            Purok           = purok,
            AddressLine     = addressLine,
            ContactNumber   = contactNumber,
            Occupation      = occupation,
            DateOfResidency = dateOfResidency,
            IsRegisteredVoter = isVoter,
            Classification  = classification
        };

        _residents.Add(resident);
        return resident;
    }

    public void RemoveResident(Resident resident)
    {
        // Requests are historical records — remove them alongside the resident
        // so no orphan request can point at a deleted person.
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
    //  Document requests
    // -----------------------------------------------------------------
    /// <summary>
    /// Files a request and assesses its fee immediately, so the clerk can tell
    /// the resident what is owed before anything is processed.
    /// </summary>
    public DocumentRequest CreateRequest(Resident resident, DocumentType type, string purpose)
    {
        ArgumentNullException.ThrowIfNull(resident);

        var request = new DocumentRequest(_nextRequestId++, resident, type, purpose);

        FeeAssessment assessment = _feeSchedule.Assess(resident, type);
        request.Fee = assessment.FinalFee;
        request.FeeBasis = assessment.Basis;

        _requests.Add(request);
        resident.AddRequest(request);

        return request;
    }

    public IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status) =>
        status is null
            ? _requests.OrderByDescending(r => r.DateRequested)
            : _requests.Where(r => r.Status == status)
                       .OrderByDescending(r => r.DateRequested);

    // -----------------------------------------------------------------
    //  Statistics for the dashboard
    // -----------------------------------------------------------------
    public int TotalResidents => _residents.Count;
    public int TotalRequests => _requests.Count;
    public int PendingCount => _requests.Count(r => r.Status == RequestStatus.Pending);
    public int ProcessingCount => _requests.Count(r => r.Status == RequestStatus.Processing);
    public int ReadyCount => _requests.Count(r => r.Status == RequestStatus.ReadyForRelease);
    public int ReleasedCount => _requests.Count(r => r.Status == RequestStatus.Released);

    public decimal TotalCollected =>
        _requests.Where(r => r.IsPaid).Sum(r => r.Fee);

    public int SeniorCitizenCount =>
        _residents.Count(r => r.HasClassification(ResidentClassification.SeniorCitizen));

    public int VoterCount => _residents.Count(r => r.IsRegisteredVoter);

    public int WaivedCount =>
        _requests.Count(r => r.Fee == 0 && r.Status == RequestStatus.Released);

    // -----------------------------------------------------------------
    //  Sample data — Barangay Magugpo Poblacion, Tagum City
    // -----------------------------------------------------------------
    private void SeedSampleData()
    {
        var today = DateTime.Today;

        var juan = AddResident("Juan", "Perez", "Dela Cruz", "",
            new DateTime(1985, 4, 12), Gender.Male, CivilStatus.Married,
            "Purok 1", "123 Rizal Street", "09171234567", "Tricycle Driver",
            new DateTime(2010, 6, 1), true, ResidentClassification.None);

        var maria = AddResident("Maria", "Santos", "Reyes", "",
            new DateTime(1955, 9, 3), Gender.Female, CivilStatus.Widowed,
            "Purok 2", "45 Bonifacio Avenue", "09181234567", "Retired",
            new DateTime(1998, 1, 15), true, ResidentClassification.SeniorCitizen);

        // 6+ months' residency and never availed — qualifies under RA 11261.
        var jose = AddResident("Jose", "Cruz", "Bautista", "Jr.",
            new DateTime(2004, 2, 20), Gender.Male, CivilStatus.Single,
            "Purok 3", "78 Mabini Street", "09191234567", "Fresh Graduate",
            today.AddMonths(-14), true, ResidentClassification.None);

        var ana = AddResident("Ana", "Lopez", "Villanueva", "",
            new DateTime(1992, 11, 8), Gender.Female, CivilStatus.Single,
            "Purok 4", "12 Quezon Street", "09201234567", "Sari-sari Store Owner",
            new DateTime(2015, 3, 20), true, ResidentClassification.SoloParent);

        var pedro = AddResident("Pedro", "Ramos", "Mendoza", "",
            new DateTime(1978, 7, 25), Gender.Male, CivilStatus.Married,
            "Purok 5", "90 Magsaysay Street", "09211234567", "Carpenter",
            new DateTime(2005, 8, 10), false, ResidentClassification.Indigent);

        var liza = AddResident("Liza", "Garcia", "Torres", "",
            new DateTime(1999, 5, 30), Gender.Female, CivilStatus.Single,
            "Purok 2", "56 Del Pilar Street", "09221234567", "Student",
            new DateTime(2019, 6, 1), true,
            ResidentClassification.Student | ResidentClassification.PWD);

        // Recently moved in — does NOT yet meet the RA 11261 six-month test.
        AddResident("Carlo", "Diaz", "Aquino", "",
            new DateTime(2003, 12, 5), Gender.Male, CivilStatus.Single,
            "Purok 1", "34 Luna Street", "09231234567", "Unemployed",
            today.AddMonths(-2), false, ResidentClassification.None);

        // --- Requests in various states ---
        var r1 = CreateRequest(juan, DocumentType.BarangayClearance, "Employment Requirement");
        r1.StartProcessing();
        r1.MarkReadyForRelease();
        r1.RecordPayment("OR-2026-00101");
        r1.Release();

        var r2 = CreateRequest(maria, DocumentType.CertificateOfResidency, "Pension Claim");
        r2.StartProcessing();
        r2.MarkReadyForRelease();
        r2.Release();   // senior citizen — no fee, so no payment needed

        var r3 = CreateRequest(jose, DocumentType.FirstTimeJobseekerCertificate,
            "NBI Clearance Application");
        r3.StartProcessing();

        var r4 = CreateRequest(pedro, DocumentType.CertificateOfIndigency,
            "Medical Assistance at Davao Regional Medical Center");
        r4.StartProcessing();
        r4.MarkReadyForRelease();

        CreateRequest(ana, DocumentType.BarangayBusinessClearance,
            "Sari-sari Store Renewal");

        CreateRequest(liza, DocumentType.CertificateOfGoodMoralCharacter,
            "Scholarship Application");
    }
}
