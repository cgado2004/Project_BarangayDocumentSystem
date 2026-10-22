using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;

using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.BusinessRules;

/// <summary>
/// Assembles a printable document: letterhead, title, body, footer.
///
/// ── DRY: the single biggest win in this refactor ────────────────────────
/// The old DocumentPrinter repeated the letterhead, the "Issued this day…"
/// closing, the signature block, the reference/fee footer and the dry-seal
/// notice across SEVEN Append* methods. Changing the letterhead meant seven
/// edits, and the seven had already drifted apart in small ways.
///
/// Here that scaffolding is written ONCE. Templates supply only their body.
///
/// ── OPEN/CLOSED ─────────────────────────────────────────────────────────
/// This class has no switch and no knowledge of any specific document. Adding
/// a certificate never touches this file.
///
/// ── SINGLE RESPONSIBILITY ───────────────────────────────────────────────
/// One job: page layout. Wording lives in templates; fees live in FeeSchedule.
/// </summary>
public class DocumentRenderer
{
    private const int PageWidth = 72;

    private const string OathText =
        "I hereby declare under oath that I am a first time jobseeker, that the " +
        "foregoing statements are true and correct, and that I am availing of the " +
        "benefits under RA 11261 for the first time.";

    /// <summary>
    /// The columns of the one-row "Document" table the Crystal report is built on.
    /// Reports/DocumentData.xsd describes the same columns for the report designer,
    /// so the two must stay in step.
    /// </summary>
    private static readonly string[] ReportColumns =
    {
        "BarangayName", "CityName", "ProvinceName", "PunongBarangay",
        "Title", "Subtitle", "Body", "Oath",
        "ReferenceNo", "ResidentName", "Purpose",
        "IssuedLine", "DateIssued", "FeeText", "FeeBasis", "ORNo"
    };

    private readonly IReadOnlyDictionary<DocumentType, IDocumentTemplate> _templates;
    private readonly BarangayProfile _profile;

    /// <summary>
    /// Templates are injected, not constructed here (DIP). The renderer never
    /// names a concrete template class.
    /// </summary>
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
            // Defensive: a document type with no registered template. Better a
            // clear message than a silently blank certificate.
            sb.AppendLine(Centre($"[No template registered for {request.DocumentType}]"));
            return sb.ToString();
        }

        AppendTitle(sb, template);
        AppendBody(sb, template, request);

        // The RA 11261 certificate needs a signed undertaking; nothing else does.
        if (request.DocumentType == DocumentType.FirstTimeJobseekerCertificate)
            AppendOathOfUndertaking(sb);

        AppendFooter(sb, request);
        return sb.ToString();
    }

    /// <summary>
    /// The same document as <see cref="Render"/>, but as data for the Crystal
    /// report: one row, with the letterhead, title, body paragraphs and footer
    /// values in separate columns. The report supplies the page layout.
    /// </summary>
    public DataTable BuildReportData(DocumentRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var table = new DataTable("Document");
        foreach (string column in ReportColumns)
            table.Columns.Add(column, typeof(string));

        string title, subtitle, body;
        if (_templates.TryGetValue(request.DocumentType, out var template))
        {
            title = template.Title;
            subtitle = template.Subtitle ?? string.Empty;
            body = string.Join(Environment.NewLine + Environment.NewLine,
                               template.BuildBody(request, _profile));
        }
        else
        {
            title = request.GetDocumentName().ToUpper();
            subtitle = string.Empty;
            body = $"[No template registered for {request.DocumentType}]";
        }

        var now = DateTime.Now;
        var row = table.NewRow();
        row["BarangayName"]   = _profile.BarangayName;
        row["CityName"]       = _profile.CityName;
        row["ProvinceName"]   = _profile.ProvinceName;
        row["PunongBarangay"] = _profile.PunongBarangay;
        row["Title"]          = title;
        row["Subtitle"]       = subtitle;
        row["Body"]           = body;
        row["Oath"]           = request.DocumentType == DocumentType.FirstTimeJobseekerCertificate
                                    ? OathText : string.Empty;
        row["ReferenceNo"]    = request.GetReferenceNumber();
        row["ResidentName"]   = request.Resident.GetFullName().ToUpper();
        row["Purpose"]        = request.Purpose;
        row["IssuedLine"]     = $"Issued this {Ordinal(now.Day)} day of {now:MMMM, yyyy} at " +
                                $"{_profile.BarangayName}, {_profile.CityName}, Davao del Norte.";
        row["DateIssued"]     = now.ToString("MMMM d, yyyy  h:mm tt");
        row["FeeText"]        = request.Fee > 0 ? $"\u20b1{request.Fee:N2}" : "FREE OF CHARGE";
        row["FeeBasis"]       = request.Fee > 0 ? string.Empty : request.FeeBasis;
        row["ORNo"]           = request.Fee > 0 ? request.OfficialReceiptNo : string.Empty;
        table.Rows.Add(row);

        return table;
    }

    /// <summary>1 → "1st", 2 → "2nd", 11 → "11th", 22 → "22nd".</summary>
    private static string Ordinal(int day)
    {
        if (day % 100 >= 11 && day % 100 <= 13) return day + "th";
        switch (day % 10)
        {
            case 1: return day + "st";
            case 2: return day + "nd";
            case 3: return day + "rd";
            default: return day + "th";
        }
    }

    // -----------------------------------------------------------------
    //  Written once, used by every document
    // -----------------------------------------------------------------
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
            // Pre-formatted lines (the ID card's "Label : value" rows and the
            // business clearance details) contain a colon at a fixed column and
            // must not be re-wrapped, or the alignment is destroyed.
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
        sb.AppendLine(Wrap(OathText));
        sb.AppendLine();
        sb.AppendLine($"{"",40}________________________");
        sb.AppendLine($"{"",42}Signature of Applicant");
        sb.AppendLine();
    }

    private void AppendFooter(StringBuilder sb, DocumentRequest request)
    {
        sb.AppendLine(Wrap($"Issued this {DateTime.Now:d} day of {DateTime.Now:MMMM, yyyy} at " +
                           $"{_profile.BarangayName}, {_profile.CityName}, Davao del Norte."));
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine($"{"",38}{_profile.PunongBarangay}");
        sb.AppendLine($"{"",42}Punong Barangay");
        sb.AppendLine();
        sb.AppendLine(new string('-', PageWidth));
        sb.AppendLine($"Reference No. : {request.GetReferenceNumber()}");
        sb.AppendLine($"Date Issued   : {DateTime.Now:MMMM d, yyyy  h:mm tt}");

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

    // -----------------------------------------------------------------
    private static string Centre(string text)
    {
        if (text.Length >= PageWidth) return text;
        return new string(' ', (PageWidth - text.Length) / 2) + text;
    }

    private static string Wrap(string text)
    {
        // .NET Framework has no Split(char, StringSplitOptions) overload -- only
        // the char[] form (added as a convenience in .NET Core).
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
