using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Documents
{
    public class BarangayIdTemplate : IDocumentTemplate
    {
        public DocumentType DocumentType { get { return DocumentType.BarangayId; } }
        public string Title { get { return "Barangay ID"; } }

        public string BuildBody(DocumentRequest request)
        {
            var resident = request.ResidentSnapshot;
            return "RESIDENT IDENTIFICATION RECORD\n\nName: " + resident.FullName.ToUpperInvariant() +
                "\nDate of birth: " + resident.DateOfBirth.ToString("MMMM d, yyyy") +
                "\nGender: " + resident.Gender + "\nCivil status: " + resident.CivilStatus +
                "\nAddress: " + resident.FullAddress + "\nContact: " + resident.ContactNumber +
                "\nResident since: " + resident.DateOfResidency.ToString("MMMM d, yyyy") +
                "\n\nHolder's signature: ______________________________\nPhoto: to be attached by the issuing barangay.";
        }
    }
}
