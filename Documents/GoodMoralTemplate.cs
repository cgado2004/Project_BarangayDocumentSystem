using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Documents
{
    public class GoodMoralTemplate : IDocumentTemplate
    {
        public DocumentType DocumentType { get { return DocumentType.CertificateOfGoodMoralCharacter; } }
        public string Title { get { return "Certificate of Good Moral Character"; } }

        public string BuildBody(DocumentRequest request)
        {
            var resident = request.ResidentSnapshot;
            return "TO WHOM IT MAY CONCERN:\n\nThis certifies that " + resident.FullName.ToUpperInvariant() +
                ", residing at " + resident.FullAddress + ", is known to the issuing barangay as a person " +
                "of good moral character, subject to the authorized officer's review of available records and attestations.";
        }
    }
}
