using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Documents
{
    public class JobseekerTemplate : IDocumentTemplate
    {
        public DocumentType DocumentType { get { return DocumentType.FirstTimeJobseekerCertificate; } }
        public string Title { get { return "First-Time Jobseeker Certificate"; } }

        public string BuildBody(DocumentRequest request)
        {
            var resident = request.ResidentSnapshot;
            return "TO WHOM IT MAY CONCERN:\n\nThis certifies that " + resident.FullName.ToUpperInvariant() +
                ", resident of " + resident.FullAddress + " since " + resident.DateOfResidency.ToString("MMMM d, yyyy") +
                ", has completed at least six months of residency and has declared that they are seeking employment " +
                "for the first time. This certification is issued without charge under RA 11261.\n\n" +
                "OATH OF UNDERTAKING\n\nI, " + resident.FullName + ", declare that I am a Filipino citizen " +
                "seeking work for the first time and have not previously used this benefit. I will present this " +
                "certification only for eligible employment requirements, provide truthful information, and " +
                "accept responsibility for false declarations.\n\nApplicant's signature: ______________________________" +
                "\nDate signed: ______________________________\nWitness / authorized officer: ______________________________";
        }
    }
}
