namespace BarangayDocumentSystem.Core.Entities;

/// <summary>
/// One request by a resident for one barangay document.
///
/// I guard every status change here: a released document cannot be rejected,
/// a rejected one cannot be released, and so on. I deliberately put these
/// rules in this class rather than in a form, so they hold no matter who
/// calls them - including a screen I have not written yet.
///
///     Pending → Processing → ReadyForRelease → Released
///        └──────────┴───────────────┴────────→ Rejected
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
    public decimal Fee { get; internal set; }

    /// <summary>My reason for the amount above, for example "RA 11261". I keep
    /// it beside the fee so the two can never be separated.</summary>
    public string FeeBasis { get; internal set; } = string.Empty;

    public bool IsPaid { get; private set; }

    /// <summary>The official receipt number. I leave it blank for a zero-fee
    /// document, because there is no payment to trace.</summary>
    public string OfficialReceiptNo { get; private set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    /// <summary>
    /// Local or abroad, and only meaningful for a Barangay Clearance.
    ///
    /// I need this because the Citizen's Charter charges ₱100 for local
    /// employment and ₱200 for work abroad, so I cannot work the fee out from
    /// the document type on its own. I ignore it for every other document.
    /// </summary>
    public ClearanceScope Scope { get; set; } = ClearanceScope.Local;

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

    /// <summary>The readable name of this document, which I use on screen and
    /// on the printed page.</summary>
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
                $"This document has an unpaid fee of {Rules.DisplayFormat.Peso(Fee)}. Record the payment before releasing.");

        Status = RequestStatus.Released;
        DateReleased = DateTime.Now;

        // RA 11261 may be availed only once, so I mark the resident here.
        // If I forgot this, the same person could come back next month and
        // claim the benefit all over again.
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
    /// So I gave loading its own door. I marked it internal, which means only
    /// code inside this Core project can open it. The screens still cannot
    /// set a status without going through the proper method.
    /// </summary>
    internal static DocumentRequest Rehydrate(
        int requestId, Resident resident, DocumentType documentType, string purpose,
        DateTime dateRequested, DateTime? dateReleased, RequestStatus status,
        decimal fee, string feeBasis, bool isPaid, string officialReceiptNo, string remarks)
    {
        var request = new DocumentRequest(requestId, resident, documentType, purpose)
        {
            DateRequested     = dateRequested,
            DateReleased      = dateReleased,
            Status            = status,
            Fee               = fee,
            FeeBasis          = feeBasis,
            IsPaid            = isPaid,
            OfficialReceiptNo = officialReceiptNo,
            Remarks           = remarks
        };
        return request;
    }
}
