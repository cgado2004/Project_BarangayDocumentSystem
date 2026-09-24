namespace BarangayDocumentSystem.Models;

/// <summary>
/// A resident of Barangay Magugpo Poblacion, Tagum City.
///
/// Resident 1 ──── 0..* DocumentRequest  (association)
///
/// I modelled this as an association rather than a composition because a
/// resident exists perfectly well without any request, and because a
/// request is a historical record. If someone corrects a resident's
/// address next year, that must not quietly rewrite a certificate issued
/// last year.
/// </summary>
public class Resident
{
    private readonly List<DocumentRequest> _requests = new();

    public int ResidentId { get; private set; }

    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;      // Jr., Sr., III

    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public CivilStatus CivilStatus { get; set; } = CivilStatus.Single;

    /// <summary>The purok this resident lives in. I offer only the real
    /// puroks of Magugpo Poblacion, so the value cannot be mistyped.</summary>
    public string Purok { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;

    public string ContactNumber { get; set; } = string.Empty;
    public string Occupation { get; set; } = string.Empty;

    /// <summary>
    /// When the person began residing in the barangay. I use this to run
    /// the six-month residency test RA 11261 requires before I can issue
    /// a first-time jobseeker certificate.
    /// </summary>
    public DateTime DateOfResidency { get; set; }

    public bool IsRegisteredVoter { get; set; }

    /// <summary>A bitwise combination, because one resident can be a senior AND
    /// an indigent at the same time and I have to honour both.</summary>
    public ResidentClassification Classification { get; set; } = ResidentClassification.None;

    /// <summary>
    /// RA 11261 may be availed only ONCE in a person's life - and it covers
    /// not just the jobseeker certificate but the barangay clearance a
    /// first-time jobseeker asks for. I set this when such a document is
    /// released, and I check it before I ever issue another.
    /// </summary>
    public bool HasAvailedFirstTimeJobseeker { get; set; }

    public IReadOnlyList<DocumentRequest> Requests => _requests.AsReadOnly();

    public Resident(int residentId, string firstName, string lastName)
    {
        ResidentId = residentId;
        FirstName = firstName;
        LastName = lastName;
        DateOfResidency = DateTime.Today;
    }

    /// <summary>
    /// The middle initial, as " P.", or nothing at all when there is no
    /// usable middle name.
    ///
    /// I look for the first actual LETTER, so punctuation at the start
    /// cannot leak onto an official document - a middle name of "." once
    /// printed as "Juan .. Dela Cruz" before I fixed this.
    /// </summary>
    private string MiddleInitial()
    {
        if (string.IsNullOrWhiteSpace(MiddleName)) return string.Empty;

        foreach (char c in MiddleName)
            if (char.IsLetter(c)) return $" {char.ToUpperInvariant(c)}.";

        return string.Empty;
    }

    public string GetFullName()
    {
        string suffix = string.IsNullOrWhiteSpace(Suffix) ? string.Empty : $" {Suffix}";
        return $"{FirstName}{MiddleInitial()} {LastName}{suffix}".Trim();
    }

    /// <summary>"Dela Cruz, Juan P." - the form I use when I need the list in
    /// alphabetical order.</summary>
    public string GetSortableName()
    {
        return $"{LastName}, {FirstName}{MiddleInitial()}".Trim();
    }

    public int GetAge()
    {
        if (DateOfBirth == default) return 0;

        int age = DateTime.Today.Year - DateOfBirth.Year;
        if (DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
        return age;
    }

    /// <summary>Whole months of continuous residency. I use this for the
    /// RA 11261 six-month test.</summary>
    public int GetMonthsOfResidency()
    {
        var today = DateTime.Today;
        int months = ((today.Year - DateOfResidency.Year) * 12)
                     + today.Month - DateOfResidency.Month;

        if (today.Day < DateOfResidency.Day) months--;
        return Math.Max(0, months);
    }

    public bool HasClassification(ResidentClassification c) =>
        Classification.HasFlag(c) && c != ResidentClassification.None;

    /// <summary>"Senior Citizen, PWD" - the readable version I show on screen
    /// instead of a raw enum value.</summary>
    public string GetClassificationText()
    {
        if (Classification == ResidentClassification.None) return "None";

        var parts = new List<string>();
        if (HasClassification(ResidentClassification.SeniorCitizen)) parts.Add("Senior Citizen");
        if (HasClassification(ResidentClassification.PWD))           parts.Add("PWD");
        if (HasClassification(ResidentClassification.Indigent))      parts.Add("Indigent");
        if (HasClassification(ResidentClassification.Student))       parts.Add("Student");
        if (HasClassification(ResidentClassification.SoloParent))    parts.Add("Solo Parent");

        return string.Join(", ", parts);
    }

    internal void AddRequest(DocumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_requests.Contains(request))
            _requests.Add(request);
    }

    public override string ToString() => GetSortableName();
}
