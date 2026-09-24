using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Documents
{
    public class ClearanceTemplate : IDocumentTemplate
    {
        public DocumentType DocumentType { get { return DocumentType.BarangayClearance; } }
        public string Title { get { return "Barangay Clearance"; } }

        public string BuildBody(DocumentRequest request)
        {
            var resident = request.ResidentSnapshot;
            return "TO WHOM IT MAY CONCERN:\n\nThis certifies that " + resident.FullName.ToUpperInvariant() +
                ", aged " + resident.GetAge(request.DateReleased ?? request.DateRequested) +
                " and residing at " + resident.FullAddress +
                ", has been cleared by the issuing barangay for the purpose stated below. " +
                "The authorized officer must verify any pending case before signing this clearance.";
        }
    }
}
