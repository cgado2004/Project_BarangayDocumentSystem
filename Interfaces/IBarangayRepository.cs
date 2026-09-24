using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces
{
    public interface IBarangayRepository
    {
        IReadOnlyList<Resident> GetResidents();
        Resident GetResident(int residentId);
        void SaveResident(Resident resident);
        void DeleteResident(int residentId);
        IReadOnlyList<DocumentRequest> GetRequests();
        DocumentRequest GetRequest(int requestId);
        void SaveRequest(DocumentRequest request);
        void ExecuteInTransaction(Action action);
    }
}
