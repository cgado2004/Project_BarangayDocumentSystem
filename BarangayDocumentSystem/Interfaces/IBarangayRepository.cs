// =====================================================================
//  PART:    Interfaces - the storage contract every screen depends on
//  ORIGIN:  leader_draft - Clint Wood Gado (v3.1 contract)
//           Fdraft - Frent Dhieniel Raborar (Reload, the RepositoryException rule)
//  EDITS:   Clint Wood Gado - merged the two contracts; added StorageDescription
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces;

/// <summary>
/// Everything my application can ask of storage.
///
/// My screens only ever talk to this interface, never to a concrete class.
/// That is what lets Program.cs choose between Frent's MySQL repository and
/// my in-memory one by changing a single line, without touching a form.
///
/// I merged two versions of this contract. Mine had the focused resident
/// queries (FindResident, ResidentsOfPurok) and the RequestInput that my
/// v3.1 fee schedule needs. Frent's had Reload, and the rule that any
/// method may throw <see cref="RepositoryException"/> when the database
/// cannot be reached or rejects a write. Both survive here.
/// </summary>
public interface IBarangayRepository
{
    IReadOnlyList<Resident> Residents { get; }
    IReadOnlyList<DocumentRequest> Requests { get; }

    /// <summary>
    /// Where the data actually is, in words fit for the status bar:
    /// "MySQL - localhost/barangay_db" or "In-memory demo - nothing is
    /// saved". I put this on the interface so the shell can say it
    /// without asking which concrete class it was given.
    /// </summary>
    string StorageDescription { get; }

    /// <summary>One resident by id, or null. The history panel and the
    /// request form use this after a grid row is picked.</summary>
    Resident? FindResident(int residentId);

    Resident AddResident(ResidentDetails details);
    void UpdateResident(Resident resident, ResidentDetails details);
    void RemoveResident(Resident resident);

    /// <summary>Free-text search across name, purok, contact and
    /// occupation. A blank term returns everyone.</summary>
    IEnumerable<Resident> SearchResidents(string term);

    /// <summary>Every resident of one purok, for the dashboard table.</summary>
    IEnumerable<Resident> ResidentsOfPurok(string purok);

    /// <summary>
    /// File a request. The store runs the fee rules itself and writes the
    /// assessment onto the request, so no screen can forget to.
    /// </summary>
    DocumentRequest CreateRequest(
        Resident resident, DocumentType type, string purpose, RequestInput? input = null);

    IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status);

    /// <summary>
    /// I call this after a request's status or payment changed, so the
    /// store can write it down. The workflow methods on DocumentRequest
    /// change the object in memory only; this is what makes it permanent.
    /// The in-memory version does nothing here; the MySQL version runs an
    /// UPDATE inside a transaction.
    /// </summary>
    void SaveRequest(DocumentRequest request);

    BarangayStatistics GetStatistics();

    /// <summary>
    /// Discard everything held in memory and read it again from storage.
    ///
    /// Frent's rule, and I kept it: after a failed save the screen must
    /// never show something the database does not have, so the view
    /// reloads instead of guessing which half of the change went through.
    /// </summary>
    void Reload();
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
