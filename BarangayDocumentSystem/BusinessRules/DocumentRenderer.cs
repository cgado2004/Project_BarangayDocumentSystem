using System;
using System.Collections.Generic;
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

        if (!string.IsNullOrWhiteSpace(template.Subtitle))
            sb.AppendLine(Centre(template.Subtitle));

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
