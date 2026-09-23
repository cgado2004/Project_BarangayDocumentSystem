using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Service.Templates;

/// <summary>
/// The Barangay Business Clearance.
///
/// The charter posts this fee as "amount varies depending on the law
/// violated", so when the clerk assessed the clearance against a violated
/// ordinance, that detail is printed on the face of the document. A
/// clearance nobody can explain is a clearance an auditor will question.
/// </summary>
public sealed class BusinessClearanceTemplate : DocumentTemplateBase
{
    public BusinessClearanceTemplate() : base(DocumentType.BarangayBusinessClearance) { }

    protected override string Noun => "clearance";

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        var r = request.Resident;

        yield return $"    This is to certify that the business establishment owned and " +
                     $"operated by {FormalName(r)} of {r.Purok}, {barangay.BarangayName}, " +
                     $"{barangay.CityName}, has been granted this clearance to operate " +
                     "within the territorial jurisdiction of this barangay.";

        yield return "";
        yield return "    This clearance does not in any way exempt the establishment from " +
                     "securing the necessary permits and licenses from the City Government " +
                     "of Tagum, nor from compliance with national and local laws.";

        string detail = request.Input.Detail.Trim();
        if (detail.Length > 0)
        {
            yield return "";
            yield return $"    This clearance is assessed in connection with: {detail}. " +
                         "Personal exemptions do not apply to this fee.";
        }
    }
}

/// <summary>
/// The Certificate of Residency: who lives here, and since when.
/// </summary>
public sealed class ResidencyTemplate : DocumentTemplateBase
{
    public ResidencyTemplate() : base(DocumentType.CertificateOfResidency) { }

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        var r = request.Resident;

        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    As such, {r.GetFullName()} has been a continuous resident of this " +
                     $"barangay since {DisplayFormat.LongDate(r.DateOfResidency)}, and remains " +
                     "a resident thereof as of the date of issuance hereof.";
    }
}

/// <summary>
/// The Barangay ID: a certification of identity and residency for
/// identification purposes.
/// </summary>
public sealed class BarangayIdTemplate : DocumentTemplateBase
{
    public BarangayIdTemplate() : base(DocumentType.BarangayID) { }

    protected override string Noun => "identification";

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        var r = request.Resident;

        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    This is to certify further that the above-named resident, " +
                     $"{r.CivilStatus.ToString().ToUpperInvariant()}, {r.Gender}, born on " +
                     $"{DisplayFormat.LongDate(r.DateOfBirth)}, is listed in the records of " +
                     "this barangay and is entitled to this identification for all lawful " +
                     "purposes within the barangay.";
    }
}
