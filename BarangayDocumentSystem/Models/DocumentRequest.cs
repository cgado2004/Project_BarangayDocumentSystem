using BarangayDocumentSystem.BusinessRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
namespace BarangayDocumentSystem.Models;

/// <summary>
/// Everything a request needs beside its type, carried as one object.
///
/// The v3.1 fee schedule has documents whose price is not fixed: the
/// business clearance varies with the law violated, the cedula is computed
/// from sworn income, facility use is charged by the hour, and the Barangay
/// Taripa items are assessed case by case. Rather than grow a method
/// signature a parameter at a time, one record carries all of it, and the
/// fields that do not apply to a document are simply left at their
/// defaults.
/// </summary>
public sealed record RequestInput(
    ClearanceScope Scope = ClearanceScope.Local,

    /// <summary>The assessed amount, for a document priced by the clerk -
    /// a business clearance under a violated ordinance, or a Taripa
    /// line. Zero means "use the standard rate".</summary>
    decimal Amount = 0m,

    /// <summary>Hours of barangay facility use. Charged per hour or part
    /// of an hour.</summary>
    decimal Hours = 0m,

    /// <summary>The declarant's sworn gross annual income, for the
    /// community tax computation under RA 7160 Sec. 156.</summary>
    decimal GrossAnnualIncome = 0m,

    /// <summary>Free text that travels with the fee: the law or ordinance
    /// violated, the Taripa item, or the facility used.</summary>
    string Detail = "",

    /// <summary>Set when a first-time jobseeker asks for a Barangay
    /// Clearance and chooses to claim the RA 11261 one-time waiver, which
    /// the law grants for the clearance as well as the certificate.</summary>
    bool ApplyJobseekerWaiver = false)
{
    public static RequestInput Default { get; } = new();
}

/// <summary>
/// One request by a resident for one barangay document.
///
/// I guard every status change here: a released document cannot be
/// rejected, a rejected one cannot be released, and so on. I deliberately
/// put these rules in this class rather than in a form, so they hold no
/// matter who calls them - including a screen I have not written yet.
///
///     Pending → Processing → ReadyForRelease → Released
///        └──────────┴───────────────┴────────→ Rejected
///
/// v3.1 also makes this class the home of the fee result: the repository
/// applies a FeeAssessment from FeeSchedule through ApplyAssessment, and
/// from then on the amount and its legal basis travel together.
/// </summary>
public class DocumentRequest
{
    public int RequestId { get; private set; }

    /// <summary>The resident who asked for this. I never allow it to be null -
    /// a document with nobody attached to it is meaningless.</summary>
    public Resident Resident { get; }

    public DocumentType DocumentType { get; }

    /// <summary>Why the document is needed. I print this on the certificate
    /// itself, so it has to be something fit to appear on paper.</summary>
    public string Purpose { get; set; } = string.Empty;

    public DateTime DateRequested { get; private set; }
    public DateTime? DateReleased { get; private set; }

    public RequestStatus Status { get; private set; }

    /// <summary>The fee after I have applied every exemption. Zero when I have
    /// waived it entirely.</summary>
    public decimal Fee { get; private set; }

    /// <summary>My reason for the amount above, for example "RA 11261". I keep
    /// it beside the fee so the two can never be separated.</summary>
    public string FeeBasis { get; private set; } = string.Empty;

    public bool IsPaid { get; private set; }

    /// <summary>The official receipt number. I leave it blank for a zero-fee
    /// document, because there is no payment to trace.</summary>
    public string OfficialReceiptNo { get; private set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    /// <summary>
    /// Local or abroad, and only meaningful for a Barangay Clearance.
    ///
    /// I need this because the Citizen's Charter charges ₱100 for local
    /// employment and ₱200 for work abroad, so I cannot work the fee out
    /// from the document type on its own. The value lives on the input
    /// record; this property reads it back.
    /// </summary>
    public ClearanceScope Scope => Input.Scope;

    /// <summary>The circumstances the fee was assessed against - the entered
    /// amount, hours, sworn income and free-text detail.</summary>
    public RequestInput Input { get; private set; }

    /// <summary>
    /// True when this request consumed the resident's once-only RA 11261
    /// benefit - either the First-Time Jobseeker Certificate itself, or a
    /// Barangay Clearance issued under the jobseeker waiver. Release()
    /// marks the resident from this flag.
    /// </summary>
    public bool AvailedUnderJobseekerAct { get; private set; }

    /// <summary>
    /// A brand-new, Pending request.
    ///
    /// Only the repository calls this (it is internal), because the store is
    /// the one that hands out ids and prices the request. The filing time is
    /// a parameter rather than always DateTime.Now so that the repository can
    /// write the same instant to MySQL that it keeps in memory - MySQL's
    /// DATETIME has whole-second precision, and the two must not disagree -
    /// and so the sample data can file one request in the past for the RA
    /// 11032 aging demonstration.
    /// </summary>
    internal DocumentRequest(
        int requestId, Resident resident, DocumentType documentType, string purpose,
        RequestInput? input = null, DateTime? filedOn = null)
    {
        RequestId = requestId;
        Resident = resident ?? throw new ArgumentNullException(nameof(resident));
        DocumentType = documentType;
        Purpose = purpose;
        Input = input ?? RequestInput.Default;
        DateRequested = filedOn ?? DateTime.Now;
        Status = RequestStatus.Pending;
    }

    /// <summary>The readable name of this document, which I use on screen and
    /// on the printed page.</summary>
    public string GetDocumentName() => FeeSchedule.NameOf(DocumentType);

    // -----------------------------------------------------------------
    //  The fee result, applied once at filing time by the repository.
    // -----------------------------------------------------------------

    /// <summary>
    /// I write the assessment's amount and legal basis onto the request in
    /// one move, so the two can never disagree. The screens never call
    /// this - only the store that creates the request does.
    /// </summary>
    internal void ApplyAssessment(BusinessRules.FeeAssessment assessment)
    {
        if (assessment is null) throw new ArgumentNullException(nameof(assessment));
        Fee = assessment.FinalFee;
        FeeBasis = assessment.Basis;
        AvailedUnderJobseekerAct = assessment.MarksJobseekerAvailment;
    }

    // -----------------------------------------------------------------
    //  Status transitions. Each one checks its own preconditions before it
    //  changes anything, so an illegal move fails loudly instead of quietly.
    // -----------------------------------------------------------------
    public void StartProcessing()
    {
        if (Status != RequestStatus.Pending)
            throw new InvalidOperationException(
                $"Only a pending request can be moved to processing. Current status: {Status}.");

        Status = RequestStatus.Processing;
    }

    public void MarkReadyForRelease()
    {
        if (Status != RequestStatus.Processing)
            throw new InvalidOperationException(
                $"Only a request being processed can be marked ready. Current status: {Status}.");

        Status = RequestStatus.ReadyForRelease;
    }

    /// <summary>
    /// I release the document.
    ///
    /// A fee-bearing request must be paid first. This is the single most
    /// important check I wrote: it is what stops a document leaving the office
    /// without the money being recorded against a receipt.
    /// </summary>
    public void Release()
    {
        if (Status != RequestStatus.ReadyForRelease)
            throw new InvalidOperationException(
                $"Only a request that is ready for release can be released. Current status: {Status}.");

        if (Fee > 0 && !IsPaid)
            throw new InvalidOperationException(
                $"This document has an unpaid fee of {BusinessRules.DisplayFormat.Peso(Fee)}. Record the payment before releasing.");

        Status = RequestStatus.Released;
        DateReleased = DateTime.Now;

        // RA 11261 may be availed only once, so I mark the resident here -
        // whether the benefit was claimed on the certificate itself or on
        // a barangay clearance issued under the waiver. If I forgot this,
        // the same person could come back next month and claim it again.
        if (DocumentType == DocumentType.FirstTimeJobseekerCertificate || AvailedUnderJobseekerAct)
            Resident.HasAvailedFirstTimeJobseeker = true;
    }

    public void Reject(string reason)
    {
        if (Status == RequestStatus.Released)
            throw new InvalidOperationException("A released document cannot be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason is required when rejecting a request.", nameof(reason));

        Status = RequestStatus.Rejected;
        Remarks = reason;
    }

    /// <summary>I record the payment and the official receipt number together.
    /// One without the other would leave money I cannot account for.</summary>
    public void RecordPayment(string officialReceiptNo)
    {
        if (Fee <= 0)
            throw new InvalidOperationException("This document carries no fee, so no payment is due.");

        if (IsPaid)
            throw new InvalidOperationException("This request has already been paid.");

        if (string.IsNullOrWhiteSpace(officialReceiptNo))
            throw new ArgumentException("An official receipt number is required.", nameof(officialReceiptNo));

        IsPaid = true;
        OfficialReceiptNo = officialReceiptNo.Trim();
    }

    /// <summary>The tracking reference I give the resident, for example
    /// "BMP-2026-0042". I build it from the year and the id rather than
    /// storing it, so it can never disagree with the row it belongs to.</summary>
    public string GetReferenceNumber() => $"BMP-{DateRequested:yyyy}-{RequestId:D4}";

    // -----------------------------------------------------------------
    //  RA 11032 service standard
    // -----------------------------------------------------------------

    /// <summary>
    /// Working days (Monday to Friday) the request has been in the queue.
    ///
    /// RA 11032, the Ease of Doing Business Act, prescribes three working
    /// days for a simple frontline transaction, and a barangay document is
    /// one. I count weekdays only and I do not subtract Philippine public
    /// holidays - that would need a holiday table I do not have - so the
    /// count is the cautious upper bound, which is the right direction to
    /// err in when the law sets a deadline.
    /// </summary>
    public int WorkingDaysInQueue() =>
        WorkingDaysBetween(DateRequested, DateTime.Now);

    /// <summary>True when this open request has sat longer than the RA 11032
    /// standard for a simple transaction.</summary>
    public bool IsBeyondRA11032Standard(int simpleWorkingDays)
    {
        if (Status is RequestStatus.Released or RequestStatus.Rejected) return false;
        return WorkingDaysInQueue() > simpleWorkingDays;
    }

    public static int WorkingDaysBetween(DateTime from, DateTime to)
    {
        int days = 0;
        var day = from.Date;
        var end = to.Date;

        while (day < end)
        {
            day = day.AddDays(1);
            if (day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                days++;
        }

        return days;
    }

    public override string ToString() =>
        $"{GetReferenceNumber()} — {GetDocumentName()} for {Resident.GetFullName()} [{Status}]";

    // -----------------------------------------------------------------
    //  Rebuilding a request that came back out of the database
    // -----------------------------------------------------------------

    /// <summary>
    /// I rebuild a request from a row that is already saved in MySQL.
    ///
    /// WHY I NEEDED THIS. Status, DateRequested, DateReleased, IsPaid and
    /// OfficialReceiptNo all have private setters on purpose, so the only way
    /// to change them is through StartProcessing, Release and the rest - and
    /// every one of those checks my rules first. That is exactly what I want
    /// while the program is running.
    ///
    /// But a row coming back out of the database has ALREADY been through all
    /// of that. If I had to replay those steps to load an old released
    /// request, I would be re-running today's rules against history, and my
    /// unpaid-release check would reject rows that were legitimately released
    /// years ago.
    ///
    /// So I gave loading its own door. It is public only because my RuleChecks
    /// harness lives in its own assembly and needs to build historical rows
    /// for its checks; no screen in this project calls it. The screens still
    /// cannot set a status without going through the proper method.
    /// </summary>
    public static DocumentRequest Rehydrate(
        int requestId, Resident resident, DocumentType documentType, string purpose,
        DateTime dateRequested, DateTime? dateReleased, RequestStatus status,
        decimal fee, string feeBasis, bool isPaid, string officialReceiptNo, string remarks,
        RequestInput? input = null, bool availedUnderJobseekerAct = false)
    {
        var request = new DocumentRequest(requestId, resident, documentType, purpose, input)
        {
            DateRequested             = dateRequested,
            DateReleased              = dateReleased,
            Status                    = status,
            Fee                       = fee,
            FeeBasis                  = feeBasis,
            IsPaid                    = isPaid,
            OfficialReceiptNo         = officialReceiptNo,
            Remarks                   = remarks,
            AvailedUnderJobseekerAct  = availedUnderJobseekerAct
        };
        return request;
    }
}
