using System;
using System.Collections.Generic;
using System.Linq;
namespace BarangayDocumentSystem.Models;


public record Dependent(string FullName, string Relationship, DateTime? DateOfBirth)
{

    public int? GetAge()
    {
        if (DateOfBirth is null) return null;
        var born = DateOfBirth.Value;
        int age = DateTime.Today.Year - born.Year;
        if (born.Date > DateTime.Today.AddYears(-age)) age--;
        return Math.Max(0, age);
    }
}








public class Resident
{
    private readonly List<DocumentRequest> _requests = new();
    private readonly List<Dependent> _dependents = new();

    public int ResidentId { get; private set; }

    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public CivilStatus CivilStatus { get; set; } = CivilStatus.Single;


    public string Purok { get; set; } = string.Empty;
    public string HouseNumber { get; set; } = string.Empty;

    public string GetAddressText() =>
        string.IsNullOrWhiteSpace(HouseNumber) ? Purok : $"House No. {HouseNumber.Trim()}, {Purok}";

    public string ContactNumber { get; set; } = string.Empty;
    public string Occupation { get; set; } = string.Empty;






    public DateTime DateOfResidency { get; set; }

    public bool IsRegisteredVoter { get; set; }


    public ResidentClassification Classification { get; set; } = ResidentClassification.None;





    public bool HasAvailedFirstTimeJobseeker { get; set; }




    public bool IsArchived { get; internal set; }
    public DateTime? ArchivedOn { get; internal set; }
    public string TransferDestination { get; internal set; } = string.Empty;
    public string TransferReason { get; internal set; } = string.Empty;

    public IReadOnlyList<DocumentRequest> Requests => _requests.AsReadOnly();




    public IReadOnlyList<Dependent> Dependents => _dependents.AsReadOnly();

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


    public string GetClassificationText()
    {
        if (Classification == ResidentClassification.None) return "None";

        var parts = new List<string>();
        if (HasClassification(ResidentClassification.SeniorCitizen)) parts.Add("Senior Citizen");
        if (HasClassification(ResidentClassification.PWD))           parts.Add("PWD");
        if (HasClassification(ResidentClassification.Indigent))      parts.Add("Indigent");

        if (HasClassification(ResidentClassification.SoloParent))    parts.Add("Solo Parent");

        return string.Join(", ", parts);
    }

    internal void AddRequest(DocumentRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (!_requests.Contains(request))
            _requests.Add(request);
    }

    internal void SetDependents(IEnumerable<Dependent>? dependents)
    {
        _dependents.Clear();
        if (dependents is not null)
            _dependents.AddRange(dependents);
    }

    public override string ToString() => GetSortableName();
}
