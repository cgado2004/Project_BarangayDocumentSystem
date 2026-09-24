using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Service.Templates;

/// <summary>
/// The Barangay Clearance.
///
/// The charter prices it two ways - ₱100 for local employment, ₱200 for
/// work abroad - so the template prints the scope on the face of the
/// document. That way the paper itself shows which rate was charged, and
/// nobody has to take the receipt's word for it.
/// </summary>
public sealed class BarangayClearanceTemplate : DocumentTemplateBase
{
    public BarangayClearanceTemplate() : base(DocumentType.BarangayClearance) { }

    /// <summary>The scope goes on the face of the paper, right under the
    /// title - "For Local Employment" or "For Employment Abroad" - because
    /// the charter prices the two differently and the certificate should
    /// show which one was issued.</summary>
    public override string? SubtitleFor(DocumentRequest request) =>
        request.Scope == ClearanceScope.Abroad
            ? "For Employment Abroad"
            : "For Local Employment";

    protected override string Noun => "clearance";

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        yield return ResidentIntroduction(request, barangay);
        yield return "";

        string scope = request.Scope == ClearanceScope.Abroad
            ? "for EMPLOYMENT ABROAD"
            : "for LOCAL EMPLOYMENT";

        yield return "    Having personally appeared before this office and been duly " +
                     "examined, and the records of this barangay having been checked, " +
                     $"the above-named resident, applying {scope}, is hereby cleared " +
                     "and found to be of good standing in this community.";

        yield return "";
        yield return "    The records of the Lupong Tagapamayapa of this barangay show no " +
                     "pending case involving the above-named resident as of the date " +
                     "of issuance hereof.";

        if (request.AvailedUnderJobseekerAct)
        {
            yield return "";
            yield return "    This clearance is issued FREE OF CHARGE under Republic Act " +
                         "No. 11261 (First Time Jobseekers Assistance Act), the benefits " +
                         "of which may be availed of only once.";
        }
    }
}
