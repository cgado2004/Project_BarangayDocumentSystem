using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;

using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.BusinessRules;



















public class DocumentRenderer
{
    private const int PageWidth = 72;

    private readonly IReadOnlyDictionary<DocumentType, IDocumentTemplate> _templates;
    private readonly BarangayProfile _profile;





    public DocumentRenderer(IEnumerable<IDocumentTemplate> templates, BarangayProfile profile)
    {
        if (templates is null) throw new ArgumentNullException(nameof(templates));

        _templates = templates.ToDictionary(t => t.DocumentType);
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public string Render(DocumentRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var sb = new StringBuilder();

        AppendLetterhead(sb);

        if (!_templates.TryGetValue(request.DocumentType, out var template))
        {


            sb.AppendLine(Centre($"[No template registered for {request.DocumentType}]"));
            return sb.ToString();
        }

        AppendTitle(sb, template);
        AppendBody(sb, template, request);


        if (request.DocumentType == DocumentType.FirstTimeJobseekerCertificate)
            AppendOathOfUndertaking(sb);

        AppendFooter(sb, request);
        return sb.ToString();
    }





    public static readonly string[] ReportColumns =
    {
        "BarangayName", "CityName", "ProvinceName", "PunongBarangay",
        "Title", "Subtitle", "Body", "Oath",
        "ReferenceNo", "ResidentName", "Purpose",
        "IssuedLine", "DateIssued", "FeeText", "FeeBasis", "ORNo"
    };







    public DataTable BuildReportData(DocumentRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var table = new DataTable("Document");
        foreach (string column in ReportColumns)
            table.Columns.Add(column, typeof(string));

        _templates.TryGetValue(request.DocumentType, out var template);
        string body = template is null
            ? string.Empty
            : string.Join("\r\n\r\n", template.BuildBody(request, _profile));

        string oath = request.DocumentType == DocumentType.FirstTimeJobseekerCertificate
            ? "I hereby declare under oath that I am a first time jobseeker, that the " +
              "foregoing statements are true and correct, and that I am availing of the " +
              "benefits under RA 11261 for the first time."
            : string.Empty;

        var issued = request.DateReleased ?? request.DateRequested;
        string feeText = request.Fee > 0 ? $"₱{request.Fee:N2}" : "FREE OF CHARGE";

        table.Rows.Add(
            _profile.BarangayName,
            _profile.CityName,
            _profile.ProvinceName,
            _profile.PunongBarangay,
            template?.Title ?? string.Empty,
            template?.Subtitle ?? string.Empty,
            body,
            oath,
            request.GetReferenceNumber(),
            request.Resident.GetFullName(),
            request.Purpose,
            $"Issued on {issued:MMMM d, yyyy} at {_profile.BarangayName}, {_profile.CityName}, Davao del Norte.",
            issued.ToString("MMMM d, yyyy h:mm tt"),
            feeText,
            request.FeeBasis,
            request.OfficialReceiptNo);

        return table;
    }




    private void AppendLetterhead(StringBuilder sb)
    {
        sb.AppendLine(Centre("Republic of the Philippines"));
        sb.AppendLine(Centre(_profile.ProvinceName));
        sb.AppendLine(Centre(_profile.CityName));
        sb.AppendLine(Centre(_profile.BarangayName));
        sb.AppendLine();
        sb.AppendLine(Centre("OFFICE OF THE PUNONG BARANGAY"));
        sb.AppendLine(new string('=', PageWidth));
        sb.AppendLine();
    }

    private static void AppendTitle(StringBuilder sb, IDocumentTemplate template)
    {
        sb.AppendLine(Centre(template.Title));

        var subtitle = template.Subtitle;
        if (!string.IsNullOrWhiteSpace(subtitle))
            sb.AppendLine(Centre(subtitle!));

        sb.AppendLine();
    }

    private void AppendBody(StringBuilder sb, IDocumentTemplate template, DocumentRequest request)
    {
        foreach (string paragraph in template.BuildBody(request, _profile))
        {



            sb.AppendLine(IsPreformatted(paragraph) ? paragraph : Wrap(paragraph));
            sb.AppendLine();
        }
    }

    private static bool IsPreformatted(string text) =>
        text.Contains("  : ") || text.Contains("   : ");

    private static void AppendOathOfUndertaking(StringBuilder sb)
    {
        sb.AppendLine(new string('-', PageWidth));
        sb.AppendLine(Centre("OATH OF UNDERTAKING"));
        sb.AppendLine(Wrap(
            "I hereby declare under oath that I am a first time jobseeker, that the " +
            "foregoing statements are true and correct, and that I am availing of the " +
            "benefits under RA 11261 for the first time."));
        sb.AppendLine();
        sb.AppendLine($"{"",40}________________________");
        sb.AppendLine($"{"",42}Signature of Applicant");
        sb.AppendLine();
    }

    private void AppendFooter(StringBuilder sb, DocumentRequest request)
    {
        var issued = request.DateReleased ?? request.DateRequested;
        sb.AppendLine(Wrap($"Issued on {issued:MMMM d, yyyy} at " +
                           $"{_profile.BarangayName}, {_profile.CityName}, Davao del Norte."));
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine($"{"",38}{_profile.PunongBarangay}");
        sb.AppendLine($"{"",42}Punong Barangay");
        sb.AppendLine();
        sb.AppendLine(new string('-', PageWidth));
        sb.AppendLine($"Reference No. : {request.GetReferenceNumber()}");
        sb.AppendLine($"Date Issued   : {issued:MMMM d, yyyy  h:mm tt}");

        if (request.Fee > 0)
        {
            sb.AppendLine($"Fee Paid      : ₱{request.Fee:N2}");
            sb.AppendLine($"O.R. Number   : {request.OfficialReceiptNo}");
        }
        else
        {
            sb.AppendLine("Fee           : FREE OF CHARGE");
            sb.AppendLine($"Basis         : {request.FeeBasis}");
        }

        sb.AppendLine(new string('=', PageWidth));
        sb.AppendLine(Centre("NOT VALID WITHOUT OFFICIAL DRY SEAL"));
    }


    private static string Centre(string text)
    {
        if (text.Length >= PageWidth) return text;
        return new string(' ', (PageWidth - text.Length) / 2) + text;
    }

    private static string Wrap(string text)
    {


        var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        var line = new StringBuilder();

        foreach (var word in words)
        {
            if (line.Length + word.Length + 1 > PageWidth)
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
