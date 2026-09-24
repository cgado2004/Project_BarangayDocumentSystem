using System;
using System.Collections.Generic;
using System.Linq;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services
{
    public class ReportingService
    {
        private readonly IBarangayRepository repository;

        public ReportingService(IBarangayRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public BarangayStatistics GetStatistics()
        {
            var residents = repository.GetResidents();
            var requests = repository.GetRequests();
            return new BarangayStatistics
            {
                TotalResidents = residents.Count,
                TotalRequests = requests.Count,
                PendingRequests = requests.Count(request => request.Status == RequestStatus.Pending),
                ReadyRequests = requests.Count(request => request.Status == RequestStatus.ReadyForRelease),
                FreeDocumentsReleased = requests.Count(request => request.Status == RequestStatus.Released && request.Fee == 0m),
                TotalCollected = requests.Where(request => request.IsPaid).Sum(request => request.Fee),
                RequestsByStatus = Enum.GetValues(typeof(RequestStatus)).Cast<RequestStatus>()
                    .Select(status => new KeyValuePair<string, int>(
                        status == RequestStatus.ReadyForRelease ? "Ready for Release" : status.ToString(),
                        requests.Count(request => request.Status == status))).ToList(),
                RequestsByDocument = requests.GroupBy(request => request.DocumentName)
                    .Select(group => new KeyValuePair<string, int>(group.Key, group.Count())).ToList(),
                ResidentsByPurok = residents.GroupBy(resident => resident.Purok).OrderBy(group => group.Key)
                    .Select(group => new KeyValuePair<string, int>(group.Key, group.Count())).ToList()
            };
        }
    }
}
