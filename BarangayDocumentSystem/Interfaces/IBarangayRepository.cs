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
    IReadOnlyList<Resident> Residents { get; }
    IReadOnlyList<DocumentRequest> Requests { get; }

    Resident AddResident(ResidentDetails details);
    void UpdateResident(Resident resident, ResidentDetails details);
    void RemoveResident(Resident resident);
    IEnumerable<Resident> SearchResidents(string term);

    DocumentRequest CreateRequest(Resident resident, DocumentType type, string purpose);

    /// <summary>
    /// Writes a request's current state (status, payment, remarks) to storage.
    /// Call it after any workflow change such as StartProcessing, Release,
    /// RecordPayment or Reject — those change the object in memory only.
    /// </summary>
    void SaveRequest(DocumentRequest request);
    IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status);

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
