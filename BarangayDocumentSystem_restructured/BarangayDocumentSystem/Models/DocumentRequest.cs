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

    public string Remarks { get; set; } = string.Empty;

    internal DocumentRequest(
        int requestId, Resident resident, DocumentType documentType, string purpose)
    {
        RequestId = requestId;
        Resident = resident ?? throw new ArgumentNullException(nameof(resident));
        DocumentType = documentType;
        Purpose = purpose;
        DateRequested = DateTime.Now;
        Status = RequestStatus.Pending;
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
        if (Status != RequestStatus.ReadyForRelease)
            throw new InvalidOperationException(
                $"Only a request that is ready for release can be released. Current status: {Status}.");

        if (Fee > 0 && !IsPaid)
            throw new InvalidOperationException(
                $"This document has an unpaid fee of ₱{Fee:N2}. Record the payment before releasing.");

        Status = RequestStatus.Released;
        DateReleased = DateTime.Now;

        // RA 11261 may be availed only once — record it against the resident.
        if (DocumentType == DocumentType.FirstTimeJobseekerCertificate)
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

    /// <summary>Records payment and the official receipt number.</summary>
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

    /// <summary>Tracking reference, e.g. "BMP-2026-0042".</summary>
    public string GetReferenceNumber() => $"BMP-{DateRequested:yyyy}-{RequestId:D4}";

    public override string ToString() =>
        $"{GetReferenceNumber()} — {GetDocumentName()} for {Resident.GetFullName()} [{Status}]";
}
