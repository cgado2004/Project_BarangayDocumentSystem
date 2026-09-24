using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Documents
{
    public class BusinessClearanceTemplate : IDocumentTemplate
    {
        public DocumentType DocumentType { get { return DocumentType.BarangayBusinessClearance; } }
        public string Title { get { return "Barangay Business Clearance"; } }

        public string BuildBody(DocumentRequest request)
        {
            var resident = request.ResidentSnapshot;
            return "TO WHOM IT MAY CONCERN:\n\nThis barangay clearance is issued for the following business, " +
                "subject to verification and applicable permits:\n\nBusiness name: " + request.BusinessName +
                "\nBusiness address: " + request.BusinessAddress + "\nNature of business: " + request.BusinessNature +
                "\nOwner / applicant: " + resident.FullName + "\n\nThis clearance does not replace a business permit.";
        }
    }
}
