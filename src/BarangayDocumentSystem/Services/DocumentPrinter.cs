using System.Text;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services;

/// <summary>
/// Renders the printable text of a barangay document.
///
/// Kept separate from the fee logic and the UI so wording can be revised
/// without touching either. The layouts follow the conventional Philippine
/// barangay certificate format.
/// </summary>
public class DocumentPrinter
{
    private const int Width = 72;

    public string BarangayName { get; set; } = "BARANGAY MAGUGPO POBLACION";
    public string CityName { get; set; } = "CITY OF TAGUM";
    public string ProvinceName { get; set; } = "PROVINCE OF DAVAO DEL NORTE";
    public string PunongBarangay { get; set; } = "HON. [PUNONG BARANGAY NAME]";

    public string Print(DocumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sb = new StringBuilder();
        AppendLetterhead(sb);

        switch (request.DocumentType)
        {
            case DocumentType.BarangayClearance:
                AppendClearance(sb, request); break;
            case DocumentType.CertificateOfResidency:
                AppendResidency(sb, request); break;
            case DocumentType.CertificateOfIndigency:
                AppendIndigency(sb, request); break;
            case DocumentType.FirstTimeJobseekerCertificate:
                AppendJobseeker(sb, request); break;
            case DocumentType.BarangayBusinessClearance:
                AppendBusinessClearance(sb, request); break;
            case DocumentType.CertificateOfGoodMoralCharacter:
                AppendGoodMoral(sb, request); break;
            case DocumentType.BarangayID:
                AppendBarangayId(sb, request); break;
            default:
                AppendGeneric(sb, request); break;
        }

        AppendFooter(sb, request);
        return sb.ToString();
    }

    // -----------------------------------------------------------------
    private void AppendLetterhead(StringBuilder sb)
    {
        sb.AppendLine(Centre("Republic of the Philippines"));
        sb.AppendLine(Centre(ProvinceName));
        sb.AppendLine(Centre(CityName));
        sb.AppendLine(Centre(BarangayName));
        sb.AppendLine();
        sb.AppendLine(Centre("OFFICE OF THE PUNONG BARANGAY"));
        sb.AppendLine(new string('=', Width));
        sb.AppendLine();
    }

    private void AppendClearance(StringBuilder sb, DocumentRequest r)
    {
        var res = r.Resident;

        sb.AppendLine(Centre("BARANGAY CLEARANCE"));
        sb.AppendLine();
        sb.AppendLine("TO WHOM IT MAY CONCERN:");
        sb.AppendLine();
        sb.AppendLine(Wrap(
            $"This is to certify that {res.GetFullName().ToUpper()}, " +
            $"{res.GetAge()} years old, {res.CivilStatus.ToString().ToLower()}, " +
            $"Filipino citizen, and a bona fide resident of {res.Purok}, " +
            $"{BarangayName}, {CityName}, is known to be of good moral character " +
            $"and law-abiding citizen in the community."));
        sb.AppendLine();
        sb.AppendLine(Wrap(
            "This further certifies that the above-named person has no derogatory " +
            "record nor pending case filed before this Barangay."));
        sb.AppendLine();
        sb.AppendLine(Wrap($"This certification is issued upon the request of the " +
                           $"above-named person for {r.Purpose.ToUpper()}."));
        sb.AppendLine();
    }

    private void AppendResidency(StringBuilder sb, DocumentRequest r)
    {
        var res = r.Resident;

        sb.AppendLine(Centre("CERTIFICATE OF RESIDENCY"));
        sb.AppendLine();
        sb.AppendLine("TO WHOM IT MAY CONCERN:");
        sb.AppendLine();
        sb.AppendLine(Wrap(
            $"This is to certify that {res.GetFullName().ToUpper()}, " +
            $"{res.GetAge()} years old, is a bona fide resident of " +
            $"{res.AddressLine}, {res.Purok}, {BarangayName}, {CityName}, " +
            $"Davao del Norte."));
        sb.AppendLine();
        sb.AppendLine(Wrap(
            $"Records of this office show that the above-named person has been " +
            $"residing in this barangay since " +
            $"{res.DateOfResidency:MMMM d, yyyy} " +
            $"({res.GetMonthsOfResidency()} months)."));
        sb.AppendLine();
        sb.AppendLine(Wrap($"Issued this day upon the request of the above-named " +
                           $"person for {r.Purpose.ToUpper()}."));
        sb.AppendLine();
    }

    private void AppendIndigency(StringBuilder sb, DocumentRequest r)
    {
        var res = r.Resident;

        sb.AppendLine(Centre("CERTIFICATE OF INDIGENCY"));
        sb.AppendLine();
        sb.AppendLine("TO WHOM IT MAY CONCERN:");
        sb.AppendLine();
        sb.AppendLine(Wrap(
            $"This is to certify that {res.GetFullName().ToUpper()}, " +
            $"{res.GetAge()} years old, {res.CivilStatus.ToString().ToLower()}, " +
            $"and a bona fide resident of {res.Purok}, {BarangayName}, " +
            $"{CityName}, belongs to an INDIGENT FAMILY in this barangay."));
        sb.AppendLine();
        sb.AppendLine(Wrap(
            "The above-named person has no sufficient means of livelihood to " +
            "support the basic necessities of the family."));
        sb.AppendLine();
        sb.AppendLine(Wrap($"This certification is issued FREE OF CHARGE upon the " +
                           $"request of the above-named person for {r.Purpose.ToUpper()}."));
        sb.AppendLine();
    }

    /// <summary>
    /// RA 11261 certificate. The wording deliberately states the statute, the
    /// six-month residency finding, and first-time status, because receiving
    /// agencies (NBI, PSA, BIR) check for exactly those.
    /// </summary>
    private void AppendJobseeker(StringBuilder sb, DocumentRequest r)
    {
        var res = r.Resident;

        sb.AppendLine(Centre("BARANGAY CERTIFICATION"));
        sb.AppendLine(Centre("(First Time Jobseeker)"));
        sb.AppendLine();
        sb.AppendLine("TO WHOM IT MAY CONCERN:");
        sb.AppendLine();
        sb.AppendLine(Wrap(
            $"This is to certify that {res.GetFullName().ToUpper()}, " +
            $"{res.GetAge()} years old, Filipino citizen, is a bona fide resident " +
            $"of {res.Purok}, {BarangayName}, {CityName}, Davao del Norte, and has " +
            $"been residing herein since {res.DateOfResidency:MMMM d, yyyy}, " +
            $"which is more than six (6) months."));
        sb.AppendLine();
        sb.AppendLine(Wrap(
            "This further certifies that the above-named person is a FIRST TIME " +
            "JOBSEEKER and is actively seeking employment, and has not previously " +
            "availed of the benefits under Republic Act No. 11261."));
        sb.AppendLine();
        sb.AppendLine(Wrap(
            "This certification is issued FREE OF CHARGE pursuant to Republic Act " +
            "No. 11261, otherwise known as the FIRST TIME JOBSEEKERS ASSISTANCE " +
            "ACT, and is valid for one (1) year from the date of issuance."));
        sb.AppendLine();
        sb.AppendLine(new string('-', Width));
        sb.AppendLine(Centre("OATH OF UNDERTAKING"));
        sb.AppendLine(Wrap(
            "I hereby declare under oath that I am a first time jobseeker, that " +
            "the foregoing statements are true and correct, and that I am availing " +
            "of the benefits under RA 11261 for the first time."));
        sb.AppendLine();
        sb.AppendLine($"{"",40}________________________");
        sb.AppendLine($"{"",42}Signature of Applicant");
        sb.AppendLine();
    }

    private void AppendBusinessClearance(StringBuilder sb, DocumentRequest r)
    {
        var res = r.Resident;

        sb.AppendLine(Centre("BARANGAY BUSINESS CLEARANCE"));
        sb.AppendLine();
        sb.AppendLine("TO WHOM IT MAY CONCERN:");
        sb.AppendLine();
        sb.AppendLine(Wrap(
            $"This is to certify that the business establishment described below, " +
            $"owned and operated by {res.GetFullName().ToUpper()} of {res.Purok}, " +
            $"{BarangayName}, {CityName}, has been granted clearance to operate " +
            $"within the territorial jurisdiction of this Barangay."));
        sb.AppendLine();
        sb.AppendLine($"  Business / Purpose : {r.Purpose}");
        sb.AppendLine($"  Location           : {res.AddressLine}, {res.Purok}");
        sb.AppendLine();
        sb.AppendLine(Wrap(
            "This clearance is issued subject to compliance with existing barangay " +
            "ordinances and is valid until 31 December of the current year."));
        sb.AppendLine();
    }

    private void AppendGoodMoral(StringBuilder sb, DocumentRequest r)
    {
        var res = r.Resident;

        sb.AppendLine(Centre("CERTIFICATE OF GOOD MORAL CHARACTER"));
        sb.AppendLine();
        sb.AppendLine("TO WHOM IT MAY CONCERN:");
        sb.AppendLine();
        sb.AppendLine(Wrap(
            $"This is to certify that {res.GetFullName().ToUpper()}, " +
            $"{res.GetAge()} years old, a bona fide resident of {res.Purok}, " +
            $"{BarangayName}, {CityName}, is a person of GOOD MORAL CHARACTER and " +
            $"has not been involved in any anomalous or illegal activity within " +
            $"this barangay."));
        sb.AppendLine();
        sb.AppendLine(Wrap($"Issued upon request for {r.Purpose.ToUpper()}."));
        sb.AppendLine();
    }

    private void AppendBarangayId(StringBuilder sb, DocumentRequest r)
    {
        var res = r.Resident;

        sb.AppendLine(Centre("BARANGAY IDENTIFICATION CARD"));
        sb.AppendLine(Centre("(Application Record)"));
        sb.AppendLine();
        sb.AppendLine($"  Name            : {res.GetFullName().ToUpper()}");
        sb.AppendLine($"  Date of Birth   : {res.DateOfBirth:MMMM d, yyyy}");
        sb.AppendLine($"  Age             : {res.GetAge()}");
        sb.AppendLine($"  Gender          : {res.Gender}");
        sb.AppendLine($"  Civil Status    : {res.CivilStatus}");
        sb.AppendLine($"  Address         : {res.AddressLine}, {res.Purok}");
        sb.AppendLine($"  Contact No.     : {res.ContactNumber}");
        sb.AppendLine($"  Resident Since  : {res.DateOfResidency:MMMM d, yyyy}");
        sb.AppendLine($"  Classification  : {res.GetClassificationText()}");
        sb.AppendLine();
        sb.AppendLine(Wrap("In case of emergency, please notify the Barangay Hall."));
        sb.AppendLine();
    }

    private void AppendGeneric(StringBuilder sb, DocumentRequest r)
    {
        sb.AppendLine(Centre(r.GetDocumentName().ToUpper()));
        sb.AppendLine();
        sb.AppendLine(Wrap($"Issued to {r.Resident.GetFullName().ToUpper()} " +
                           $"for {r.Purpose}."));
        sb.AppendLine();
    }

    private void AppendFooter(StringBuilder sb, DocumentRequest r)
    {
        sb.AppendLine(Wrap($"Issued this {DateTime.Now:d'th' day 'of' MMMM, yyyy} " +
                           $"at {BarangayName}, {CityName}, Davao del Norte."));
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine($"{"",38}{PunongBarangay}");
        sb.AppendLine($"{"",42}Punong Barangay");
        sb.AppendLine();
        sb.AppendLine(new string('-', Width));
        sb.AppendLine($"Reference No. : {r.GetReferenceNumber()}");
        sb.AppendLine($"Date Issued   : {DateTime.Now:MMMM d, yyyy  h:mm tt}");

        if (r.Fee > 0)
        {
            sb.AppendLine($"Fee Paid      : ₱{r.Fee:N2}");
            sb.AppendLine($"O.R. Number   : {r.OfficialReceiptNo}");
        }
        else
        {
            sb.AppendLine($"Fee           : FREE OF CHARGE");
            sb.AppendLine($"Basis         : {r.FeeBasis}");
        }

        sb.AppendLine(new string('=', Width));
        sb.AppendLine(Centre("NOT VALID WITHOUT OFFICIAL DRY SEAL"));
    }

    // -----------------------------------------------------------------
    private static string Centre(string text)
    {
        if (text.Length >= Width) return text;
        return new string(' ', (Width - text.Length) / 2) + text;
    }

    /// <summary>Word-wraps a paragraph to the page width.</summary>
    private static string Wrap(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        var line = new StringBuilder();

        foreach (var word in words)
        {
            if (line.Length + word.Length + 1 > Width)
            {
                sb.AppendLine(line.ToString());
                line.Clear();
            }

            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }

        if (line.Length > 0) sb.Append(line);
        return sb.ToString();
    }
}
