using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.BusinessRules.DocumentTemplates;

/// <summary>
/// The Certificate of Good Moral Character.
/// </summary>
public sealed class GoodMoralTemplate : DocumentTemplateBase
{
    public GoodMoralTemplate() : base(DocumentType.CertificateOfGoodMoralCharacter) { }

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    As such, {request.Resident.GetFullName()} is known to this office " +
                     "and to the community to be of GOOD MORAL CHARACTER, law-abiding, and " +
                     "has no derogatory record on file in this office.";
    }
}

/// <summary>
/// The Solo Parent Certification. The controlling law is Republic Act
/// No. 11861, the Expanded Solo Parents Welfare Act; the barangay certifies
/// the circumstances it knows first-hand - residency and standing in the
/// community - for the solo parent's application to the City Social Welfare
/// and Development Office.
/// </summary>
public sealed class SoloParentTemplate : DocumentTemplateBase
{
    public SoloParentTemplate() : base(DocumentType.SoloParentCertification) { }

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    This is to certify further that {request.Resident.GetFullName()} " +
                     "has declared under oath to this office that the said resident is " +
                     "solo parenting a child or children, as contemplated under Republic " +
                     "Act No. 11861 (Expanded Solo Parents Welfare Act), and that this " +
                     "certification is issued for the declared purpose without prejudice " +
                     "to the evaluation of the appropriate social welfare office.";

        yield return "";
        yield return "    This certification is issued FREE OF CHARGE as social-service " +
                     "documentation under the Barangay Citizen's Charter.";
    }
}

/// <summary>
/// The assistance and social-service certifications: medical, financial and
/// burial assistance. One template, because the wording differs only in the
/// kind of assistance named - the resident is certified so a hospital, an
/// agency or a foundation can act on the request.
/// </summary>
public sealed class AssistanceTemplate : DocumentTemplateBase
{
    private readonly string _assistance;

    /// <param name="assistance">The kind of assistance, for example
    /// "MEDICAL". Used in the document as "for MEDICAL assistance".</param>
    private AssistanceTemplate(DocumentType type, string assistance) : base(type)
    {
        _assistance = assistance;
    }

    public static AssistanceTemplate Medical() =>
        new(DocumentType.MedicalAssistanceCertification, "MEDICAL");

    public static AssistanceTemplate Financial() =>
        new(DocumentType.FinancialAssistanceCertification, "FINANCIAL");

    public static AssistanceTemplate Burial() =>
        new(DocumentType.BurialAssistanceCertification, "BURIAL");

    protected override string Noun => "certification";

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    This is to certify that {request.Resident.GetFullName()} has " +
                     $"personally appeared before this office requesting {_assistance} " +
                     "assistance, and that this certification is issued to support the " +
                     "said request with the office or agency concerned.";

        yield return "";
        yield return "    This certification is issued FREE OF CHARGE as social-service " +
                     "documentation under the Barangay Citizen's Charter.";
    }
}

/// <summary>
/// The scholarship certifications for IP and 4Ps beneficiaries.
/// </summary>
public sealed class ScholarshipTemplate : DocumentTemplateBase
{
    private readonly string _programme;

    private ScholarshipTemplate(DocumentType type, string programme) : base(type)
    {
        _programme = programme;
    }

    public static ScholarshipTemplate IndigenousPeople() =>
        new(DocumentType.IpScholarshipCertification,
            "indigenous peoples, in recognition of the rights of ICCs/IPs under " +
            "Republic Act No. 8371 (IPRA)");

    public static ScholarshipTemplate FourPs() =>
        new(DocumentType.FourPsScholarshipCertification,
            "households covered by the Pantawid Pamilyang Pilipino Program (4Ps) under " +
            "Republic Act No. 11310");

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    This is to certify that {request.Resident.GetFullName()} belongs " +
                     $"to {_programme}, and that this certification is issued to support " +
                     "the application of the above-named resident for the scholarship or " +
                     "educational benefit concerned.";

        yield return "";
        yield return "    This certification is issued FREE OF CHARGE as social-service " +
                     "documentation under the Barangay Citizen's Charter.";
    }
}
