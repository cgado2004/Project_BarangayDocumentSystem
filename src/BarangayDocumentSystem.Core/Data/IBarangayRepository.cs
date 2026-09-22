using BarangayDocumentSystem.Core.Entities;

namespace BarangayDocumentSystem.Core.Data;

/// <summary>
/// Everything my application can ask of storage.
///
/// My screens only ever talk to this interface, never to a concrete class.
/// That is what lets me swap the in-memory store for a MySQL one by changing
/// a single line in Program.cs, without touching a single form.
/// </summary>
public interface IBarangayRepository
{
    IReadOnlyList<Resident> Residents { get; }
    IReadOnlyList<DocumentRequest> Requests { get; }

    Resident AddResident(ResidentDetails details);
    void UpdateResident(Resident resident, ResidentDetails details);
    void RemoveResident(Resident resident);
    IEnumerable<Resident> SearchResidents(string term);

    DocumentRequest CreateRequest(
        Resident resident, DocumentType type, string purpose, ClearanceScope scope);

    IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status);

    /// <summary>I call this after a request's status or payment changed, so
    /// the store can write it down. The in-memory version does nothing here;
    /// the MySQL version runs an UPDATE.</summary>
    void SaveRequest(DocumentRequest request);

    BarangayStatistics GetStatistics();
}

/// <summary>
/// The editable fields of a resident, carried together as one object.
///
/// Without this my AddResident would need fourteen separate parameters, and
/// every caller would have to get their order exactly right. Passing one
/// object means the compiler catches my mistakes instead of the user.
/// </summary>
public record ResidentDetails(
    string FirstName,
    string MiddleName,
    string LastName,
    string Suffix,
    DateTime DateOfBirth,
    Gender Gender,
    CivilStatus CivilStatus,
    string Purok,
    string AddressLine,
    string ContactNumber,
    string Occupation,
    DateTime DateOfResidency,
    bool IsRegisteredVoter,
    ResidentClassification Classification);

/// <summary>Everything my dashboard shows, worked out in one place so the
/// figures can never disagree with each other.</summary>
public record BarangayStatistics(
    int TotalResidents,
    int RegisteredVoters,
    int SeniorCitizens,
    int TotalRequests,
    int Pending,
    int Processing,
    int ReadyForRelease,
    int Released,
    decimal TotalCollected,
    int IssuedFreeOfCharge,
    IReadOnlyDictionary<string, int> RequestsByDocumentType,
    IReadOnlyDictionary<string, int> ResidentsByPurok);
