using System;
using System.Collections.Generic;
namespace BarangayDocumentSystem.Models;

/// <summary>
/// A resident of Barangay Magugpo Poblacion, Tagum City.
///
/// Resident 1 ──── 0..* DocumentRequest  (association)
/// A resident exists independently of any request, and requests are historical
/// records that outlive changes to the resident's details.
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

    /// <summary>Purok / sitio / street within Magugpo Poblacion.</summary>
    public string Purok { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;

    public string ContactNumber { get; set; } = string.Empty;
    public string Occupation { get; set; } = string.Empty;

    /// <summary>
    /// When the person began residing in the barangay. Drives the six-month
    /// residency test that RA 11261 requires for a first-time jobseeker
    /// certificate.
    /// </summary>
    public DateTime DateOfResidency { get; set; }

    public bool IsRegisteredVoter { get; set; }

    /// <summary>Bitwise combination — a resident can be senior AND indigent.</summary>
    public ResidentClassification Classification { get; set; } = ResidentClassification.None;

    /// <summary>
    /// RA 11261 may be availed only ONCE. Set when a first-time jobseeker
    /// certificate is released, and checked before issuing another.
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

    public string GetFullName()
    {
        string middle = string.IsNullOrWhiteSpace(MiddleName)
            ? string.Empty
            : $" {MiddleName[0]}.";

        string suffix = string.IsNullOrWhiteSpace(Suffix) ? string.Empty : $" {Suffix}";
        return $"{FirstName}{middle} {LastName}{suffix}".Trim();
    }

    /// <summary>"Dela Cruz, Juan P." — for alphabetical listings.</summary>
    public string GetSortableName()
    {
        string middle = string.IsNullOrWhiteSpace(MiddleName)
            ? string.Empty
            : $" {MiddleName[0]}.";
        return $"{LastName}, {FirstName}{middle}".Trim();
    }

    public int GetAge()
    {
        if (DateOfBirth == default) return 0;

        int age = DateTime.Today.Year - DateOfBirth.Year;
        if (DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
        return age;
    }

    /// <summary>Whole months of continuous residency, used by the RA 11261 test.</summary>
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

    /// <summary>"Senior Citizen, PWD" — for display.</summary>
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
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (!_requests.Contains(request))
            _requests.Add(request);
    }

    public override string ToString() => GetSortableName();
}
