using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.BusinessRules.DocumentTemplates;

/// <summary>
/// The Employment Certification: what the resident does for a living, as
/// known to the barangay.
/// </summary>
public sealed class EmploymentTemplate : DocumentTemplateBase
{
    public EmploymentTemplate() : base(DocumentType.EmploymentCertification) { }

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        var r = request.Resident;

        string occupation = string.IsNullOrWhiteSpace(r.Occupation)
            ? "self-employed in the community"
            : r.Occupation.ToLowerInvariant();

        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    As known to this office, {r.GetFullName()} is engaged in gainful " +
                     $"occupation as {occupation}.";
    }
}

/// <summary>
/// The Acceptance Certificate - the barangay's record that something was
/// received or accepted, most often documents or property turned over to
/// the office or to a resident, with the purpose stating what.
/// </summary>
public sealed class AcceptanceTemplate : DocumentTemplateBase
{
    public AcceptanceTemplate() : base(DocumentType.AcceptanceCertificate) { }

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    This is to certify that the matters described in the stated " +
                     $"purpose of this request have been received and accepted by this " +
                     $"office, with {request.Resident.GetFullName()} as the requesting " +
                     "party of record.";
    }
}

/// <summary>
/// Certification of a blotter entry - the barangay's own record of an
/// incident as reported. It certifies THAT an entry exists; it does not
/// adjudge anything, and the certificate says so.
/// </summary>
public sealed class BlotterTemplate : DocumentTemplateBase
{
    public BlotterTemplate() : base(DocumentType.BlotterRelatedIncident) { }

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    This is to certify that {request.Resident.GetFullName()} has " +
                     "reported to this office the incident described in the stated purpose " +
                     "of this request, and that the same was entered in the barangay " +
                     "blotter kept by the Barangay Secretary.";

        yield return "";
        yield return "    This certification is a mere certification of the existence of a " +
                     "blotter entry. It does not determine the truth of the matters " +
                     "reported therein, nor the liability of any party.";

        yield return "";
        yield return "    Issued FREE OF CHARGE under the Barangay Citizen's Charter.";
    }
}
