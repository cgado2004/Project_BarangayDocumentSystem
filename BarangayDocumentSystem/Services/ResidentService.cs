// ---------------------------------------------------------------------------
//  ResidentService.cs - the rules around the resident registry.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;

namespace BarangayDocumentSystem.Services
{
    /// <summary>
    /// Everything the residents screen is allowed to do to a record.
    ///
    /// I put the work here rather than in the form so that the same rules hold
    /// no matter who calls: the search, the duplicate check, the suggestion for
    /// the temporary / permanent / newcomer status, the household handling, and
    /// the deactivate-and-archive rules. The form collects what the clerk typed
    /// and shows what this class decided.
    ///
    /// The one rule I want to point out is that Delete does not exist. A
    /// resident is deactivated (they moved out, or they died) or archived (the
    /// record is finished with), and both of those keep the row, because
    /// certificates that were already issued and receipts that were already
    /// collected point at it.
    /// </summary>
    public class ResidentService
    {
        private readonly IBarangayRepository _repository;
        private readonly ActivityLogService _log;
        private readonly SessionManager _session;
        private readonly IClock _clock;

        public ResidentService(IBarangayRepository repository, ActivityLogService log,
                               SessionManager session, IClock clock)
        {
            if (repository == null) throw new ArgumentNullException("repository");

            _repository = repository;
            _log = log;
            _session = session;
            _clock = clock == null ? new SystemClock() : clock;
        }

        // ==================================================================
        //  Reading
        // ==================================================================

        public IList<Resident> Search(ResidentQuery query)
        {
            return _repository.GetResidents(query);
        }

        public Resident Get(int residentId)
        {
            return _repository.GetResident(residentId);
        }

        /// <summary>The people who can be chosen on the requests screen: active
        /// residents only, because the barangay does not issue a clearance to a
        /// record that was switched off.</summary>
        public IList<Resident> GetActiveResidents()
        {
            ResidentQuery query = new ResidentQuery();
            query.IncludeInactive = false;
            return _repository.GetResidents(query);
        }

        public IList<Dependent> GetDependents(int residentId)
        {
            return _repository.GetDependents(residentId);
        }

        /// <summary>
        /// What the program would put in the status box if the clerk left it
        /// alone. The clerk can always overrule it - the barangay knows its own
        /// people better than a rule about months does.
        /// </summary>
        public ResidencyStatus SuggestResidencyStatus(Resident resident)
        {
            if (resident == null) return ResidencyStatus.Newcomer;
            return resident.SuggestResidencyStatus(AppConfig.NewcomerMonths, AppConfig.PermanentResidencyYears);
        }

        // ==================================================================
        //  Registering and editing
        // ==================================================================

        public OperationResult<Resident> Register(Resident resident, bool acceptAsNewResident)
        {
            string refusal;
            if (!Allowed(Permission.ManageResidents, out refusal))
                return OperationResult<Resident>.Fail(refusal);

            if (resident == null) return OperationResult<Resident>.Fail("There is no resident to save.");

            var problems = InputValidator.ValidateResident(resident);
            if (problems.Count > 0)
                return OperationResult<Resident>.Fail(string.Join(Environment.NewLine, problems.ToArray()));

            // The duplicate check. This is the one mistake a registry screen
            // makes easy - the same person encoded twice, then counted twice in
            // the census - so I look for the same name and birthday and ask the
            // clerk to confirm before I save.
            IList<Resident> possible = _repository.FindPossibleDuplicates(
                resident.FirstName, resident.LastName, resident.DateOfBirth, 0);

            if (possible.Count > 0 && !acceptAsNewResident)
            {
                return OperationResult<Resident>.Fail(
                    "Somebody with that name and date of birth is already on the registry: "
                    + possible[0].GetSortableName() + " (" + EnumText.Of(possible[0].RecordState)
                    + "). Open that record instead, or confirm that this is a different person.");
            }

            resident.CreatedOn = _clock.Now();
            resident.CreatedBy = _session.Username;
            resident.RecordState = RecordState.Active;

            if (resident.ResidencyStatus == ResidencyStatus.Newcomer && resident.GetMonthsOfResidency() >= 0)
                resident.ResidencyStatus = SuggestResidencyStatus(resident);

            _repository.InsertResident(resident);

            _log.Record(ActivityModule.Residents, "Registered", "Resident", resident.ResidentId.ToString(),
                "Registered " + resident.GetFullName() + " of " + resident.Purok + ", "
                + EnumText.Of(resident.ResidencyStatus) + " resident, fee category "
                + resident.GetFeeCategoryText() + ".");

            return OperationResult<Resident>.Ok(resident,
                resident.GetFullName() + " is on the registry. The household has "
                + resident.GetHouseholdSize() + " member(s).");
        }

        public OperationResult Update(Resident resident, bool acceptAsNewResident)
        {
            string refusal;
            if (!Allowed(Permission.ManageResidents, out refusal))
                return OperationResult.Fail(refusal);

            if (resident == null) return OperationResult.Fail("There is no resident to save.");

            var problems = InputValidator.ValidateResident(resident);
            if (problems.Count > 0)
                return OperationResult.Fail(string.Join(Environment.NewLine, problems.ToArray()));

            IList<Resident> possible = _repository.FindPossibleDuplicates(
                resident.FirstName, resident.LastName, resident.DateOfBirth, resident.ResidentId);

            if (possible.Count > 0 && !acceptAsNewResident)
            {
                return OperationResult.Fail(
                    "Another record has the same name and date of birth: " + possible[0].GetSortableName()
                    + ". Please check the two records before saving.");
            }

            resident.UpdatedOn = _clock.Now();
            resident.UpdatedBy = _session.Username;

            _repository.UpdateResident(resident);

            _log.Record(ActivityModule.Residents, "Edited", "Resident", resident.ResidentId.ToString(),
                "Edited the record of " + resident.GetFullName() + " of " + resident.Purok
                + ". Status " + EnumText.Of(resident.RecordState) + ", residency "
                + EnumText.Of(resident.ResidencyStatus) + ".");

            return OperationResult.Ok("The record of " + resident.GetFullName() + " is saved.");
        }

        // ==================================================================
        //  Deactivate, archive, reactivate
        // ==================================================================

        /// <summary>
        /// Switches a record off, files it away, or brings it back.
        ///
        /// All three go through one method because they are the same action
        /// with a different reason, and because each of them must write a
        /// reason. The Activity Log then reads like a sentence: who moved this
        /// resident, when, and why.
        /// </summary>
        public OperationResult ChangeState(Resident resident, RecordState state, string reason)
        {
            string refusal;
            if (!Allowed(Permission.ArchiveResidents, out refusal))
                return OperationResult.Fail(refusal);

            if (resident == null) return OperationResult.Fail("Please choose a resident first.");
            if (state != RecordState.Active && string.IsNullOrWhiteSpace(reason))
                return OperationResult.Fail("Please write why this record is being "
                    + (state == RecordState.Inactive ? "deactivated" : "archived") + ".");

            if (state == RecordState.Archived && HasOpenRequests(resident.ResidentId))
                return OperationResult.Fail(
                    "This resident still has requests in the queue. Please finish or reject them first, "
                    + "so the queue never points at a record that was archived.");

            RecordState previous = resident.RecordState;
            resident.SetRecordState(state, reason, _session.Username, _clock.Now());
            _repository.SetRecordState(resident.ResidentId, state, reason, _session.Username, _clock.Now());

            _log.Record(ActivityModule.Residents, ActionFor(state), "Resident", resident.ResidentId.ToString(),
                resident.GetFullName() + " changed from " + EnumText.Of(previous) + " to "
                + EnumText.Of(state) + ". Reason: " + reason);

            switch (state)
            {
                case RecordState.Inactive:
                    return OperationResult.Ok(resident.GetFullName()
                        + " is now inactive. The record keeps the history, so old certificates and receipts still point at it.");
                case RecordState.Archived:
                    return OperationResult.Ok(resident.GetFullName() + " is archived and out of the active lists.");
                default:
                    return OperationResult.Ok(resident.GetFullName() + " is active in the barangay again.");
            }
        }

        private bool HasOpenRequests(int residentId)
        {
            RequestQuery query = new RequestQuery();
            query.ResidentId = residentId;

            foreach (DocumentRequest request in _repository.GetRequests(query))
                if (!request.IsClosed) return true;

            return false;
        }

        private static string ActionFor(RecordState state)
        {
            switch (state)
            {
                case RecordState.Inactive: return "Deactivated";
                case RecordState.Archived: return "Archived";
                default: return "Reactivated";
            }
        }

        // ==================================================================
        //  Dependents
        // ==================================================================

        /// <summary>
        /// Adds somebody to a household.
        ///
        /// A dependent cannot exist without a resident to hang off, which is
        /// why the id comes from the record that is open on screen. I also mark
        /// the head of the household automatically, because a resident with
        /// dependents is by definition heading a family, and asking the clerk to
        /// tick a box as well would just be one more chance to forget.
        /// </summary>
        public OperationResult<Dependent> AddDependent(Resident head, Dependent dependent)
        {
            string refusal;
            if (!Allowed(Permission.ManageResidents, out refusal))
                return OperationResult<Dependent>.Fail(refusal);

            if (head == null) return OperationResult<Dependent>.Fail("Please open a resident first.");
            if (dependent == null) return OperationResult<Dependent>.Fail("There is no dependent to save.");

            var problems = InputValidator.ValidateDependent(dependent);
            if (problems.Count > 0)
                return OperationResult<Dependent>.Fail(string.Join(Environment.NewLine, problems.ToArray()));

            dependent.HeadResidentId = head.ResidentId;
            dependent.CreatedOn = _clock.Now();
            dependent.CreatedBy = _session.Username;

            _repository.InsertDependent(dependent);
            head.AddDependent(dependent);

            if (!head.IsHeadOfFamily)
            {
                head.IsHeadOfFamily = true;
                _repository.UpdateResident(head);
            }

            _log.Record(ActivityModule.Residents, "Registered dependent", "Dependent",
                dependent.DependentId.ToString(),
                "Registered " + dependent.FullName + " (" + dependent.GetRelationText() + ", "
                + dependent.GetAge() + ") under " + head.GetFullName() + ". The household now has "
                + head.GetHouseholdSize() + " member(s).");

            return OperationResult<Dependent>.Ok(dependent, dependent.FullName + " is registered under the household.");
        }

        public OperationResult UpdateDependent(Resident head, Dependent dependent)
        {
            string refusal;
            if (!Allowed(Permission.ManageResidents, out refusal))
                return OperationResult.Fail(refusal);

            if (dependent == null) return OperationResult.Fail("There is no dependent to save.");

            var problems = InputValidator.ValidateDependent(dependent);
            if (problems.Count > 0)
                return OperationResult.Fail(string.Join(Environment.NewLine, problems.ToArray()));

            _repository.UpdateDependent(dependent);

            _log.Record(ActivityModule.Residents, "Edited dependent", "Dependent",
                dependent.DependentId.ToString(),
                "Edited " + dependent.FullName + " under " + (head == null ? "the household" : head.GetFullName()) + ".");

            return OperationResult.Ok("The dependent's details are saved.");
        }

        public OperationResult RemoveDependent(Resident head, Dependent dependent)
        {
            string refusal;
            if (!Allowed(Permission.ManageResidents, out refusal))
                return OperationResult.Fail(refusal);

            if (dependent == null) return OperationResult.Fail("Please choose a dependent first.");

            _repository.DeleteDependent(dependent.DependentId);

            _log.Record(ActivityModule.Residents, "Removed dependent", "Dependent",
                dependent.DependentId.ToString(),
                "Removed " + dependent.FullName + " from "
                + (head == null ? "the household" : head.GetFullName() + "'s household") + ".");

            return OperationResult.Ok(dependent.FullName + " is removed from the household.");
        }

        // ==================================================================
        //  Internals
        // ==================================================================

        private bool Allowed(Permission permission, out string refusal)
        {
            refusal = string.Empty;

            if (_session == null || !_session.IsSignedIn)
            {
                refusal = "Please sign in first.";
                return false;
            }

            if (_session.Has(permission)) return true;

            refusal = _session.RefusalFor(permission);
            _log.Record(ActivityModule.Residents, "Refused", "Resident", string.Empty,
                _session.DescribeForLog(permission));
            return false;
        }
    }
}
