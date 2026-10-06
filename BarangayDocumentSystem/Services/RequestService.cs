// ---------------------------------------------------------------------------
//  RequestService.cs - filing a request and moving it along.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;

namespace BarangayDocumentSystem.Services
{
    /// <summary>What the confirmation dialog shows before a request is filed.</summary>
    public class RequestPreview
    {
        public Resident Resident { get; set; }
        public DocumentRequest Request { get; set; }
        public FeeAssessment Assessment { get; set; }
        public RequestStatus StartingStatus { get; set; }
        public string TimeExplanation { get; set; }
        public string StatusExplanation { get; set; }

        public RequestPreview()
        {
            TimeExplanation = string.Empty;
            StatusExplanation = string.Empty;
        }

        /// <summary>The lines the clerk reads back to the resident before
        /// anything is saved. I keep them here so the confirmation dialog, the
        /// printed counter slip and the rule checks all say the same thing.</summary>
        public IList<string> GetLines()
        {
            List<string> lines = new List<string>();

            lines.Add("Resident: " + (Resident == null ? "-" : Resident.GetFullName()));
            lines.Add("Purok: " + (Resident == null ? "-" : Resident.Purok));
            if (Resident != null)
                lines.Add("Age / residency: " + Resident.GetAge() + " years old, "
                          + EnumText.Of(Resident.ResidencyStatus) + " resident");

            lines.Add("Document: " + (Request == null ? "-" : Request.GetDocumentName()));
            lines.Add("Purpose: " + (Request == null ? "-" : Request.Purpose));

            if (Request != null && Request.DocumentType == DocumentType.BarangayBusinessClearance
                && Request.Business != null)
                lines.Add("Business: " + Request.Business.Summary());

            lines.Add("Fee to collect: " + (Assessment == null
                ? "P0.00"
                : "P" + Assessment.FinalFee.ToString("#,##0.00") + (Assessment.IsFree ? " (no charge)" : string.Empty)));

            lines.Add("Starting status: " + EnumText.Of(StartingStatus));
            lines.Add(TimeExplanation);

            return lines;
        }
    }

    /// <summary>
    /// Filing requests and moving them through the queue.
    ///
    /// This is the busiest class in the program, so I will describe what it
    /// guarantees, because that is what the barangay is really asking for:
    ///
    ///  * the fee always comes from the fee schedule, never from the screen;
    ///  * the starting status always comes from the 8:00 AM to 4:00 PM rule
    ///    (or from Processing, when the document needs validation);
    ///  * a request cannot be released before it is ready AND the money is
    ///    settled against an official receipt;
    ///  * every move is written into the request's own history and into the
    ///    activity log, in the same breath;
    ///  * a rejection always carries a reason.
    ///
    /// Where a screen needs to know what will happen before saving, it asks
    /// Preview() and shows the answer. That is the Confirmation step: nothing
    /// is written until the clerk has seen the resident, the document, the fee
    /// and the status in one place.
    /// </summary>
    public class RequestService
    {
        private readonly IBarangayRepository _repository;
        private readonly ActivityLogService _log;
        private readonly SessionManager _session;
        private readonly IClock _clock;
        private readonly FeeSchedule _fees;
        private readonly TimeWindowPolicy _window;

        public RequestService(IBarangayRepository repository, ActivityLogService log, SessionManager session,
                              IClock clock, FeeSchedule fees, TimeWindowPolicy window)
        {
            if (repository == null) throw new ArgumentNullException("repository");

            _repository = repository;
            _log = log;
            _session = session;
            _clock = clock == null ? new SystemClock() : clock;
            _fees = fees == null ? new FeeSchedule() : fees;
            _window = window == null ? new TimeWindowPolicy(clock) : window;
        }

        public FeeSchedule Fees { get { return _fees; } }
        public TimeWindowPolicy Window { get { return _window; } }

        // ==================================================================
        //  Reading
        // ==================================================================

        public IList<DocumentRequest> GetRequests(RequestQuery query)
        {
            return _repository.GetRequests(query);
        }

        public DocumentRequest GetRequest(int requestId)
        {
            return _repository.GetRequest(requestId);
        }

        /// <summary>The queue for a chosen day, which is the "per day
        /// transaction" filter the barangay asked for. Today is the default and
        /// the most used.</summary>
        public IList<DocumentRequest> GetRequestsForDay(DateTime day)
        {
            RequestQuery query = new RequestQuery();
            query.From = day.Date;
            query.To = day.Date;
            return _repository.GetRequests(query);
        }

        // ==================================================================
        //  Working out what will happen, before anything is saved
        // ==================================================================

        /// <summary>
        /// Builds the whole picture of a request that has not been filed yet:
        /// the resident, the document, the fee the schedule decided, and the
        /// status the clock decided.
        ///
        /// The confirmation dialog simply shows this. Because the preview and
        /// the real filing call the same two rules, the dialog can never
        /// promise something the save will not do.
        /// </summary>
        public RequestPreview Preview(Resident resident, DocumentRequest request, bool forceValidation)
        {
            if (resident == null) throw new ArgumentNullException("resident");
            if (request == null) throw new ArgumentNullException("request");

            request.ResidentId = resident.ResidentId;
            request.ResidentName = resident.GetFullName();
            request.DateRequested = request.DateRequested == default(DateTime) ? _clock.Now() : request.DateRequested;
            request.RequiresValidation = forceValidation || request.RequiresValidation
                || request.DocumentType == DocumentType.BarangayBusinessClearance;

            request.FiledDuringOfficeWindow = _window.IsInsideOfficeWindow(request.DateRequested);

            FeeAssessment assessment = _fees.Assess(resident, request);
            request.SetAssessment(assessment.FinalFee, assessment.Basis);

            RequestStatus starting = _window.SuggestStartingStatus(request, forceValidation);

            RequestPreview preview = new RequestPreview();
            preview.Resident = resident;
            preview.Request = request;
            preview.Assessment = assessment;
            preview.StartingStatus = starting;
            preview.TimeExplanation = _window.DescribeShift(request.DateRequested, request.RequiresValidation);
            preview.StatusExplanation = _window.ExplainStartingStatus(request, starting);
            return preview;
        }

        // ==================================================================
        //  Filing
        // ==================================================================

        /// <summary>
        /// Saves a new request. This is the only way a request gets into the
        /// system, so every rule that has to hold at filing time holds here.
        /// </summary>
        public OperationResult<DocumentRequest> FileRequest(Resident resident, DocumentRequest request,
                                                            bool forceValidation)
        {
            string refusal;
            if (!Allowed(Permission.FileRequests, out refusal))
                return OperationResult<DocumentRequest>.Fail(refusal);

            if (resident == null)
                return OperationResult<DocumentRequest>.Fail("Please choose the resident asking for the document.");

            if (request == null)
                return OperationResult<DocumentRequest>.Fail("There is no request to file.");

            var problems = InputValidator.ValidateRequest(resident, request, forceValidation);
            if (problems.Count > 0)
                return OperationResult<DocumentRequest>.Fail(string.Join(Environment.NewLine, problems.ToArray()));

            // RA 11261 is checked twice on purpose: once when the document type
            // is chosen (so the screen can explain itself) and once here. A
            // screen guard can be bypassed by an odd event order; this one
            // cannot.
            if (request.ApplyJobseekerWaiver)
            {
                string reason;
                if (!_fees.IsJobseekerEligible(resident, out reason))
                    return OperationResult<DocumentRequest>.Fail(reason);

                if (!FeeSchedule.IsCoveredByJobseekerAct(request.DocumentType))
                    return OperationResult<DocumentRequest>.Fail(
                        "The RA 11261 first-time jobseeker benefit does not cover "
                        + request.GetDocumentName() + ".");
            }

            RequestPreview preview = Preview(resident, request, forceValidation);

            // The counter number comes from the store, inside a transaction, so
            // two clerks on two machines cannot hand out the same number.
            request.ReferenceNumber = _repository.NextReferenceNumber(request.DateRequested);

            request.SetInitialStatus(preview.StartingStatus, preview.StatusExplanation, _session.Username,
                _clock.Now());
            request.LastStatusChangeOn = _clock.Now();
            request.LastStatusChangeBy = _session.Username;

            if (request.ApplyJobseekerWaiver) request.MarkJobseekerBenefitUsed();

            _repository.InsertRequest(request);
            resident.AddRequest(request);

            string details = "Filed " + request.ReferenceNumber + " (" + request.GetDocumentName() + ") for "
                    + resident.GetFullName() + ". Fee "
                    + (preview.Assessment.IsFree
                        ? "free - " + preview.Assessment.Basis
                        : "P" + preview.Assessment.FinalFee.ToString("#,##0.00") + " - " + preview.Assessment.Basis)
                    + " Starting status " + EnumText.Of(preview.StartingStatus) + ".";

            _log.Record(ActivityModule.Requests, "Filed", "Document request", request.ReferenceNumber, details);

            // A request that starts as Cleared has two history lines: the
            // status, and the reason it was allowed to skip the queue. I write
            // the second one too, because "cleared at 9:14 AM" on its own does
            // not explain itself a year later.
            AppendLatestHistory(request);

            return OperationResult<DocumentRequest>.Ok(request,
                "Request " + request.ReferenceNumber + " is filed as " + request.GetStatusText() + ".");
        }

        // ==================================================================
        //  Moving a request along
        // ==================================================================

        public OperationResult SendToProcessing(DocumentRequest request, string reason)
        {
            string refusal;
            if (!Allowed(Permission.ValidateRequests, out refusal)) return OperationResult.Fail(refusal);

            return Move(request, delegate (DateTime when)
            {
                request.SendToProcessing(reason, _session.Username, when);
            }, reason);
        }

        /// <summary>Clears a request. This is the validator's approval, and it
        /// is also what the 8:00 AM to 4:00 PM rule does by itself.</summary>
        public OperationResult Clear(DocumentRequest request, string reason)
        {
            string refusal;
            if (!Allowed(Permission.ValidateRequests, out refusal)) return OperationResult.Fail(refusal);

            if (string.IsNullOrWhiteSpace(reason))
                reason = _window.DescribeShift(request == null ? _clock.Now() : request.DateRequested, false);

            return Move(request, delegate (DateTime when)
            {
                request.Clear(reason, _session.Username, when);
            }, reason);
        }

        /// <summary>Marks the document printed and waiting on the counter. The
        /// fee is checked by the model, so a fee-bearing document cannot get
        /// here before the receipt exists.</summary>
        public OperationResult MarkReadyForRelease(DocumentRequest request)
        {
            string refusal;
            if (!Allowed(Permission.ReleaseDocuments, out refusal)) return OperationResult.Fail(refusal);

            return Move(request, delegate (DateTime when)
            {
                request.MarkReadyForRelease(_session.Username, when);
            }, string.Empty);
        }

        /// <summary>Hands the document over. I also write the resident's
        /// acknowledgement name, because that is what the barangay's paper
        /// record has always had on it.</summary>
        public OperationResult Release(DocumentRequest request, string receivedBy)
        {
            string refusal;
            if (!Allowed(Permission.ReleaseDocuments, out refusal)) return OperationResult.Fail(refusal);

            if (request == null) return OperationResult.Fail("Please choose a request first.");

            string missing;
            if (!request.CanRelease(out missing)) return OperationResult.Fail(missing);

            if (string.IsNullOrWhiteSpace(receivedBy))
                return OperationResult.Fail("Please write who received the document.");

            OperationResult result = Move(request, delegate (DateTime when)
            {
                request.Release(receivedBy, _session.Username, when);
            }, "Released to " + receivedBy);

            // The once-only RA 11261 benefit is spent at release time, not at
            // filing time: a request that is rejected must not consume it.
            if (result.Succeeded && request.AvailedUnderJobseekerAct)
            {
                request.MarkJobseekerBenefitUsed();
                Resident resident = _repository.GetResident(request.ResidentId);
                if (resident != null)
                {
                    resident.HasAvailedFirstTimeJobseeker = true;
                    _repository.UpdateResident(resident);

                    _log.Record(ActivityModule.Requests, "Jobseeker benefit used", "Resident",
                        resident.ResidentId.ToString(),
                        resident.GetFullName() + " used the once-only RA 11261 benefit on "
                        + request.ReferenceNumber + ".");
                }
            }

            return result;
        }

        public OperationResult Reject(DocumentRequest request, string reason)
        {
            string refusal;
            if (!Allowed(Permission.ValidateRequests, out refusal)) return OperationResult.Fail(refusal);

            if (string.IsNullOrWhiteSpace(reason))
                return OperationResult.Fail("Please write the reason for the rejection. It goes on the record.");

            return Move(request, delegate (DateTime when)
            {
                request.Reject(reason, _session.Username, when);
            }, reason);
        }

        /// <summary>Puts a rejected request back in the queue, for the case
        /// that always happens: the reason turned out to be wrong.</summary>
        public OperationResult Reopen(DocumentRequest request, string reason)
        {
            if (_session == null || !_session.Has(Permission.ValidateRequests))
                return OperationResult.Fail(_session == null ? "Please sign in first."
                    : _session.RefusalFor(Permission.ValidateRequests));

            return Move(request, delegate (DateTime when)
            {
                request.ReopenFromRejection(reason, _session.Username, when);
            }, reason);
        }

        // ==================================================================
        //  The rule that empties the overnight queue
        // ==================================================================

        /// <summary>
        /// Clears the requests that were filed outside office hours, now that
        /// the window has opened again.
        ///
        /// The program runs this when it starts, which is what makes the queue
        /// honest on a Monday morning: everything filed after 4:00 PM on Friday
        /// is still Pending when the office opens, and this sweeps it to
        /// Cleared in one pass. I return the count so the status bar can say
        /// what happened instead of changing rows silently.
        /// </summary>
        public int ClearWaitingRequests()
        {
            DateTime now = _clock.Now();
            int cleared = 0;

            foreach (DocumentRequest request in _repository.GetRequestsWaitingForWindow())
            {
                if (!_window.ShouldClearNow(request, now)) continue;

                RequestStatus previous = request.Status;
                request.Clear("Cleared automatically when the " + _window.GetWindowText()
                            + " window opened, because this document needs no validation.",
                              "system", now);

                _repository.UpdateRequest(request);
                AppendLatestHistory(request);

                _log.RecordAs("system", UserRole.Administrator, ActivityModule.Requests, "Cleared",
                    "Document request", request.ReferenceNumber,
                    "Moved " + request.ReferenceNumber + " from " + EnumText.Of(previous)
                    + " to Cleared when the office window opened.");

                cleared++;
            }

            return cleared;
        }

        // ==================================================================
        //  Internals
        // ==================================================================

        /// <summary>
        /// The one place a status change is saved, logged and explained. Every
        /// public method above funnels through here, which is why the guard in
        /// the model cannot be forgotten by a future screen.
        /// </summary>
        private OperationResult Move(DocumentRequest request, Action<DateTime> change, string reason)
        {
            if (request == null) return OperationResult.Fail("Please choose a request first.");

            RequestStatus previous = request.Status;

            try
            {
                change(_clock.Now());
            }
            catch (InvalidOperationException error)
            {
                // The model refused. That is a normal answer, not a crash: the
                // clerk reads the sentence and tries something else.
                return OperationResult.Fail(error.Message);
            }

            _repository.UpdateRequest(request);
            AppendLatestHistory(request);
            _log.RecordStatusChange(request, previous, reason);

            string message = request.ReferenceNumber + " is now " + request.GetStatusText() + ".";

            if (request.Status == RequestStatus.ReadyForRelease)
                message += " Print it and hand it over when the resident comes back.";

            if (request.Status == RequestStatus.Released && request.AvailedUnderJobseekerAct)
                message += " The RA 11261 benefit is now marked as used for this resident.";

            return OperationResult.Ok(message);
        }

        /// <summary>Writes the newest history line of the request into the
        /// store, for databases that keep the history in their own table.</summary>
        private void AppendLatestHistory(DocumentRequest request)
        {
            if (request.History.Count == 0) return;

            RequestStatusChange latest = request.History[request.History.Count - 1];
            _repository.AppendStatusHistory(request.RequestId, latest);
        }

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
            _log.Record(ActivityModule.Requests, "Refused", "Document request", string.Empty,
                _session.DescribeForLog(permission));
            return false;
        }
    }
}
