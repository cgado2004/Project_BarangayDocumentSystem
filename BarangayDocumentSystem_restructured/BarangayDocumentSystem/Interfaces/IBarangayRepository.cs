using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces;

/// <summary>
/// Storage contract for residents and their document requests.
///
/// ── DEPENDENCY INVERSION PRINCIPLE ──────────────────────────────────────
/// Before: MainForm did `new BarangayRepository()`. The UI was welded to one
/// concrete storage class — you could not swap in MySQL, and you could not
/// test a form without dragging the whole in-memory store along.
///
/// Now the UI depends on THIS INTERFACE, which lives in the Domain project.
/// Both the UI and the concrete store depend on the abstraction; neither
/// depends on the other. That is the inversion.
///
/// Swapping to MySQL becomes: write MySqlBarangayRepository : IBarangayRepository,
/// change one line in Program.cs. No form changes at all.
/// </summary>
public interface IBarangayRepository
{
    IReadOnlyList<Resident> Residents { get; }
    IReadOnlyList<DocumentRequest> Requests { get; }

    Resident AddResident(ResidentDetails details);
    void UpdateResident(Resident resident, ResidentDetails details);
    void RemoveResident(Resident resident);
    IEnumerable<Resident> SearchResidents(string term);

    DocumentRequest CreateRequest(Resident resident, DocumentType type, string purpose);
    IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status);

    BarangayStatistics GetStatistics();
}

/// <summary>
/// Carries the editable fields of a resident between the UI and the store.
///
/// ── DRY ─────────────────────────────────────────────────────────────────
/// Before, AddResident took FOURTEEN positional parameters, and MainForm then
/// repeated all fourteen assignments again by hand for the edit case. Two
/// near-identical blocks that had to be kept in sync by memory.
///
/// One record type replaces both. Add a field here and Add + Update both
/// pick it up automatically.
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

/// <summary>
/// Dashboard figures, computed in one place.
///
/// ── DRY ─────────────────────────────────────────────────────────────────
/// Before, the repository exposed eleven separate computed properties and the
/// form read each one individually. One object now carries them all.
/// </summary>
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
