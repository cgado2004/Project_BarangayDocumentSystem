using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces;

/// <summary>
/// Everything my application can ask of storage.
///
/// My screens only ever talk to this interface, never to a concrete class.
/// That is what lets me swap the in-memory store for a MySQL one by changing
/// a single line in Program.cs, without touching a single form.
///
/// v3.1 adds the focused resident query methods - FindResident and
/// ResidentsOfPurok - so the screens ask the store for what they want
/// instead of filtering the whole registry themselves.
/// </summary>
public interface IBarangayRepository
{
    IReadOnlyList<Resident> Residents { get; }
    IReadOnlyList<DocumentRequest> Requests { get; }

    /// <summary>One resident by id, or null. The history panel and the
    /// request form use this after a grid row is picked.</summary>
    Resident? FindResident(int residentId);

    Resident AddResident(ResidentDetails details);
    void UpdateResident(Resident resident, ResidentDetails details);
    void RemoveResident(Resident resident);

    /// <summary>Free-text search across name, purok, contact and
    /// occupation. A blank term returns everyone.</summary>
    IEnumerable<Resident> SearchResidents(string term);

    /// <summary>Every resident of one purok, for the dashboard chips.</summary>
    IEnumerable<Resident> ResidentsOfPurok(string purok);

    /// <summary>
    /// File a request. The store runs the fee rules itself and writes the
    /// assessment onto the request, so no screen can forget to.
    /// </summary>
    DocumentRequest CreateRequest(
        Resident resident, DocumentType type, string purpose, RequestInput? input = null);

    IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status);

    /// <summary>I call this after a request's status or payment changed, so
    /// the store can write it down. The in-memory version does nothing here
    /// beyond guarding the store's own invariants; the MySQL version runs an
    /// UPDATE.</summary>
    void SaveRequest(DocumentRequest request);

    /// <summary>
    /// True when an official receipt number is already recorded on a
    /// DIFFERENT paid request.
    ///
    /// v3.1.1, adapted from Jonathan Del Rosario's Draft branch: money must
    /// be traceable to exactly one request, so the screens ask the store
    /// before writing a receipt number that is already on file. The SQL
    /// version will back this with the unique index the schema already
    /// plans for paid rows (UX_Requests_Receipt in DBContext/db/01-schema.sql terms).
    /// </summary>
    bool ReceiptNumberExists(string officialReceiptNo, DocumentRequest? excluding = null);

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
