// =====================================================================
//  PART:    Database - the shared skeleton of every repository
//  ORIGIN:  leader_draft - Clint Wood Gado
//           (the queries and Apply were duplicated in my in-memory store and
//            in Frent's MySQL store; I pulled the single copy up into here)
//  EDITS:   Clint Wood Gado - new file for v3.2
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Database;

/// <summary>
/// The part of a repository that is the same no matter where the rows live.
///
/// WHY THIS CLASS EXISTS
/// When I put Frent's MySQL repository next to my in-memory one, more than
/// half of each file was the other file: the same working-set lists, the
/// same Apply, the same search, the same statistics, and two different
/// copies of the same seven sample residents. Two copies means two places
/// to fix the next bug, and they had already drifted (his search ignored
/// occupation; mine did not order by name). That is the textbook DRY
/// violation, so the shared part now lives here exactly once.
///
/// HOW IT IS SHAPED (Template Method)
/// Every command has the same skeleton: check the arguments, persist the
/// change, then mirror it in the working set that the screens read from.
/// This class owns the skeleton; the storage-specific step is an abstract
/// "hook" - InsertResident, UpdateRequestRow and so on - that each subclass
/// fills in. The in-memory hooks hand out ids from a counter and do nothing
/// else. Frent's MySQL hooks run his parameterised SQL. Neither subclass
/// can forget to mirror a change or forget to price a request, because
/// they never get to write that part.
///
/// Every query is answered from the working set, which is why the queries
/// need no hook at all: both storages hold the same lists once they are
/// loaded.
/// </summary>
public abstract class RepositoryBase : IBarangayRepository
{
    private readonly List<Resident> _residents = new();
    private readonly List<DocumentRequest> _requests = new();

    protected RepositoryBase(FeeSchedule fees)
    {
        Fees = fees ?? throw new ArgumentNullException(nameof(fees));
    }

    /// <summary>The one fee schedule every request is priced with.</summary>
    protected FeeSchedule Fees { get; }

    public IReadOnlyList<Resident> Residents => _residents.AsReadOnly();
    public IReadOnlyList<DocumentRequest> Requests => _requests.AsReadOnly();

    public abstract string StorageDescription { get; }

    // -----------------------------------------------------------------
    //  Queries. Written once, here, because both storages answer them
    //  from the same working set.
    // -----------------------------------------------------------------

    public Resident? FindResident(int residentId) =>
        _residents.FirstOrDefault(r => r.ResidentId == residentId);

    public IEnumerable<Resident> SearchResidents(string term)
    {
        // I sort by surname either way, so the list does not jump around
        // between "everyone" and "everyone matching a letter".
        if (string.IsNullOrWhiteSpace(term))
            return _residents.OrderBy(r => r.LastName).ThenBy(r => r.FirstName);

        term = term.Trim();
        return _residents
            .Where(r => Contains(r.GetFullName(), term)
                     || Contains(r.GetSortableName(), term)
                     || Contains(r.Purok, term)
                     || Contains(r.ContactNumber, term)
                     || Contains(r.Occupation, term))
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName);
    }

    public IEnumerable<Resident> ResidentsOfPurok(string purok) =>
        _residents.Where(r => r.Purok.Equals(purok, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status)
    {
        IEnumerable<DocumentRequest> rows =
            status is null ? _requests : _requests.Where(r => r.Status == status.Value);

        // Newest first: the request a clerk is looking for is almost always
        // the one that was just filed.
        return rows.OrderByDescending(r => r.DateRequested).ThenByDescending(r => r.RequestId);
    }

    public BarangayStatistics GetStatistics()
    {
        var byType = _requests
            .GroupBy(r => FeeSchedule.NameOf(r.DocumentType))
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        var byPurok = _residents
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Purok) ? "(unassigned)" : r.Purok)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        return new BarangayStatistics(
            _residents.Count,
            _residents.Count(r => r.IsRegisteredVoter),
            _residents.Count(r => r.HasClassification(ResidentClassification.SeniorCitizen)),
            _requests.Count,
            _requests.Count(r => r.Status == RequestStatus.Pending),
            _requests.Count(r => r.Status == RequestStatus.Processing),
            _requests.Count(r => r.Status == RequestStatus.ReadyForRelease),
            _requests.Count(r => r.Status == RequestStatus.Released),
            _requests.Where(r => r.IsPaid).Sum(r => r.Fee),
            _requests.Count(r => r.Fee == 0 && r.Status == RequestStatus.Released),
            byType,
            byPurok);
    }

    // -----------------------------------------------------------------
    //  Commands. Each one is: validate, persist (hook), mirror in memory.
    //  The hook runs FIRST, so if the database says no, the screen never
    //  sees a row that does not exist.
    // -----------------------------------------------------------------

    public Resident AddResident(ResidentDetails details)
    {
        if (details is null) throw new ArgumentNullException(nameof(details));

        int id = InsertResident(details);

        var resident = new Resident(id, details.FirstName, details.LastName);
        Apply(resident, details);
        _residents.Add(resident);
        return resident;
    }

    public void UpdateResident(Resident resident, ResidentDetails details)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));
        if (details is null) throw new ArgumentNullException(nameof(details));

        UpdateResidentRow(resident, details);
        Apply(resident, details);
    }

    public void RemoveResident(Resident resident)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));

        DeleteResidentRow(resident);

        // A resident's requests go with them. If I left them, the request
        // list would point at somebody who no longer exists and the grid
        // would crash the next time it drew that row. MySQL does the same
        // thing on its side through ON DELETE CASCADE; this keeps the
        // working set honest without a reload.
        _requests.RemoveAll(r => r.Resident.ResidentId == resident.ResidentId);
        _residents.Remove(resident);
    }

    public DocumentRequest CreateRequest(
        Resident resident, DocumentType type, string purpose, RequestInput? input = null) =>
        File(resident, type, purpose, input, filedOn: null);

    /// <summary>
    /// File a request AND price it in the same breath.
    ///
    /// The fee rules run here, inside the store, rather than on any screen.
    /// That way no screen can forget to run them, apply them twice, or send
    /// a request the law says cannot be issued: if the assessment is
    /// blocked (a second RA 11261 claim, a cedula for a minor) I refuse to
    /// file it at all. The request form already greys out its button in
    /// that case; this is the rule itself, and the form merely reflects it.
    ///
    /// The filing time can be supplied so the sample data can backdate one
    /// request for the RA 11032 aging demonstration. It is internal because
    /// the screens must always file "now".
    /// </summary>
    internal DocumentRequest File(
        Resident resident, DocumentType type, string purpose, RequestInput? input, DateTime? filedOn)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));

        input ??= RequestInput.Default;
        purpose ??= string.Empty;

        var assessment = Fees.Assess(resident, type, input);
        if (assessment.IsBlocked)
            throw new InvalidOperationException(assessment.BlockReason);

        // Whole seconds, because that is all MySQL's DATETIME keeps. Trimming
        // before the insert means the time in memory is exactly the time on
        // disk, so a reload can never shuffle the queue order by a few ticks.
        var when = TrimToSeconds(filedOn ?? DateTime.Now);

        int id = InsertRequest(resident, type, purpose, input, assessment, when);

        var request = new DocumentRequest(id, resident, type, purpose, input, when);
        request.ApplyAssessment(assessment);

        _requests.Add(request);
        resident.AddRequest(request);
        return request;
    }

    public void SaveRequest(DocumentRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        UpdateRequestRow(request);
    }

    /// <summary>
    /// Nothing to do unless the subclass has somewhere to reload from. The
    /// in-memory store IS its own storage, so it keeps this; the MySQL
    /// store overrides it and reads the tables again.
    /// </summary>
    public virtual void Reload() { }

    // -----------------------------------------------------------------
    //  The hooks. A subclass writes only these.
    // -----------------------------------------------------------------

    /// <summary>Store a new resident and return the id storage gave it.</summary>
    protected abstract int InsertResident(ResidentDetails details);

    protected abstract void UpdateResidentRow(Resident resident, ResidentDetails details);

    protected abstract void DeleteResidentRow(Resident resident);

    /// <summary>Store a new Pending request, already assessed, and return
    /// the id storage gave it.</summary>
    protected abstract int InsertRequest(
        Resident resident, DocumentType type, string purpose, RequestInput input,
        FeeAssessment assessment, DateTime filedOn);

    protected abstract void UpdateRequestRow(DocumentRequest request);

    // -----------------------------------------------------------------
    //  Helpers for the subclasses.
    // -----------------------------------------------------------------

    /// <summary>
    /// Swap the whole working set at once. I take copies before I clear
    /// anything, so a subclass can hand me the lists it just loaded and a
    /// failure half-way through loading never leaves the screens empty.
    /// </summary>
    protected void ReplaceWorkingSet(IEnumerable<Resident> residents, IEnumerable<DocumentRequest> requests)
    {
        var newResidents = residents.ToList();
        var newRequests = requests.ToList();

        _residents.Clear();
        _residents.AddRange(newResidents);
        _requests.Clear();
        _requests.AddRange(newRequests);
    }

    /// <summary>Copy the editable fields onto a resident. Add and Update
    /// both use this, so they cannot disagree about which fields exist.</summary>
    protected static void Apply(Resident r, ResidentDetails d)
    {
        r.FirstName         = d.FirstName;
        r.MiddleName        = d.MiddleName;
        r.LastName          = d.LastName;
        r.Suffix            = d.Suffix;
        r.DateOfBirth       = d.DateOfBirth;
        r.Gender            = d.Gender;
        r.CivilStatus       = d.CivilStatus;
        r.Purok             = d.Purok;
        r.AddressLine       = d.AddressLine;
        r.ContactNumber     = d.ContactNumber;
        r.Occupation        = d.Occupation;
        r.DateOfResidency   = d.DateOfResidency;
        r.IsRegisteredVoter = d.IsRegisteredVoter;
        r.Classification    = d.Classification;
    }

    protected static DateTime TrimToSeconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Kind);

    // .NET Framework's string.Contains has no case-insensitive overload
    // (that arrived in .NET Core), so IndexOf does the job.
    private static bool Contains(string haystack, string needle) =>
        haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
}
