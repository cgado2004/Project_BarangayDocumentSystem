using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Service.Templates;

/// <summary>
/// RA 11261 First-Time Jobseeker certificate.
///
/// The wording deliberately states the statute, the six-month residency
/// finding, and first-time status, because NBI, PSA and BIR check for exactly
/// those. The Oath of Undertaking is appended by the renderer when
/// RequiresOath is honoured.
/// </summary>
public class JobseekerTemplate : IDocumentTemplate
{
    public DocumentType DocumentType => DocumentType.FirstTimeJobseekerCertificate;
    public string Title => "BARANGAY CERTIFICATION";
    public string? Subtitle => "(First Time Jobseeker)";

    public IEnumerable<string> BuildBody(DocumentRequest request, BarangayProfile profile)
    {
        var r = request.Resident;

        yield return "TO WHOM IT MAY CONCERN:";
        yield return $"This is to certify that {r.GetFullName().ToUpper()}, {r.GetAge()} years " +
                     $"old, Filipino citizen, is a bona fide resident of {r.Purok}, " +
                     $"{profile.BarangayName}, {profile.CityName}, Davao del Norte, and has been " +
                     $"residing herein since {r.DateOfResidency:MMMM d, yyyy}, which is more " +
                     $"than six (6) months.";
        yield return "This further certifies that the above-named person is a FIRST TIME " +
                     "JOBSEEKER and is actively seeking employment, and has not previously " +
                     "availed of the benefits under Republic Act No. 11261.";
        yield return "This certification is issued FREE OF CHARGE pursuant to Republic Act " +
                     "No. 11261, otherwise known as the FIRST TIME JOBSEEKERS ASSISTANCE ACT, " +
                     "and is valid for one (1) year from the date of issuance.";
    }
}
