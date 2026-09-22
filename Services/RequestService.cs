using System;
using System.Collections.Generic;
using System.Linq;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services
{
    public class RequestService
    {
        private readonly IBarangayRepository repository;
        private readonly FeeSchedule feeSchedule;
        private readonly DocumentRenderer renderer;

        public RequestService(IBarangayRepository repository, FeeSchedule feeSchedule, DocumentRenderer renderer)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.feeSchedule = feeSchedule ?? throw new ArgumentNullException(nameof(feeSchedule));
            this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public List<DocumentRequest> Search(string searchText = "", RequestStatus? status = null)
        {
            string term = (searchText ?? "").Trim();
            return repository.GetRequests()
                .Where(request => !status.HasValue || request.Status == status.Value)
                .Where(request => (request.ReferenceNumber + " " + request.ResidentSnapshot.FullName + " " + request.DocumentName)
                    .IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(request => request.RequestId).ToList();
        }

        public DocumentRequest Get(int requestId) { return repository.GetRequest(requestId); }

        public FeeAssessment Assess(int residentId, DocumentType type)
        {
            var resident = repository.GetResident(residentId);
            if (type == DocumentType.FirstTimeJobseekerCertificate)
            {
                EnsureNoOtherJobseekerRequest(residentId, 0);
            }
            return feeSchedule.Assess(resident, type);
        }

        public DocumentRequest Create(RequestDetails details)
        {
            if (details == null) throw new ArgumentException("Enter the request details.");
            var resident = repository.GetResident(details.ResidentId);
            string purpose = ResidentValidator.Required(details.Purpose, "Purpose", 300);
            string businessName = "";
            string businessAddress = "";
            string businessNature = "";
            if (details.DocumentType == DocumentType.BarangayBusinessClearance)
            {
                businessName = ResidentValidator.Required(details.BusinessName, "Business name", 120);
                businessAddress = ResidentValidator.Required(details.BusinessAddress, "Business address", 250);
                businessNature = ResidentValidator.Required(details.BusinessNature, "Nature of business", 150);
            }
            var assessment = Assess(resident.ResidentId, details.DocumentType);
            var request = new DocumentRequest
            {
                ResidentId = resident.ResidentId,
                ResidentSnapshot = resident.Copy(),
                DocumentType = details.DocumentType,
                DocumentName = renderer.GetTemplate(details.DocumentType).Title,
                Purpose = purpose,
                BusinessName = businessName,
                BusinessAddress = businessAddress,
                BusinessNature = businessNature,
                DateRequested = DateTime.Now,
                Status = RequestStatus.Pending,
                Fee = assessment.Amount,
                FeeBasis = assessment.Basis
            };
            repository.SaveRequest(request);
            return request.Copy();
        }

        public void StartProcessing(int requestId)
        {
            var request = Get(requestId);
            RequireStatus(request, RequestStatus.Pending);
            request.Status = RequestStatus.Processing;
            repository.SaveRequest(request);
        }

        public void MarkReady(int requestId)
        {
            var request = Get(requestId);
            RequireStatus(request, RequestStatus.Processing);
            request.Status = RequestStatus.ReadyForRelease;
            repository.SaveRequest(request);
        }

        public void Release(int requestId)
        {
            var request = Get(requestId);
            RequireStatus(request, RequestStatus.ReadyForRelease);
            if (request.Fee > 0m && !request.IsPaid)
                throw new InvalidOperationException("Record payment and an official receipt number before releasing this document.");

            Resident resident = null;
            if (request.DocumentType == DocumentType.FirstTimeJobseekerCertificate)
            {
                resident = repository.GetResident(request.ResidentId);
                feeSchedule.ValidateJobseeker(resident);
                EnsureNoOtherJobseekerRequest(resident.ResidentId, request.RequestId);
            }

            request.DateReleased = DateTime.Now;
            request.Status = RequestStatus.Released;
            // Store the final text so later profile or resident edits cannot rewrite history.
            request.ReleasedDocumentText = renderer.Render(request);
            repository.SaveRequest(request);
            if (resident != null)
            {
                resident.HasUsedJobseekerBenefit = true;
                repository.SaveResident(resident);
            }
        }

        public void RecordPayment(int requestId, string receiptNumber)
        {
            var request = Get(requestId);
            if (request.Status == RequestStatus.Rejected || request.Status == RequestStatus.Released)
                throw new InvalidOperationException("Payments cannot be added to rejected or released requests.");
            if (request.Fee == 0m) throw new InvalidOperationException("This request is free. No payment is needed.");
            if (request.IsPaid) throw new InvalidOperationException("Payment is already recorded for this request.");
            string receipt = ResidentValidator.Required(receiptNumber, "Official receipt number", 50);
            if (repository.GetRequests().Any(item => item.IsPaid &&
                string.Equals(item.OfficialReceiptNumber, receipt, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("That official receipt number is already used by another request.");
            request.IsPaid = true;
            request.OfficialReceiptNumber = receipt;
            request.DatePaid = DateTime.Now;
            repository.SaveRequest(request);
        }

        public void Reject(int requestId, string reason)
        {
            var request = Get(requestId);
            if (request.Status == RequestStatus.Released || request.Status == RequestStatus.Rejected)
                throw new InvalidOperationException("Released and rejected requests cannot be changed.");
            request.RejectionReason = ResidentValidator.Required(reason, "Rejection reason", 300);
            request.Status = RequestStatus.Rejected;
            repository.SaveRequest(request);
        }

        public string Preview(int requestId)
        {
            return renderer.Render(Get(requestId));
        }

        private void EnsureNoOtherJobseekerRequest(int residentId, int currentRequestId)
        {
            if (repository.GetRequests().Any(request => request.ResidentId == residentId &&
                request.RequestId != currentRequestId &&
                request.DocumentType == DocumentType.FirstTimeJobseekerCertificate &&
                request.Status != RequestStatus.Rejected))
                throw new InvalidOperationException("This resident already has an active or released first-time jobseeker request.");
        }

        private static void RequireStatus(DocumentRequest request, RequestStatus required)
        {
            if (request.Status != required)
                throw new InvalidOperationException("This action requires a " +
                    (required == RequestStatus.ReadyForRelease ? "Ready for Release" : required.ToString()) +
                    " request. Current status: " + request.StatusText + ".");
        }
    }
}
