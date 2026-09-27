// =====================================================================
//  PART:    Document templates - indigency, low income and the RA 11261 jobseeker certificate
//  ORIGIN:  leader_draft - Clint Wood Gado
//  EDITS:   Clint Wood Gado - my v3.1 templates written against my fee model (Fdraft carried Frent's one-file-per-document templates for the core documents; these replace them)
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.BusinessRules.DocumentTemplates;

/// <summary>
/// The Certificate of Indigency and the Certificate of Low Income - one
/// template with two wordings, because the two documents differ by a
/// phrase and nothing else. Both are FREE under the charter; charging for
/// a paper whose whole point is to prove the holder cannot pay would
/// defeat the service.
/// </summary>
public sealed class IndigencyTemplate : DocumentTemplateBase
{
    private readonly bool _lowIncome;

    /// <param name="lowIncome">False for the Certificate of Indigency, true
    /// for the Certificate of Low Income.</param>
    public IndigencyTemplate(bool lowIncome)
        : base(lowIncome
            ? DocumentType.CertificateOfLowIncome
            : DocumentType.CertificateOfIndigency)
    {
        _lowIncome = lowIncome;
    }

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        yield return ResidentIntroduction(request, barangay);

        string standing = _lowIncome
            ? "belongs to a LOW INCOME family of this barangay"
            : "belongs to an INDIGENT family of this barangay";

        yield return "";
        yield return $"    As such, {request.Resident.GetFullName()} {standing}, as determined " +
                     "from the records of this office and the known circumstances of the " +
                     "household in the community.";

        yield return "";
        yield return "    This certification is issued FREE OF CHARGE pursuant to the " +
                     "Barangay Citizen's Charter, consistent with Republic Act No. 11291 " +
                     "(Magna Carta of the Poor).";
    }
}

/// <summary>
/// The First-Time Jobseeker Certificate, with the Oath of Undertaking RA
/// 11261 requires printed on the same page. Without the oath the office
/// issuing the certificate has not met the law.
/// </summary>
public sealed class JobseekerTemplate : DocumentTemplateBase
{
    public JobseekerTemplate() : base(DocumentType.FirstTimeJobseekerCertificate) { }

    public override bool RequiresOath => true;

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        var r = request.Resident;
        int months = r.GetMonthsOfResidency();

        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    As such, {r.GetFullName()} has been a resident of this barangay for " +
                     $"at least {months} month(s), and has personally executed before this " +
                     "office the Oath of Undertaking required under Republic Act No. 11261, " +
                     "the First Time Jobseekers Assistance Act.";

        yield return "";
        yield return "    This is to certify further that the above-named resident is a " +
                     "FIRST TIME JOBSEEKER, is actively seeking employment for the first " +
                     "time, and is qualified to avail of the benefits granted under the " +
                     "said Act. This certification and the barangay clearance issued in " +
                     "connection therewith are FREE OF CHARGE, and the benefits under the " +
                     "Act may be availed of only ONCE.";
    }

    public override IEnumerable<string> OathLines(DocumentRequest request)
    {
        yield return "OATH OF UNDERTAKING";
        yield return "";
        yield return "    I solemnly swear that all the information I have provided are true " +
                     "and correct to the best of my knowledge; that I am a Filipino citizen; " +
                     "that I am actively seeking employment for the first time; and that I " +
                     "am availing of the benefits under Republic Act No. 11261 for the " +
                     "first and only time.";
        yield return "";
        yield return "    I undertake to inform the issuing barangay once I become employed.";
        yield return "";
        yield return "    _______________________________";
        yield return "         Signature over printed name";
    }
}
