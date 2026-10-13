// ---------------------------------------------------------------------------
//  ResidentService.cs - everything the residents screen is allowed to do to a record.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
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

        public IList<Resident> GetResidents(ResidentQuery query)
        {
            return _repository.GetResidents(query);
        }

        public Resident GetResident(int residentId)
        {
            return _repository.GetResident(residentId);
        }

        /// <summary>The household of one resident, oldest first - the
        /// dependents screen and the census both read it through here so
        /// neither has to know how a household is stored.</summary>
        public IList<Dependent> GetDependents(int headResidentId)
        {
            return _repository.GetDependents(headResidentId);
        }

        /// <summary>
        /// Suggests the residency status from the date of residency, the way
        /// the barangay described it: a newcomer is inside the first six
        /// months, temporary is anything up to five years, and permanent is
        /// five years or more. The clerk can still override it.
        /// </summary>
        public ResidencyStatus SuggestResidencyStatus(Resident resident)
        {
            if (resident == null) return ResidencyStatus.Newcomer;

            int months = resident.GetMonthsOfResidency();
            if (months < AppConfig.NewcomerMonths) return ResidencyStatus.Newcomer;
            if (months < AppConfig.PermanentResidencyYears * 12) return ResidencyStatus.Temporary;
            return ResidencyStatus.Permanent;
        }

        // ==================================================================
        //  Registering and updating
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

            _log.Record(ActivityModule.Residents, "Updated", "Resident", resident.ResidentId.ToString(),
                "Updated " + resident.GetFullName() + " of " + resident.Purok + ".");

            return OperationResult.Ok(resident.GetFullName() + " is saved.");
        }

        // ==================================================================
        //  Deactivate, reactivate, archive
        // ==================================================================

        public OperationResult ChangeState(Resident resident, RecordState newState, string reason)
        {
            string refusal;
            if (!Allowed(Permission.ManageResidents, out refusal))
                return OperationResult.Fail(refusal);

            if (resident == null) return OperationResult.Fail("Please choose a resident first.");

            if (string.IsNullOrWhiteSpace(reason))
                return OperationResult.Fail("I need a reason before I can change a record's state.");

            if (resident.RecordState == newState)
                return OperationResult.Fail("That record is already " + EnumText.Of(newState) + ".");

            RecordState previous = resident.RecordState;
            _repository.SetRecordState(resident.ResidentId, newState, reason.Trim(),
                _session.Username, _clock.Now());
            resident.RecordState = newState;
            resident.StateReason = reason.Trim();

            _log.Record(ActivityModule.Residents,
                EnumText.Of(newState) + " (was " + EnumText.Of(previous) + ")", "Resident",
                resident.ResidentId.ToString(),
                resident.GetFullName() + " is now " + EnumText.Of(newState)
                + ". Reason: " + reason.Trim() + ".");

            return OperationResult.Ok(resident.GetFullName() + " is now " + EnumText.Of(newState) + ".");
        }

        // ==================================================================
        //  Household
        // ==================================================================

        public OperationResult<Dependent> AddDependent(Resident head, Dependent dependent)
        {
            string refusal;
            if (!Allowed(Permission.ManageResidents, out refusal))
                return OperationResult<Dependent>.Fail(refusal);

            if (head == null) return OperationResult<Dependent>.Fail("Please choose the head of the family first.");
            if (dependent == null) return OperationResult<Dependent>.Fail("There is no dependent to save.");

            var problems = InputValidator.ValidateDependent(dependent);
            if (problems.Count > 0)
                return OperationResult<Dependent>.Fail(string.Join(Environment.NewLine, problems.ToArray()));

            dependent.HeadResidentId = head.ResidentId;
            dependent.CreatedOn = _clock.Now();
            dependent.CreatedBy = _session.Username;

            _repository.InsertDependent(dependent);

            _log.Record(ActivityModule.Residents, "Dependent added", "Resident", head.ResidentId.ToString(),
                dependent.FullName + " (" + dependent.Relation + ") added to " + head.GetFullName() + "'s household.");

            return OperationResult<Dependent>.Ok(dependent,
                dependent.FullName + " is registered under " + head.GetFullName() + ".");
        }

        /// <summary>Saves the changes to one person of the household. The
        /// dependent has to belong to that household - a change may never
        /// quietly move somebody into another family's record.</summary>
        public OperationResult UpdateDependent(Resident head, Dependent dependent)
        {
            string refusal;
            if (!Allowed(Permission.ManageResidents, out refusal))
                return OperationResult.Fail(refusal);

            if (head == null) return OperationResult.Fail("Please choose the head of the family first.");
            if (dependent == null) return OperationResult.Fail("Please choose a dependent first.");
            if (dependent.DependentId <= 0 || dependent.HeadResidentId != head.ResidentId)
                return OperationResult.Fail(dependent.FullName + " is not part of "
                    + head.GetFullName() + "'s household, so the change cannot be saved here.");

            var problems = InputValidator.ValidateDependent(dependent);
            if (problems.Count > 0)
                return OperationResult.Fail(string.Join(Environment.NewLine, problems.ToArray()));

            _repository.UpdateDependent(dependent);

            _log.Record(ActivityModule.Residents, "Dependent updated", "Resident", head.ResidentId.ToString(),
                dependent.FullName + " (" + dependent.Relation + ") in " + head.GetFullName() + "'s household was edited.");

            return OperationResult.Ok(dependent.FullName + "'s record is saved.");
        }

        public OperationResult RemoveDependent(Resident head, Dependent dependent)
        {
            string refusal;
            if (!Allowed(Permission.ManageResidents, out refusal))
                return OperationResult.Fail(refusal);

            if (head == null) return OperationResult.Fail("Please choose the head of the family first.");
            if (dependent == null) return OperationResult.Fail("Please choose a dependent first.");

            _repository.DeleteDependent(dependent.DependentId);

            _log.Record(ActivityModule.Residents, "Dependent removed", "Resident", head.ResidentId.ToString(),
                dependent.FullName + " removed from " + head.GetFullName() + "'s household.");

            return OperationResult.Ok(dependent.FullName + " is removed from the household.");
        }

        // ==================================================================
        //  Small helpers
        // ==================================================================

        private bool Allowed(Permission permission, out string refusal)
        {
            refusal = string.Empty;
            if (_session == null || _session.User == null)
            {
                refusal = "Please sign in first.";
                return false;
            }

            if (!PermissionSet.Allows(_session.User.Role, permission))
            {
                refusal = "Your account is not allowed to change resident records. "
                        + "Ask the administrator if you need this.";
                return false;
            }

            return true;
        }
    }
}
