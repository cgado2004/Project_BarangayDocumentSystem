using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Documents
{
    public class IndigencyTemplate : IDocumentTemplate
    {
        public DocumentType DocumentType { get { return DocumentType.CertificateOfIndigency; } }
        public string Title { get { return "Certificate of Indigency"; } }

        public string BuildBody(DocumentRequest request)
        {
            var resident = request.ResidentSnapshot;
            return "TO WHOM IT MAY CONCERN:\n\nThis certifies that " + resident.FullName.ToUpperInvariant() +
                ", residing at " + resident.FullAddress + ", is requesting certification of indigency " +
                "for the purpose stated below. The issuing officer certifies indigent status after verification " +
                "of the supporting information. This classroom certificate carries no fee.";
        }
    }
}
