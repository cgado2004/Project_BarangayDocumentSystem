using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Documents
{
    public class ResidencyTemplate : IDocumentTemplate
    {
        public DocumentType DocumentType { get { return DocumentType.CertificateOfResidency; } }
        public string Title { get { return "Certificate of Residency"; } }

        public string BuildBody(DocumentRequest request)
        {
            var resident = request.ResidentSnapshot;
            return "TO WHOM IT MAY CONCERN:\n\nThis certifies that " + resident.FullName.ToUpperInvariant() +
                " is a resident of " + resident.FullAddress + " and has resided in this barangay since " +
                resident.DateOfResidency.ToString("MMMM d, yyyy") + ".";
        }
    }
}
