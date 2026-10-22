using System;
using System.Collections.Generic;
namespace BarangayDocumentSystem.Models;

/// <summary>
/// One request by a resident for one barangay document.
///
/// The status transitions are guarded: a released document cannot be
/// rejected, a rejected one cannot be released, and so on. The rules live
/// here rather than in the UI so they hold no matter who calls them.
///
///     Pending → Processing → ReadyForRelease → Released
///        └──────────┴───────────────┴────────→ Rejected
/// </summary>
public class DocumentRequest
{
    public int RequestId { get; private set; }

    /// <summary>The requesting resident. Never null.</summary>
    public Resident Resident { get; }

    public DocumentType DocumentType { get; }

    /// <summary>Why the document is needed — printed on the certificate.</summary>
    public string Purpose { get; set; } = string.Empty;

    public DateTime DateRequested { get; }
    public DateTime? DateReleased { get; private set; }

    public RequestStatus Status { get; private set; }

    /// <summary>Fee after all exemptions. Zero when waived.</summary>
    public decimal Fee { get; internal set; }

    /// <summary>Why the fee was waived or reduced, e.g. "RA 11261".</summary>
    public string FeeBasis { get; internal set; } = string.Empty;

    public bool IsPaid { get; private set; }

    /// <summary>Official receipt number, blank for zero-fee documents.</summary>
    public string OfficialReceiptNo { get; private set; } = string.Empty;

    /// <summary>When the fee was paid. Blank for free documents and for older payments.</summary>
    public DateTime? DatePaid { get; private set; }

    /// <summary>Name of the staff member who took the payment.</summary>
    public string ReceivedBy { get; private set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    internal DocumentRequest(
        int requestId, Resident resident, DocumentType documentType, string purpose,
        DateTime? dateRequested = null)
    {
        RequestId = requestId;
        Resident = resident ?? throw new ArgumentNullException(nameof(resident));
        DocumentType = documentType;
        Purpose = purpose;
        DateRequested = dateRequested ?? DateTime.Now;
        Status = RequestStatus.Pending;
    }

    /// <summary>
    /// Rebuilds a request exactly as it was stored in the database.
    ///
    /// The normal constructor always starts a request as Pending and unpaid,
    /// and the status setters are private on purpose (only the guarded
    /// transitions below may change them). Loading a row that is already
    /// Released or Paid therefore needs its own door in. It is `internal`,
    /// so only code in this project (the repository) can use it — the forms
    /// still cannot forge a request.
    /// </summary>
    internal static DocumentRequest Restore(
        int requestId, Resident resident, DocumentType documentType, string purpose,
        DateTime dateRequested, DateTime? dateReleased, RequestStatus status,
        decimal fee, string feeBasis, bool isPaid, string officialReceiptNo, string remarks,
        DateTime? datePaid = null, string receivedBy = "")
    {
        return new DocumentRequest(requestId, resident, documentType, purpose, dateRequested)
        {
            DateReleased = dateReleased,
            Status = status,
            Fee = fee,
            FeeBasis = feeBasis,
            IsPaid = isPaid,
            OfficialReceiptNo = officialReceiptNo,
            Remarks = remarks,
            DatePaid = datePaid,
            ReceivedBy = receivedBy ?? string.Empty
        };
    }

    /// <summary>Human-readable document name.</summary>
    public string GetDocumentName() => DocumentType switch
    {
        DocumentType.BarangayClearance             => "Barangay Clearance",
        DocumentType.CertificateOfResidency        => "Certificate of Residency",
        DocumentType.CertificateOfIndigency        => "Certificate of Indigency",
        DocumentType.BarangayBusinessClearance     => "Barangay Business Clearance",
        DocumentType.BarangayID                    => "Barangay ID",
        DocumentType.FirstTimeJobseekerCertificate => "First-Time Jobseeker Certificate",
        DocumentType.CertificateOfGoodMoralCharacter => "Certificate of Good Moral Character",
        _                                          => DocumentType.ToString()
    };

    // -----------------------------------------------------------------
    //  Status transitions — each guards its own preconditions
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
        // Older rows can still be Pending (the Start Processing button is gone),
        // so a pending request is moved through Processing on the way.
        if (Status == RequestStatus.Pending)
            StartProcessing();

        if (Status != RequestStatus.Processing)
            throw new InvalidOperationException(
                $"Only a request being processed can be marked ready. Current status: {Status}.");

        Status = RequestStatus.ReadyForRelease;
    }

    /// <summary>
    /// Releases the document. A fee-bearing request must be paid first —
    /// this is the control that stops documents leaving unpaid.
    /// </summary>
    public void Release()
    {
        if (!CanRelease(out string reason))
            throw new InvalidOperationException(reason);

        Status = RequestStatus.Released;
        DateReleased = DateTime.Now;

        // RA 11261 may be availed only once — record it against the resident.
        if (DocumentType == DocumentType.FirstTimeJobseekerCertificate)
            Resident.HasAvailedFirstTimeJobseeker = true;
    }

    /// <summary>
    /// Whether the document may be released now. The UI asks this BEFORE
    /// showing the release preview, so a clerk never confirms something that
    /// is about to be refused.
    /// </summary>
    public bool CanRelease(out string reason)
    {
        if (Status != RequestStatus.ReadyForRelease)
        {
            reason = $"Only a request that is ready for release can be released. Current status: {Status}.";
            return false;
        }

        if (Fee > 0 && !IsPaid)
        {
            reason = $"This document has an unpaid fee of ₱{Fee:N2}. Record the payment before releasing.";
            return false;
        }

        reason = string.Empty;
        return true;
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

    /// <summary>Records payment and the official receipt number.</summary>
    public void RecordPayment(string officialReceiptNo, string receivedBy = "")
    {
        if (Fee <= 0)
            throw new InvalidOperationException("This document carries no fee, so no payment is due.");

        if (IsPaid)
            throw new InvalidOperationException("This request has already been paid.");

        if (string.IsNullOrWhiteSpace(officialReceiptNo))
            throw new ArgumentException("An official receipt number is required.", nameof(officialReceiptNo));

        IsPaid = true;
        OfficialReceiptNo = officialReceiptNo.Trim();
        ReceivedBy = receivedBy?.Trim() ?? string.Empty;

        // MySQL DATETIME keeps whole seconds, so store exactly that.
        var now = DateTime.Now;
        DatePaid = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond);
    }

    /// <summary>Tracking reference, e.g. "BMP-2026-0042".</summary>
    public string GetReferenceNumber() => $"BMP-{DateRequested:yyyy}-{RequestId:D4}";

    public override string ToString() =>
        $"{GetReferenceNumber()} — {GetDocumentName()} for {Resident.GetFullName()} [{Status}]";
}

/// <summary>
/// Official receipt numbers. Format: OR-yyyyMMdd-N, for example OR-20261022-1,
/// OR-20261022-2 and so on. N starts at 1 each day and goes up with every payment.
/// </summary>
public static class OfficialReceiptNumber
{
    public static string Prefix(DateTime date) => $"OR-{date:yyyyMMdd}-";

    /// <summary>The next number not yet used on <paramref name="date"/>.</summary>
    public static string Next(IEnumerable<DocumentRequest> requests, DateTime date)
    {
        string prefix = Prefix(date);
        int highest = 0;

        foreach (var request in requests)
        {
            string no = request.OfficialReceiptNo;
            if (string.IsNullOrEmpty(no) || !no.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(no.Substring(prefix.Length), out int n) && n > highest)
                highest = n;
        }

        return prefix + (highest + 1);
    }
}
