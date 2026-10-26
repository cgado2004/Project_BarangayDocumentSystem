using System;
using System.Collections.Generic;
using System.Linq;
namespace BarangayDocumentSystem.Models;











public class DocumentRequest
{
    public int RequestId { get; private set; }


    public Resident Resident { get; }

    public DocumentType DocumentType { get; }


    public string Purpose { get; set; } = string.Empty;

    public DateTime DateRequested { get; }
    public DateTime? DateReleased { get; private set; }

    public RequestStatus Status { get; private set; }


    public decimal Fee { get; internal set; }


    public string FeeBasis { get; internal set; } = string.Empty;

    public bool IsPaid { get; private set; }


    public string OfficialReceiptNo { get; private set; } = string.Empty;
    public DateTime? DatePaid { get; private set; }
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




    public void StartProcessing()
    {
        if (Status != RequestStatus.Pending)
            throw new InvalidOperationException(
                $"Only a pending request can be moved to processing. Current status: {Status}.");

        Status = RequestStatus.Processing;
    }

    public void MarkReadyForRelease()
    {


        if (Status == RequestStatus.Pending)
            StartProcessing();

        if (Status != RequestStatus.Processing)
            throw new InvalidOperationException(
                $"Only a request being processed can be marked ready. Current status: {Status}.");

        Status = RequestStatus.ReadyForRelease;
    }





    public void Release()
    {
        if (!CanRelease(out string reason))
            throw new InvalidOperationException(reason);

        Status = RequestStatus.Released;
        DateReleased = DateTime.Now;


        if (DocumentType == DocumentType.FirstTimeJobseekerCertificate)
            Resident.HasAvailedFirstTimeJobseeker = true;
    }






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
        var now = DateTime.Now;
        DatePaid = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond);
    }


    public string GetReferenceNumber() => $"BMP-{DateRequested:yyyy}-{RequestId:D4}";

    public override string ToString() =>
        $"{GetReferenceNumber()} — {GetDocumentName()} for {Resident.GetFullName()} [{Status}]";
}

public static class OfficialReceiptNumber
{
    public static string Prefix(DateTime date) => $"OR-{date:yyyyMMdd}-";

    public static string Next(IEnumerable<DocumentRequest> requests, DateTime date)
    {
        string prefix = Prefix(date);
        int highest = requests.Select(request => request.OfficialReceiptNo)
            .Where(number => number.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(number => int.TryParse(number.Substring(prefix.Length), out int value) ? value : 0)
            .DefaultIfEmpty(0).Max();
        return prefix + (highest + 1);
    }
}
