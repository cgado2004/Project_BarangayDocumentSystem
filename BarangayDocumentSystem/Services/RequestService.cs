// ---------------------------------------------------------------------------
//  RequestService.cs - filing a request and moving it along.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// the resident, the document, the fee the schedule decides, the
        /// starting status the clock decides, and the sentences that explain
        /// both. The confirmation dialog shows this; the clerk says it back to
        /// the resident; then File() saves exactly what was shown.
        /// </summary>
        public RequestPreview Preview(Resident resident, DocumentRequest request, bool forceValidation)
        {
            if (resident == null) throw new ArgumentNullException("resident");
            if (request == null) throw new ArgumentNullException("request");

            FeeAssessment assessment = _fees.Assess(resident, request);

            // The preview answers for the moment on the request, the same
            // moment File() will honour when the clerk confirms: a request
            // dated 4:20 PM must preview as Pending even if the clerk only
            // reads the preview at 4:31.
            request.FiledDuringOfficeWindow = _window.IsInsideOfficeWindow(request.DateRequested);

            RequestPreview preview = new RequestPreview();
            preview.Resident = resident;
            preview.Request = request;
            preview.Assessment = assessment;
            preview.StartingStatus = _window.SuggestStartingStatus(request, forceValidation);

            if (preview.StartingStatus == RequestStatus.Processing)
                preview.TimeExplanation = "Business clearances are inspected before the barangay signs, "
                    + "so this one starts in Processing.";
            else if (preview.StartingStatus == RequestStatus.Pending)
                preview.TimeExplanation = "Filed after 4:00 PM, so it waits as Pending until the next "
                    + "office window opens at 8:00 AM.";
            else
                preview.TimeExplanation = "Filed inside office hours with nothing to check, "
                    + "so it is Cleared on the spot.";

            preview.StatusExplanation = preview.TimeExplanation;
            return preview;
        }

        // ==================================================================
        //  Filing
        // ==================================================================

        public OperationResult<DocumentRequest> File(Resident resident, DocumentRequest request, bool forceValidation)
        {
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
                    return OperationResult<DocumentRequest>.Fail(
                        "The first-time jobseeker waiver cannot be applied. " + reason);
            }

            FeeAssessment assessment = _fees.Assess(resident, request);
            DateTime now = _clock.Now();

            // The filing moment is the one on the request - the clerk files
            // for the resident standing at the counter, and the history must
            // say 4:20 PM when the request says 4:20 PM, not whenever the
            // button was clicked. The clock is only the fallback for a
            // request that carries no moment of its own.
            DateTime filedAt = request.DateRequested == default(DateTime) ? now : request.DateRequested;

            request.ReferenceNumber = _repository.NextReferenceNumber(filedAt);
            request.ResidentId = resident.ResidentId;
            request.ResidentName = resident.GetFullName();
            request.DateRequested = filedAt;
            request.Fee = assessment.FinalFee;
            request.FeeBasis = assessment.Basis;
            request.RequiresValidation = forceValidation
                || request.DocumentType == DocumentType.BarangayBusinessClearance;
            request.FiledDuringOfficeWindow = _window.IsInsideOfficeWindow(filedAt);

            RequestStatus starting = _window.SuggestStartingStatus(request, forceValidation);
            request.SetInitialStatus(starting,
                _window.ExplainStartingStatus(request, starting), _session.Username, filedAt);

            _repository.InsertRequest(request);

            _log.Record(ActivityModule.Requests, "Filed " + request.ReferenceNumber, "Document request",
                request.ReferenceNumber,
                resident.GetFullName() + " filed a " + request.GetDocumentName()
                + " (" + request.Purpose + "). Fee " + request.GetFeeText()
                + ", starts as " + EnumText.Of(starting) + ".");

            return OperationResult<DocumentRequest>.Ok(request,
                request.ReferenceNumber + " is filed as " + EnumText.Of(starting) + ".");
        }

        // ==================================================================
        //  The moves
        // ==================================================================

        public OperationResult SendToProcessing(DocumentRequest request, string reason)
        {
            return Move(request, "Sent to processing",
                delegate { request.SendToProcessing(reason, _session.Username, _clock.Now()); });
        }

        public OperationResult Clear(DocumentRequest request, string reason)
        {
            return Move(request, "Cleared",
                delegate { request.Clear(reason, _session.Username, _clock.Now()); });
        }

        public OperationResult MarkReadyForRelease(DocumentRequest request)
        {
            return Move(request, "Ready for release",
                delegate { request.MarkReadyForRelease(_session.Username, _clock.Now()); });
        }

        public OperationResult Release(DocumentRequest request, string receivedBy)
        {
            return Move(request, "Released",
                delegate { request.Release(receivedBy, _session.Username, _clock.Now()); });
        }

        public OperationResult Reject(DocumentRequest request, string reason)
        {
            return Move(request, "Rejected",
                delegate { request.Reject(reason, _session.Username, _clock.Now()); });
        }

        public OperationResult Reopen(DocumentRequest request, string reason)
        {
            return Move(request, "Reopened",
                delegate { request.ReopenFromRejection(reason, _session.Username, _clock.Now()); });
        }

        private OperationResult Move(DocumentRequest request, string action, Action change)
        {
            if (request == null) return OperationResult.Fail("Please choose a request first.");

            try
            {
                change();
            }
            catch (InvalidOperationException error)
            {
                return OperationResult.Fail(error.Message);
            }

            _repository.UpdateRequest(request);

            _log.Record(ActivityModule.Requests, action + " " + request.ReferenceNumber,
                "Document request", request.ReferenceNumber,
                request.GetDocumentName() + " for " + request.ResidentName
                + " is now " + EnumText.Of(request.Status) + ".");

            return OperationResult.Ok(request.ReferenceNumber + " is now "
                + EnumText.Of(request.Status) + ".");
        }

        // ==================================================================
        //  The automatic next-morning clearances
        // ==================================================================

        /// <summary>
        /// Moves every after-hours request from Pending to Cleared, once the
        /// office window has opened again.
        ///
        /// This runs on startup so the queue is honest on a Monday morning.
        /// The stored procedure does the same work inside the database; this
        /// method is the in-memory fallback and the one the rule checks use.
        /// </summary>
        public int ClearWaitingRequests()
        {
            DateTime now = _clock.Now();
            if (!_window.IsInsideOfficeWindow(now)) return 0;

            int cleared = 0;
            foreach (DocumentRequest request in _repository.GetRequests(null))
            {
                if (request.WaitsForNextWindow)
                {
                    request.Clear("Automatically cleared when the next office window opened.",
                        "system", now);
                    _repository.UpdateRequest(request);
                    cleared++;
                }
            }

            if (cleared > 0)
                _log.Record(ActivityModule.Requests, "Cleared " + cleared + " waiting request(s)",
                    "Document request", string.Empty,
                    "The office window opened and " + cleared + " request(s) filed outside hours were cleared.");

            return cleared;
        }
    }
}
