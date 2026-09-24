using System;
using System.Collections.Generic;
using System.Linq;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Data
{
    public class InMemoryBarangayRepository : IBarangayRepository
    {
        private readonly List<Resident> residents = new List<Resident>();
        private readonly List<DocumentRequest> requests = new List<DocumentRequest>();
        private int nextResidentId = 1;
        private int nextRequestId = 1;

        // Copies keep unsaved form edits from changing stored records.
        public IReadOnlyList<Resident> GetResidents()
        {
            return residents.Select(resident => resident.Copy()).ToList().AsReadOnly();
        }

        public Resident GetResident(int residentId)
        {
            var resident = residents.FirstOrDefault(item => item.ResidentId == residentId);
            if (resident == null) throw new InvalidOperationException("The resident record no longer exists.");
            return resident.Copy();
        }

        public void SaveResident(Resident resident)
        {
            if (resident == null) throw new ArgumentNullException(nameof(resident));
            if (resident.ResidentId == 0)
            {
                resident.ResidentId = nextResidentId++;
                resident.Version = 1;
                residents.Add(resident.Copy());
                return;
            }

            int index = residents.FindIndex(item => item.ResidentId == resident.ResidentId);
            if (index < 0) throw new InvalidOperationException("The resident record no longer exists.");
            if (residents[index].Version != resident.Version)
                throw new InvalidOperationException("This resident was changed by another action. Refresh and reopen the record.");
            resident.Version++;
            residents[index] = resident.Copy();
        }

        public void DeleteResident(int residentId)
        {
            if (requests.Any(request => request.ResidentId == residentId))
                throw new InvalidOperationException("This resident has document requests. Keep the resident record to preserve their history.");
            var resident = residents.FirstOrDefault(item => item.ResidentId == residentId);
            if (resident == null) throw new InvalidOperationException("The resident record no longer exists.");
            residents.Remove(resident);
        }

        public IReadOnlyList<DocumentRequest> GetRequests()
        {
            return requests.Select(request => request.Copy()).ToList().AsReadOnly();
        }

        public DocumentRequest GetRequest(int requestId)
        {
            var request = requests.FirstOrDefault(item => item.RequestId == requestId);
            if (request == null) throw new InvalidOperationException("The document request no longer exists.");
            return request.Copy();
        }

        public void SaveRequest(DocumentRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.RequestId == 0)
            {
                request.RequestId = nextRequestId++;
                request.Version = 1;
                requests.Add(request.Copy());
                return;
            }

            int index = requests.FindIndex(item => item.RequestId == request.RequestId);
            if (index < 0) throw new InvalidOperationException("The document request no longer exists.");
            if (requests[index].Version != request.Version)
                throw new InvalidOperationException("This request was changed by another action. Refresh and try again.");
            request.Version++;
            requests[index] = request.Copy();
        }

        public void ExecuteInTransaction(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var savedResidents = residents.Select(resident => resident.Copy()).ToList();
            var savedRequests = requests.Select(request => request.Copy()).ToList();
            int savedResidentId = nextResidentId;
            int savedRequestId = nextRequestId;
            try { action(); }
            catch
            {
                residents.Clear();
                residents.AddRange(savedResidents);
                requests.Clear();
                requests.AddRange(savedRequests);
                nextResidentId = savedResidentId;
                nextRequestId = savedRequestId;
                throw;
            }
        }
    }
}
