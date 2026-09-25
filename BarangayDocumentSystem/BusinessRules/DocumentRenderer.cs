using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.BusinessRules.DocumentTemplates;

namespace BarangayDocumentSystem.BusinessRules;

/// <summary>
/// The GDI+ layout engine for every document the barangay issues.
///
/// v3.1 separates WHAT a document says (the IDocumentTemplate classes)
/// from HOW it is laid out (this class). The engine draws the letterhead,
/// wraps the body with real font metrics, spaces the signature block and
/// prints the reference block; the template supplies only the words. A new
/// document type is one small class, and a change to the letterhead is one
/// edit for every document at once.
///
/// Two renderings come out of the same templates:
///   • Draw()            - the printed page, through GDI+ (also used by
///                         the scaled print preview in DocumentPreviewForm)
///   • RenderText()      - the plain-text form the rule checks run on,
///                         because a test harness has no use for pixels
/// </summary>
public sealed class DocumentRenderer
{
    private readonly BarangayProfile _profile;
    private readonly FeeSchedule _fees;
    private readonly Dictionary<DocumentType, IDocumentTemplate> _templates;
    private readonly GenericCertificationTemplate _fallback = new(DocumentType.OtherCertification);

    public DocumentRenderer(BarangayProfile profile, FeeSchedule? fees = null)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _fees = fees ?? new FeeSchedule();
        _templates = BuildRegistry(_fees);
    }

    /// <summary>
    /// One template per document type. The generic template catches
    /// anything unlisted, so no document type can render as a blank page.
    /// </summary>
    private static Dictionary<DocumentType, IDocumentTemplate> BuildRegistry(FeeSchedule fees) =>
        new()
        {
            [DocumentType.BarangayClearance]             = new BarangayClearanceTemplate(),
            [DocumentType.CertificateOfResidency]        = new ResidencyTemplate(),
            [DocumentType.CertificateOfIndigency]        = new IndigencyTemplate(lowIncome: false),
            [DocumentType.CertificateOfLowIncome]        = new IndigencyTemplate(lowIncome: true),
            [DocumentType.BarangayBusinessClearance]     = new BusinessClearanceTemplate(),
            [DocumentType.BarangayID]                    = new BarangayIdTemplate(),
            [DocumentType.FirstTimeJobseekerCertificate] = new JobseekerTemplate(),
            [DocumentType.CertificateOfGoodMoralCharacter] = new GoodMoralTemplate(),
            [DocumentType.SoloParentCertification]       = new SoloParentTemplate(),
            [DocumentType.MedicalAssistanceCertification]  = AssistanceTemplate.Medical(),
            [DocumentType.FinancialAssistanceCertification]= AssistanceTemplate.Financial(),
            [DocumentType.BurialAssistanceCertification]   = AssistanceTemplate.Burial(),
            [DocumentType.IpScholarshipCertification]      = ScholarshipTemplate.IndigenousPeople(),
            [DocumentType.FourPsScholarshipCertification]  = ScholarshipTemplate.FourPs(),
            [DocumentType.EmploymentCertification]       = new EmploymentTemplate(),
            [DocumentType.AcceptanceCertificate]         = new AcceptanceTemplate(),
            [DocumentType.BlotterRelatedIncident]        = new BlotterTemplate(),
            [DocumentType.CommunityTaxCertificate]       = new CommunityTaxTemplate(fees),
            [DocumentType.LuponCaseFiling]               = new CaseFilingTemplate(),
            [DocumentType.BarangayFacilityRental]        = new FacilityRentalTemplate(fees),
            [DocumentType.OtherTarifaProcessingFee]      = new TarifaFeeTemplate(),
        };

    /// <summary>The template for a document type. Never null: an unlisted
    /// type gets the generic certification wording.</summary>
    public IDocumentTemplate TemplateFor(DocumentType type) =>
        _templates.TryGetValue(type, out var template) ? template : _fallback;

    // =================================================================
    //  The printed page
    // =================================================================

    /// <summary>
    /// I lay one document out inside the given bounds, with GDI+.
    ///
    /// Everything is measured before it is drawn and the cursor only moves
    /// forward, so the layout holds at any DPI - which matters, because
    /// DocumentPreviewForm rescales this when it lands on a monitor with a
    /// different DPI and the paper must not jump about.
    /// </summary>
    public void Draw(Graphics g, Rectangle bounds, DocumentRequest request)
    {
        if (g is null) throw new ArgumentNullException(nameof(g));
        if (request is null) throw new ArgumentNullException(nameof(request));

        var template = TemplateFor(request.DocumentType);

        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        using var letterhead = new Font(SerifFamily, 10.5f, FontStyle.Regular, GraphicsUnit.Point);
        using var letterheadBold = new Font(SerifFamily, 11f, FontStyle.Bold, GraphicsUnit.Point);
        using var titleFont = new Font(SerifFamily, 14f, FontStyle.Bold, GraphicsUnit.Point);
        using var subtitleFont = new Font(SerifFamily, 10.5f, FontStyle.Italic, GraphicsUnit.Point);
        using var bodyFont = new Font(SerifFamily, 10.5f, FontStyle.Regular, GraphicsUnit.Point);
        using var smallFont = new Font(SerifFamily, 8.5f, FontStyle.Regular, GraphicsUnit.Point);

        float left = bounds.Left + 8f;
        float width = bounds.Width - 16f;
        float centre = left + width / 2f;
        float y = bounds.Top + 6f;

        // ---- the letterhead ----
        y = Centre(g, $"Republic of the Philippines", letterhead, centre, y);
        y = Centre(g, $"Province of {_profile.ProvinceName}", letterhead, centre, y);
        y = Centre(g, _profile.CityName, letterhead, centre, y);
        y = Centre(g, _profile.BarangayName.ToUpperInvariant(), letterheadBold, centre, y);
        y = Centre(g, "OFFICE OF THE PUNONG BARANGAY", letterhead, centre, y + 4f);

        // ---- the rule under the letterhead ----
        y += 8f;
        using (var pen = new Pen(Color.Black, 1.4f))
            g.DrawLine(pen, left, y, left + width, y);
        y += 14f;

        // ---- the title ----
        y = Centre(g, template.Title.ToUpperInvariant(), titleFont, centre, y);
        string? subtitle = template.SubtitleFor(request);
        if (!string.IsNullOrEmpty(subtitle))
            y = Centre(g, subtitle, subtitleFont, centre, y);
        y += 10f;

        // ---- the body ----
        foreach (string logical in template.BodyLines(request, _profile))
        {
            if (logical.Length == 0) { y += bodyFont.GetHeight(g) * 0.6f; continue; }

            foreach (string physical in Wrap(g, logical, bodyFont, width))
                y = Left(g, physical, bodyFont, left, y);
        }

        // ---- the oath, when the document carries one ----
        if (template.RequiresOath)
        {
            y += 10f;
            using (var pen = new Pen(Color.Black, 0.8f))
                g.DrawLine(pen, left, y, left + width, y);
            y += 8f;

            var oath = template.OathLines(request).ToList();
            if (oath.Count > 0)
            {
                y = Centre(g, oath[0].ToUpperInvariant(), letterheadBold, centre, y);
                foreach (string logical in oath.Skip(1))
                {
                    if (logical.Length == 0) { y += bodyFont.GetHeight(g) * 0.5f; continue; }
                    foreach (string physical in Wrap(g, logical, bodyFont, width))
                        y = Left(g, physical, bodyFont, left, y);
                }
            }
        }

        // ---- the signature block ----
        y += bodyFont.GetHeight(g) * 2.6f;
        float sigLeft = centre + width * 0.08f;
        float sigWidth = width * 0.42f;

        using (var pen = new Pen(Color.Black, 1f))
            g.DrawLine(pen, sigLeft, y, sigLeft + sigWidth, y);
        y += 4f;
        y = Centre(g, _profile.PunongBarangay, letterheadBold, sigLeft + sigWidth / 2f, y);
        y = Centre(g, "Punong Barangay", letterhead, sigLeft + sigWidth / 2f, y);

        // ---- the reference block, pinned above the bottom edge ----
        float blockHeight = 5.2f * smallFont.GetHeight(g);
        float blockTop = Math.Min(Math.Max(y + 18f, bounds.Bottom - blockHeight),
                                  bounds.Bottom - blockHeight);

        using (var pen = new Pen(Color.Black, 0.8f))
            g.DrawLine(pen, left, blockTop, left + width, blockTop);

        float by = blockTop + 6f;
        by = Left(g, $"Issued this {DisplayFormat.LongDate(DateTime.Today)} at " +
                     $"{_profile.BarangayName}, {_profile.CityName}, {_profile.ProvinceName}.",
                  smallFont, left, by);
        by = Left(g, $"Reference : {request.GetReferenceNumber()}", smallFont, left, by);
        by = Left(g, $"Fee       : {DisplayFormat.PesoOrFree(request.Fee)} — {request.FeeBasis}",
                  smallFont, left, by);

        string orLine = request.IsPaid
            ? $"O.R. No.  : {request.OfficialReceiptNo}"
            : request.Fee == 0m
                ? "O.R. No.  : not required — issued free of charge"
                : "O.R. No.  : (unpaid)";
        by = Left(g, orLine, smallFont, left, by);
        Left(g, $"Status    : {request.Status}", smallFont, left, by);
    }

    /// <summary>Creates the print job for one document - Letter size, sane
    /// margins. The preview form and the Print button share it, so what you
    /// see is exactly what prints.</summary>
    public PrintDocument CreatePrintDocument(DocumentRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var document = new PrintDocument
        {
            DocumentName = request.GetReferenceNumber()
        };

        document.DefaultPageSettings.Margins = new Margins(75, 60, 60, 60);
        document.PrintPage += (_, e) =>
        {
            Draw(e.Graphics, e.MarginBounds, request);
            e.HasMorePages = false;
        };

        return document;
    }

    // =================================================================
    //  The plain-text rendering
    // =================================================================

    /// <summary>
    /// The document as plain text, 64 characters wide.
    ///
    /// The rule checks run on this, because compiling only proves my code
    /// is grammatical - it says nothing about whether the certificate says
    /// the right thing. The monospaced layout also makes a decent fallback
    /// for a quick look in Notepad.
    /// </summary>
    public string RenderText(DocumentRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var template = TemplateFor(request.DocumentType);
        var sb = new StringBuilder();

        sb.AppendLine(CentreText("Republic of the Philippines"));
        sb.AppendLine(CentreText($"Province of {_profile.ProvinceName}"));
        sb.AppendLine(CentreText(_profile.CityName));
        sb.AppendLine(CentreText(_profile.BarangayName.ToUpperInvariant()));
        sb.AppendLine();
        sb.AppendLine(CentreText("OFFICE OF THE PUNONG BARANGAY"));
        sb.AppendLine();
        sb.AppendLine(new string('=', 64));
        sb.AppendLine();
        sb.AppendLine(CentreText(template.Title.ToUpperInvariant()));

        string? subtitle = template.SubtitleFor(request);
        if (!string.IsNullOrEmpty(subtitle))
            sb.AppendLine(CentreText(subtitle));

        sb.AppendLine();

        foreach (string logical in template.BodyLines(request, _profile))
        {
            if (logical.Length == 0) { sb.AppendLine(); continue; }
            foreach (string physical in WrapText(logical, 64))
                sb.AppendLine(physical.TrimEnd());
        }

        if (template.RequiresOath)
        {
            sb.AppendLine();
            sb.AppendLine(new string('-', 64));
            var oath = template.OathLines(request).ToList();
            if (oath.Count > 0)
            {
                sb.AppendLine(CentreText(oath[0].ToUpperInvariant()));
                sb.AppendLine();
                foreach (string logical in oath.Skip(1))
                {
                    if (logical.Length == 0) { sb.AppendLine(); continue; }
                    foreach (string physical in WrapText(logical, 64))
                        sb.AppendLine(physical.TrimEnd());
                }
            }
            sb.AppendLine(new string('-', 64));
        }

        sb.AppendLine();
        sb.AppendLine($"    Issued this {DisplayFormat.LongDate(DateTime.Today)} at");
        sb.AppendLine($"    {_profile.BarangayName}, {_profile.CityName}, {_profile.ProvinceName}.");
        sb.AppendLine();
        sb.AppendLine("                              _______________________________");
        sb.AppendLine($"                              {_profile.PunongBarangay}");
        sb.AppendLine("                                     Punong Barangay");
        sb.AppendLine();
        sb.AppendLine(new string('=', 64));
        sb.AppendLine($"Reference   : {request.GetReferenceNumber()}");
        sb.AppendLine($"Fee         : {DisplayFormat.PesoOrFree(request.Fee)}");
        sb.AppendLine($"Basis       : {request.FeeBasis}");

        if (request.IsPaid)
            sb.AppendLine($"O.R. Number : {request.OfficialReceiptNo}");
        else if (request.Fee == 0m)
            sb.AppendLine("O.R. Number : not required - issued free of charge");

        sb.AppendLine($"Status      : {request.Status}");
        return sb.ToString();
    }

    // =================================================================
    //  Text plumbing
    // =================================================================

    private static string _serifFamily = string.Empty;

    /// <summary>
    /// The serif family the certificates are set in. Official Philippine
    /// documents read better in a serif; I fall back through the fonts
    /// every Windows machine has rather than trusting one to exist. This
    /// resolver is deliberately independent of AppTheme - the rendering
    /// engine picks its own typefaces and never reaches into the UI theme.
    /// </summary>
    private static string SerifFamily
    {
        get
        {
            if (_serifFamily.Length > 0) return _serifFamily;

            _serifFamily = "Segoe UI";
            try
            {
                using var installed = new System.Drawing.Text.InstalledFontCollection();
                var names = new HashSet<string>(
                    installed.Families.Select(f => f.Name),
                    StringComparer.OrdinalIgnoreCase);

                foreach (string candidate in new[] { "Georgia", "Times New Roman", "Cambria" })
                    if (names.Contains(candidate)) { _serifFamily = candidate; break; }
            }
            catch
            {
                // If I cannot read the font list for any reason, the fallback
                // family above stands. A slightly plainer certificate beats
                // a crash at print time.
            }

            return _serifFamily;
        }
    }

    /// <summary>Draws one centred line and returns the y just below it.</summary>
    private static float Centre(Graphics g, string text, Font font, float cx, float y)
    {
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, Brushes.Black, new PointF(cx - size.Width / 2f, y));
        return y + size.Height;
    }

    /// <summary>Draws one left-aligned line and returns the y just below it.</summary>
    private static float Left(Graphics g, string text, Font font, float x, float y)
    {
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, Brushes.Black, new PointF(x, y));
        return y + size.Height;
    }

    /// <summary>
    /// I wrap a logical line to a pixel width myself, word by word, rather
    /// than letting DrawString do it. Two reasons: I keep the leading
    /// indent a template puts at the start of a paragraph, and the same
    /// wrapping rules run in the plain-text rendering, so the two outputs
    /// break at the same places.
    /// </summary>
    private static List<string> Wrap(Graphics g, string text, Font font, float maxWidth)
    {
        var lines = new List<string>();
        string indent = new(' ', text.Length - text.TrimStart(' ').Length);
        string[] words = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return lines;

        string current = indent;
        foreach (string word in words)
        {
            string candidate = current.Length == 0 ? word
                             : current + " " + word;
            if (current.Length > 0 && g.MeasureString(candidate, font).Width > maxWidth
                && candidate.TrimStart().Length > word.Length)
            {
                lines.Add(current.TrimEnd());
                current = new string(' ', indent.Length) + word;
            }
            else
            {
                current = candidate;
            }
        }

        if (current.Trim().Length > 0) lines.Add(current.TrimEnd());
        return lines;
    }

    /// <summary>Wraps a logical line to a character width, for the
    /// plain-text rendering.</summary>
    private static IEnumerable<string> WrapText(string text, int width)
    {
        string indent = new(' ', text.Length - text.TrimStart(' ').Length);
        string[] words = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) { yield break; }

        string current = indent;
        foreach (string word in words)
        {
            string candidate = current.Length == 0 ? word : current + " " + word;
            if (current.Length > 0 && candidate.Length > width
                && candidate.TrimStart().Length > word.Length)
            {
                yield return current.TrimEnd();
                current = new string(' ', indent.Length) + word;
            }
            else
            {
                current = candidate;
            }
        }

        if (current.Trim().Length > 0) yield return current.TrimEnd();
    }

    private static string CentreText(string text)
    {
        const int width = 64;
        if (text.Length >= width) return text;
        return new string(' ', (width - text.Length) / 2) + text;
    }
}
