using System.Text;
using BarangayDocumentSystem.Core.Data;
using BarangayDocumentSystem.Core.Entities;

namespace BarangayDocumentSystem.Core.Rules;

/// <summary>
/// I turn a request into the text of the actual certificate.
///
/// I wrote the letterhead, the closing block and the signature line once, and
/// only the middle paragraph changes per document type. That way a change to
/// the barangay's letterhead is one edit for me, not twenty.
/// </summary>
public class DocumentRenderer
{
    private readonly BarangayProfile _profile;

    public DocumentRenderer(BarangayProfile profile) =>
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));

    public string Render(DocumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sb = new StringBuilder();
        AppendLetterhead(sb, request);
        AppendBody(sb, request);
        AppendFooter(sb, request);
        return sb.ToString();
    }

    private void AppendLetterhead(StringBuilder sb, DocumentRequest r)
    {
        sb.AppendLine(Centre("Republic of the Philippines"));
        sb.AppendLine(Centre($"Province of {_profile.ProvinceName}"));
        sb.AppendLine(Centre(_profile.CityName));
        sb.AppendLine(Centre(_profile.BarangayName.ToUpperInvariant()));
        sb.AppendLine();
        sb.AppendLine(Centre("OFFICE OF THE PUNONG BARANGAY"));
        sb.AppendLine();
        sb.AppendLine(new string('=', 64));
        sb.AppendLine();
        sb.AppendLine(Centre(FeeSchedule.NameOf(r.DocumentType).ToUpperInvariant()));
        sb.AppendLine();
        sb.AppendLine();
    }

    private void AppendBody(StringBuilder sb, DocumentRequest r)
    {
        var p = r.Resident;
        string name = p.GetFullName().ToUpperInvariant();

        sb.AppendLine("TO WHOM IT MAY CONCERN:");
        sb.AppendLine();

        switch (r.DocumentType)
        {
            case DocumentType.CertificateOfIndigency:
                sb.AppendLine($"    This is to certify that {name}, {p.GetAge()} years of age,");
                sb.AppendLine($"is a bona fide resident of {p.Purok}, {_profile.BarangayName},");
                sb.AppendLine("and belongs to an INDIGENT family of this barangay.");
                break;

            case DocumentType.CertificateOfLowIncome:
                sb.AppendLine($"    This is to certify that {name}, {p.GetAge()} years of age,");
                sb.AppendLine($"is a bona fide resident of {p.Purok}, {_profile.BarangayName},");
                sb.AppendLine("and belongs to a LOW INCOME family of this barangay.");
                break;

            case DocumentType.BarangayBusinessClearance:
                sb.AppendLine($"    This is to certify that the business establishment owned and");
                sb.AppendLine($"operated by {name} of {p.Purok} has been granted clearance to");
                sb.AppendLine($"operate within the territorial jurisdiction of this barangay.");
                break;

            case DocumentType.FirstTimeJobseekerCertificate:
                sb.AppendLine($"    This is to certify that {name}, {p.GetAge()} years of age, a");
                sb.AppendLine($"resident of {p.Purok}, {_profile.BarangayName}, is a FIRST TIME");
                sb.AppendLine("JOBSEEKER and is qualified to avail of the benefits under");
                sb.AppendLine("Republic Act No. 11261, the First Time Jobseekers Assistance Act.");
                break;

            case DocumentType.SoloParentCertification:
                sb.AppendLine($"    This is to certify that {name} of {p.Purok},");
                sb.AppendLine($"{_profile.BarangayName}, is a SOLO PARENT as defined under");
                sb.AppendLine("Republic Act No. 11861, the Expanded Solo Parents Welfare Act.");
                break;

            case DocumentType.CertificateOfGoodMoralCharacter:
                sb.AppendLine($"    This is to certify that {name}, a bona fide resident of");
                sb.AppendLine($"{p.Purok}, {_profile.BarangayName}, is known to be of GOOD MORAL");
                sb.AppendLine("CHARACTER and has no derogatory record on file in this office.");
                break;

            case DocumentType.CertificateOfResidency:
                sb.AppendLine($"    This is to certify that {name}, {p.GetAge()} years of age,");
                sb.AppendLine($"is a bona fide RESIDENT of {p.Purok}, {_profile.BarangayName},");
                sb.AppendLine($"and has resided here since {DisplayFormat.LongDate(p.DateOfResidency)}.");
                break;

            default:
                sb.AppendLine($"    This is to certify that {name}, {p.GetAge()} years of age,");
                sb.AppendLine($"is a bona fide resident of {p.Purok}, {_profile.BarangayName},");
                sb.AppendLine("and is known to this office to be of good standing.");
                break;
        }

        sb.AppendLine();
        sb.AppendLine($"    This certification is issued upon the request of the above-named");
        sb.AppendLine($"person for {r.Purpose}.");
        sb.AppendLine();

        if (r.DocumentType == DocumentType.FirstTimeJobseekerCertificate)
            AppendOath(sb);
    }

    /// <summary>
    /// RA 11261 requires the jobseeker to sign an Oath of Undertaking, so I
    /// print it on the same page. Without it the certificate is not complete
    /// and the office issuing it has not met the law.
    /// </summary>
    private static void AppendOath(StringBuilder sb)
    {
        sb.AppendLine(new string('-', 64));
        sb.AppendLine(Centre("OATH OF UNDERTAKING"));
        sb.AppendLine();
        sb.AppendLine("    I solemnly swear that all the information above are true and");
        sb.AppendLine("correct to the best of my knowledge; that I am a first time jobseeker");
        sb.AppendLine("and that I am availing of this benefit for the first and only time.");
        sb.AppendLine();
        sb.AppendLine("    I further undertake to inform the barangay should I be employed.");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("                              _______________________________");
        sb.AppendLine("                                   Signature over printed name");
        sb.AppendLine();
        sb.AppendLine(new string('-', 64));
        sb.AppendLine();
    }

    private void AppendFooter(StringBuilder sb, DocumentRequest r)
    {
        sb.AppendLine($"    Issued this {DisplayFormat.LongDate(DateTime.Today)} at");
        sb.AppendLine($"{_profile.BarangayName}, {_profile.CityName}, {_profile.ProvinceName}.");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("                              _______________________________");
        sb.AppendLine($"                              {_profile.PunongBarangay}");
        sb.AppendLine("                                     Punong Barangay");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine(new string('=', 64));
        sb.AppendLine($"Reference   : {r.GetReferenceNumber()}");
        sb.AppendLine($"Fee         : {DisplayFormat.Peso(r.Fee)}");
        sb.AppendLine($"Basis       : {r.FeeBasis}");

        if (r.IsPaid)
            sb.AppendLine($"O.R. Number : {r.OfficialReceiptNo}");
        else if (r.Fee == 0)
            sb.AppendLine("O.R. Number : not required - issued free of charge");

        sb.AppendLine($"Status      : {r.Status}");
    }

    /// <summary>
    /// I centre a line within the 64-character page width.
    ///
    /// This only works because I show the preview in a monospaced font, where
    /// every character is the same width, so counting characters genuinely
    /// lines things up. In a proportional font it would come out ragged.
    /// </summary>
    private static string Centre(string text)
    {
        const int width = 64;
        if (text.Length >= width) return text;
        int pad = (width - text.Length) / 2;
        return new string(' ', pad) + text;
    }
}
