// ---------------------------------------------------------------------------
//  DocumentRequest.cs - one request for one paper.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace BarangayDocumentSystem.Models
{
    /// <summary>One line of the request's history: who moved it, when, and why.</summary>
    public class RequestStatusChange
    {
        public RequestStatus Status { get; set; }
        public DateTime ChangedOn { get; set; }
        public string ChangedBy { get; set; }
        public string Reason { get; set; }

        public RequestStatusChange()
        {
            ChangedBy = string.Empty;
            Reason = string.Empty;
        }

        public string ToDisplay()
        {
            string text = ChangedOn.ToString("dd MMM yyyy h:mm tt") + " - " + EnumText.Of(Status);
            if (!string.IsNullOrWhiteSpace(Reason)) text += " (" + Reason + ")";
            if (!string.IsNullOrWhiteSpace(ChangedBy)) text += " by " + ChangedBy;
            return text;
        }
    }

    /// <summary>
    /// A resident's request for a document.
    ///
    /// The rules that matter live in this class, not in the screens, because a
    /// screen can be bypassed by a stale label or a shortcut key but a method
    /// cannot:
    ///
    ///  * a request only moves forward through the states I allow;
    ///  * nothing is released until the fee is settled against an official
    ///    receipt (or the fee is zero in the first place);
    ///  * a rejection always carries a reason;
    ///  * a released document is history and cannot be changed back.
    ///
    /// About the money: the request stores the AMOUNT and, separately, the
    /// legal basis behind it. The basis is internal - it is written on the
    /// activity log and it is available to the admin, but it is deliberately
    /// not shown to the clerk on the request screen, which is what the
    /// barangay asked for.
    /// </summary>
    public class DocumentRequest
    {
        private readonly List<RequestStatusChange> _history = new List<RequestStatusChange>();

        public int RequestId { get; internal set; }

        /// <summary>The number the resident is given at the counter, e.g.
        /// "2026-000123". I build it from the year plus a running count so it
        /// is easy to write by hand on the paper stub.</summary>
        public string ReferenceNumber { get; set; }

        public int ResidentId { get; set; }

        /// <summary>Filled in when the request is loaded with its resident.
        /// It saves the screens a second lookup.</summary>
        public string ResidentName { get; set; }

        public DocumentType DocumentType { get; set; }
        public string Purpose { get; set; }
        public DateTime DateRequested { get; set; }
        public RequestStatus Status { get; internal set; }

        // ---- the money ----
        public decimal Fee { get; internal set; }

        /// <summary>Why that amount was charged or waived. Internal only - see
        /// the class comment. The admin sees it on the activity log and in the
        /// collection report.</summary>
        public string FeeBasis { get; internal set; }

        public bool IsPaid { get; internal set; }
        public string OfficialReceiptNumber { get; internal set; }

        /// <summary>The control number of the government receipt booklet the
        /// collection was written in. This is what makes the collection
        /// auditable, so it is part of the release check, not an extra.</summary>
        public string OrControlNumber { get; internal set; }

        public DateTime? PaymentDate { get; internal set; }
        public string CollectedBy { get; internal set; }

        // ---- what the fee was worked out from ----
        public ClearanceScope Scope { get; set; }
        public decimal AssessedAmount { get; set; }
        public decimal Hours { get; set; }
        public decimal GrossAnnualIncome { get; set; }
        public string Detail { get; set; }
        public bool ApplyJobseekerWaiver { get; set; }
        public bool AvailedUnderJobseekerAct { get; set; }

        // ---- business clearance part ----
        /// <summary>Only filled in for a Barangay Business Clearance. A
        /// business request always needs validation before it can be cleared,
        /// which is why it goes to Processing instead of straight to Cleared.</summary>
        public BusinessDetails Business { get; set; }

        /// <summary>True when this request has to be checked by a person
        /// before it can be cleared (business clearances, and anything the
        /// clerk ticks as needing validation).</summary>
        public bool RequiresValidation { get; set; }

        // ---- filing time rule ----
        /// <summary>True when the request was filed between 8:00 AM and
        /// 4:00 PM. I keep the flag so the queue can explain itself later
        /// ("filed outside office hours - cleared the next working day").</summary>
        public bool FiledDuringOfficeWindow { get; set; }

        /// <summary>True when the request is still waiting for the next office
        /// window to open, so the screen can show "for clearing" instead of a
        /// bare "Pending".</summary>
        public bool WaitsForNextWindow
        {
            get
            {
                return Status == RequestStatus.Pending
                    && !RequiresValidation
                    && !FiledDuringOfficeWindow;
            }
        }

        // ---- release ----
        public DateTime? DateReleased { get; internal set; }
        public string ReleasedBy { get; internal set; }
        public string ReceivedBy { get; set; }

        public string RejectionReason { get; internal set; }
        public string Remarks { get; set; }

        public DateTime LastStatusChangeOn { get; internal set; }
        public string LastStatusChangeBy { get; internal set; }

        public IReadOnlyList<RequestStatusChange> History { get { return _history.AsReadOnly(); } }

        public DocumentRequest()
        {
            ReferenceNumber = string.Empty;
            ResidentName = string.Empty;
            Purpose = string.Empty;
            Detail = string.Empty;
            FeeBasis = string.Empty;
            OfficialReceiptNumber = string.Empty;
            OrControlNumber = string.Empty;
            CollectedBy = string.Empty;
            ReleasedBy = string.Empty;
            ReceivedBy = string.Empty;
            RejectionReason = string.Empty;
            Remarks = string.Empty;
            LastStatusChangeBy = string.Empty;
            DateRequested = DateTime.Now;
            Status = RequestStatus.Pending;
        }

        // ==================================================================
        //  Reading the request
        // ==================================================================

        public bool IsReleased { get { return Status == RequestStatus.Released; } }
        public bool IsRejected { get { return Status == RequestStatus.Rejected; } }
        public bool IsClosed { get { return IsReleased || IsRejected; } }
        public bool IsBusinessRequest { get { return DocumentType == DocumentType.BarangayBusinessClearance; } }

        /// <summary>True when there is still money to collect before the paper
        /// can be handed over.</summary>
        public bool HasUnsettledFee
        {
            get { return Fee > 0m && !IsPaid; }
        }

        /// <summary>The amount the resident still has to pay today.</summary>
        public decimal OutstandingAmount
        {
            get { return IsPaid ? 0m : Fee; }
        }

        /// <summary>What the clerk is allowed to see: the amount, and whether
        /// it has been paid. No sentence of law, no figure to argue with.</summary>
        public string GetFeeText()
        {
            if (Fee <= 0m) return "Free";
            return "P" + Fee.ToString("#,##0.00") + (IsPaid ? " (paid)" : string.Empty);
        }

        public string GetStatusText()
        {
            if (WaitsForNextWindow) return "Pending (next working day)";
            return EnumText.Of(Status);
        }

        public string GetDocumentName()
        {
            return EnumText.Spaced(DocumentType.ToString());
        }

        /// <summary>How many working days the request has been sitting on the
        /// counter. The queue colours anything past the RA 11032 limit.</summary>
        public int GetWaitingDays(DateTime today)
        {
            return Math.Max(0, (today.Date - DateRequested.Date).Days);
        }

        // ==================================================================
        //  Guards, in one place
        // ==================================================================

        /// <summary>
        /// Whether this request may be handed to the resident.
        ///
        /// I wrote it as a question that returns a reason rather than a plain
        /// true/false, because a clerk staring at a disabled button deserves
        /// to know what is missing. The screen shows that sentence.
        /// </summary>
        public bool CanRelease(out string reason)
        {
            if (Status == RequestStatus.Released)
            {
                reason = "This document was already released.";
                return false;
            }

            if (Status == RequestStatus.Rejected)
            {
                reason = "This request was rejected. File a new one instead of releasing this.";
                return false;
            }

            if (Status != RequestStatus.ReadyForRelease)
            {
                reason = "Only a request that is Ready for Release can be handed over. This one is "
                       + GetStatusText() + ".";
                return false;
            }

            if (HasUnsettledFee)
            {
                reason = "The fee of P" + Fee.ToString("#,##0.00")
                       + " has not been collected yet. Record the official receipt first.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        // ==================================================================
        //  Moving the request
        // ==================================================================

        /// <summary>Send the request to validation. Business clearances always
        /// come through here, because a business has to be inspected and its
        /// papers checked before the barangay signs.</summary>
        public void SendToProcessing(string reason, string changedBy, DateTime when)
        {
            Require(Status == RequestStatus.Pending || Status == RequestStatus.Processing,
                "Only a pending request can be sent for processing. This one is " + GetStatusText() + ".");
            Move(RequestStatus.Processing, reason, changedBy, when);
        }

        /// <summary>Approve the request. This is what the 8:00 AM to 4:00 PM
        /// rule does automatically for requests that need no validation, and
        /// what a validator does by hand for the rest.</summary>
        public void Clear(string reason, string changedBy, DateTime when)
        {
            Require(Status == RequestStatus.Pending || Status == RequestStatus.Processing,
                "Only a pending or processing request can be cleared. This one is " + GetStatusText() + ".");

            if (RequiresValidation && Status == RequestStatus.Pending)
                throw new InvalidOperationException(
                    "This request needs validation first, so it has to pass through Processing before it can be cleared.");

            Move(RequestStatus.Cleared, reason, changedBy, when);
        }

        /// <summary>Mark the document as printed and waiting on the counter.
        /// Money is checked here: I will not put a fee-bearing document in the
        /// ready tray before the receipt exists.</summary>
        public void MarkReadyForRelease(string changedBy, DateTime when)
        {
            Require(Status == RequestStatus.Cleared,
                "Only a cleared request can be made ready for release. This one is " + GetStatusText() + ".");
            Require(!HasUnsettledFee,
                "The fee of P" + Fee.ToString("#,##0.00") + " has to be collected before the document is prepared.");

            Move(RequestStatus.ReadyForRelease, string.Empty, changedBy, when);
        }

        /// <summary>Hand the document over. This is the last step; after this
        /// the request is history.</summary>
        public void Release(string receivedBy, string releasedBy, DateTime when)
        {
            string reason;
            if (!CanRelease(out reason)) throw new InvalidOperationException(reason);

            ReceivedBy = receivedBy == null ? string.Empty : receivedBy.Trim();
            DateReleased = when;
            ReleasedBy = releasedBy ?? string.Empty;
            Move(RequestStatus.Released, "Released to " + ReceivedBy, releasedBy, when);
        }

        /// <summary>Refuse the request. A reason is mandatory: a resident who
        /// is told "no" is always told why, and that sentence goes on the log.</summary>
        public void Reject(string reason, string changedBy, DateTime when)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("I need a reason before I can reject a request.");

            Require(!IsReleased, "A released document cannot be rejected.");
            Require(!IsRejected, "This request was already rejected.");

            Move(RequestStatus.Rejected, reason.Trim(), changedBy, when);
        }

        /// <summary>Put back into the queue when a request was rejected by
        /// mistake. Only the administrator gets to do this, and it is logged.</summary>
        public void ReopenFromRejection(string reason, string changedBy, DateTime when)
        {
            Require(IsRejected, "Only a rejected request can be reopened.");
            Move(RequestStatus.Pending, reason, changedBy, when);
        }

        // ==================================================================
        //  Payment
        // ==================================================================

        /// <summary>
        /// Record the collection. The official receipt number and its control
        /// number are both compulsory when money changes hands - that is the
        /// "Government OR control receipt" requirement, and it is also the
        /// only way a collection can be traced back to a booklet in an audit.
        /// </summary>
        public void RecordPayment(decimal amount, string orNumber, string orControlNumber,
                                  string collectedBy, DateTime when)
        {
            if (amount < 0m) throw new ArgumentOutOfRangeException("amount", "A collection cannot be negative.");
            if (amount < Fee)
                throw new InvalidOperationException("The collection of P" + amount.ToString("#,##0.00")
                    + " is short of the assessed fee of P" + Fee.ToString("#,##0.00") + ".");
            if (string.IsNullOrWhiteSpace(orNumber))
                throw new InvalidOperationException("The official receipt number is required.");
            if (string.IsNullOrWhiteSpace(orControlNumber))
                throw new InvalidOperationException("The control number of the receipt booklet is required.");

            IsPaid = true;
            OfficialReceiptNumber = orNumber.Trim();
            OrControlNumber = orControlNumber.Trim();
            PaymentDate = when;
            CollectedBy = collectedBy ?? string.Empty;
        }

        /// <summary>Marks the resident as having used the once-only RA 11261
        /// benefit. I call this at release time, not at filing time, because a
        /// request that is rejected must not consume the benefit.</summary>
        public void MarkJobseekerBenefitUsed()
        {
            if (AvailedUnderJobseekerAct) HasAvailedFirstTimeJobseeker = true;
        }

        /// <summary>
        /// Puts the request back to unpaid when the receipt that settled it has
        /// been voided.
        ///
        /// Without this, voiding a receipt would leave the request looking paid
        /// and the document could be handed over on a collection the barangay
        /// has already cancelled - which is exactly the hole an audit looks
        /// for. If the document was already prepared for release, it drops back
        /// to Cleared so it cannot be handed over either.
        /// </summary>
        public void ClearPaymentForVoidedReceipt(string changedBy, DateTime when)
        {
            IsPaid = false;
            OfficialReceiptNumber = string.Empty;
            OrControlNumber = string.Empty;
            PaymentDate = null;
            CollectedBy = string.Empty;

            if (Status == RequestStatus.ReadyForRelease)
                Move(RequestStatus.Cleared, "The receipt for this request was voided, so it went back to Cleared.",
                    changedBy, when);
        }

        public bool HasAvailedFirstTimeJobseeker { get; internal set; }

        public void AddJobseekerWaiver(string basis, decimal originalFee)
        {
            ApplyJobseekerWaiver = true;
            AvailedUnderJobseekerAct = true;
            AssessedAmount = originalFee;
        }

        // ==================================================================
        //  Internals
        // ==================================================================

        /// <summary>Filing-time decisions: the fee comes from the fee schedule,
        /// and the starting status comes from the time on the clock.</summary>
        internal void SetAssessment(decimal fee, string basis)
        {
            Fee = fee;
            FeeBasis = basis ?? string.Empty;
        }

        internal void SetInitialStatus(RequestStatus status, string reason, string changedBy, DateTime when)
        {
            Status = status;
            LastStatusChangeOn = when;
            LastStatusChangeBy = changedBy ?? string.Empty;
            _history.Add(new RequestStatusChange
            {
                Status = status,
                ChangedOn = when,
                ChangedBy = changedBy ?? string.Empty,
                Reason = reason ?? string.Empty
            });
        }

        internal void LoadHistory(IEnumerable<RequestStatusChange> history)
        {
            _history.Clear();
            if (history != null) _history.AddRange(history);
        }

        private void Move(RequestStatus status, string reason, string changedBy, DateTime when)
        {
            Status = status;
            LastStatusChangeOn = when;
            LastStatusChangeBy = changedBy ?? string.Empty;

            if (status == RequestStatus.Rejected) RejectionReason = reason ?? string.Empty;

            _history.Add(new RequestStatusChange
            {
                Status = status,
                ChangedOn = when,
                ChangedBy = changedBy ?? string.Empty,
                Reason = reason ?? string.Empty
            });
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        public override string ToString()
        {
            return ReferenceNumber + " - " + GetDocumentName() + " (" + GetStatusText() + ")";
        }
    }
}
