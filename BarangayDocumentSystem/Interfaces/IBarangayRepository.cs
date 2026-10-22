using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces;

/// <summary>
/// Storage contract for residents and their document requests.
///
/// The views and forms depend on THIS interface, never on a concrete class.
/// The only implementation today is <c>MySqlBarangayRepository</c> (Database
/// folder); Program.cs is the one place that names it.
///
/// Any method may throw <see cref="RepositoryException"/> when the database
/// cannot be reached or rejects the request.
/// </summary>
public interface IBarangayRepository
{
    /// <summary>Every resident, active AND archived.</summary>
    IReadOnlyList<Resident> Residents { get; }
    IReadOnlyList<DocumentRequest> Requests { get; }

    Resident AddResident(ResidentDetails details);
    void UpdateResident(Resident resident, ResidentDetails details);

    /// <summary>
    /// Moves a resident to the archive because they transferred out of the
    /// barangay. Nothing is deleted — the record and request history are kept.
    /// </summary>
    void ArchiveResident(Resident resident, string destination, string reason);

    /// <summary>Brings an archived resident back to the active registry.</summary>
    void RestoreResident(Resident resident);

    /// <summary>Active residents only (the Residents page and new requests).</summary>
    IEnumerable<Resident> SearchResidents(string term);

    /// <summary>Archived (transferred) residents only.</summary>
    IEnumerable<Resident> SearchArchivedResidents(string term);

    /// <param name="isStudent">The clerk confirmed the student discount for this request.</param>
    DocumentRequest CreateRequest(Resident resident, DocumentType type, string purpose, bool isStudent = false);

    /// <summary>
    /// Writes a request's current state (status, payment, remarks) to storage.
    /// Call it after any workflow change such as StartProcessing, Release,
    /// RecordPayment or Reject — those change the object in memory only.
    /// </summary>
    void SaveRequest(DocumentRequest request);
    IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status);

    /// <summary>
    /// The next free official receipt number, e.g. OR-20261022-3: today's date plus
    /// a counter that starts at 1 each day and goes up with every payment.
    /// </summary>
    string GetNextOfficialReceiptNo();

    BarangayStatistics GetStatistics();

    /// <summary>
    /// Discards everything held in memory and reloads it from storage. Used to
    /// recover after a failed save so the screen never shows data the
    /// database does not have.
    /// </summary>
    void Reload();
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
    string HouseNumber,
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

/// <summary>A staff account that can sign in to the system.</summary>
public record UserAccount(int UserId, string Username, string FullName, string PasswordHash);

/// <summary>
/// Storage contract for staff accounts. Kept apart from
/// <see cref="IBarangayRepository"/> because accounts are not barangay data.
/// Any method may throw <see cref="RepositoryException"/>.
/// </summary>
public interface IUserRepository
{
    /// <summary>False on a brand-new database — the first account is then created from the login screen.</summary>
    bool AnyUsers();

    UserAccount? Find(string username);

    /// <summary>Stores a new account. The caller passes an already-hashed password.</summary>
    void Create(string username, string fullName, string passwordHash);
}
