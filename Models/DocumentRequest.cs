using System;

namespace BarangayDocumentSystem.Models
{
    public class DocumentRequest
    {
        public int RequestId { get; internal set; }
        // Stops old changes from replacing newer ones.
        public int Version { get; internal set; }
        public int ResidentId { get; internal set; }
        public Resident ResidentSnapshot { get; internal set; }
        public DocumentType DocumentType { get; internal set; }
        public string DocumentName { get; internal set; }
        public string Purpose { get; internal set; }
        public string BusinessName { get; internal set; }
        public string BusinessAddress { get; internal set; }
        public string BusinessNature { get; internal set; }
        public DateTime DateRequested { get; internal set; }
        public DateTime? DateReleased { get; internal set; }
        public RequestStatus Status { get; internal set; }
        public decimal Fee { get; internal set; }
        public string FeeBasis { get; internal set; }
        public bool IsPaid { get; internal set; }
        public string OfficialReceiptNumber { get; internal set; } = "";
        public DateTime? DatePaid { get; internal set; }
        public string RejectionReason { get; internal set; } = "";
        public string ReleasedDocumentText { get; internal set; } = "";

        public string ResidentName { get { return ResidentSnapshot.FullName; } }
        public string PaymentText { get { return Fee == 0m ? "Free" : (IsPaid ? "Paid" : "Unpaid"); } }

        public string ReferenceNumber
        {
            get { return "BMP-" + DateRequested.Year + "-" + RequestId.ToString("D4"); }
        }

        public string StatusText
        {
            get { return Status == RequestStatus.ReadyForRelease ? "Ready for Release" : Status.ToString(); }
        }

        public DocumentRequest Copy()
        {
            var copy = (DocumentRequest)MemberwiseClone();
            copy.ResidentSnapshot = ResidentSnapshot.Copy();
            return copy;
        }
    }
}
