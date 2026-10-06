// ---------------------------------------------------------------------------
//  DocumentRenderer.cs - how a paper looks, once, for every document.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services.Documents
{
    /// <summary>
    /// Draws the page: the letterhead, the title, the body the template wrote,
    /// the signature block and the footer.
    ///
    /// I wrote this once instead of once per document. The templates only
    /// supply sentences; this class owns the margins, the fonts, the seal at
    /// the top and the footer with the reference number, so all twenty-four
    /// papers come out looking like they came from the same office - which is
    /// how a barangay actually wants its papers to look.
    ///
    /// Where does the seal come from? The same file the login screen and the
    /// sidebar use. The barangay asked me not to touch the logo, so I do not:
    /// I load it and draw it, and there is exactly one copy of it in the
    /// project (Assets\barangay-logo.png).
    /// </summary>
    public class DocumentRenderer
    {
        private readonly DocumentTemplateRegistry _registry;

        private const float Margin = 72f;          // one inch, in points
        private const float LogoWidth = 78f;

        public DocumentRenderer() : this(new DocumentTemplateRegistry())
        {
        }

        public DocumentRenderer(DocumentTemplateRegistry registry)
        {
            _registry = registry == null ? new DocumentTemplateRegistry() : registry;
        }

        public DocumentTemplateRegistry Templates { get { return _registry; } }

        // ==================================================================
        //  Building the document
        // ==================================================================

        /// <summary>
        /// A configured PrintDocument, ready for the print preview or for the
        /// printer. The form does not draw anything itself - it calls this and
        /// hands the result to the preview control or to Print().
        /// </summary>
        public PrintDocument BuildDocument(DocumentContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            IDocumentTemplate template = _registry.Find(context.Request.DocumentType);
            if (template == null)
                throw new InvalidOperationException(
                    "There is no wording for " + context.Request.GetDocumentName()
                    + " yet. Add a template class and register it in DocumentTemplateRegistry.");

            PrintDocument document = new PrintDocument();
            document.DocumentName = context.Request.DocumentType + " - " + context.Request.ReferenceNumber;

            // A4 is what the barangay hall actually loads. If the printer only
            // has letter paper, the framework scales it - I would rather the
            // clerk not have to think about paper sizes.
            document.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            document.DefaultPageSettings.Margins = new Margins(72, 72, 72, 72);

            document.PrintPage += delegate (object sender, PrintPageEventArgs e)
            {
                DrawPage(e.Graphics, context, template, e.MarginBounds);

                // Every document this system prints fits on one page. The
                // long ones (the business clearance, the jobseeker oath) are
                // still short enough; if a future template outgrows a page,
                // this is the line that has to change to a paging loop.
                e.HasMorePages = false;
            };

            return document;
        }

        /// <summary>A short description of what will be printed, for the
        /// preview window's title bar.</summary>
        public string Describe(DocumentContext context)
        {
            if (context == null || context.Request == null) return "Document";

            return context.Request.GetDocumentName() + " - " + context.Request.ReferenceNumber
                 + " - " + (context.Resident == null ? string.Empty : context.Resident.GetFullName());
        }

        // ==================================================================
        //  Drawing
        // ==================================================================

        private void DrawPage(Graphics graphics, DocumentContext context, IDocumentTemplate template, Rectangle bounds)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            float y = bounds.Top;

            y = DrawLetterhead(graphics, context, bounds, y);
            y = DrawTitle(graphics, template.GetTitle(context), bounds, y);

            using (Font bodyFont = new Font(AppThemeFontFamily(), 10.5f, FontStyle.Regular))
            using (Font boldFont = new Font(AppThemeFontFamily(), 10.5f, FontStyle.Bold))
            using (Font smallFont = new Font(AppThemeFontFamily(), 8.5f, FontStyle.Regular))
            {
                float lineHeight = bodyFont.GetHeight(graphics) + 2.4f;

                foreach (string line in template.BuildBody(context))
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        y += lineHeight * 0.55f;
                        continue;
                    }

                    // The long statements (the oath of undertaking, the
                    // conditions on a business clearance) are wrapped rather
                    // than drawn off the edge of the paper.
                    y = DrawWrapped(graphics, line, bodyFont, boldFont, bounds, y, lineHeight);
                    if (y > bounds.Bottom - 150f) break;
                }

                y = DrawSignature(graphics, context, template, bounds, y, bodyFont, boldFont, smallFont);
                DrawFooter(graphics, context, bounds, smallFont);
            }
        }

        private float DrawLetterhead(Graphics graphics, DocumentContext context, Rectangle bounds, float y)
        {
            BarangayProfile profile = context.Profile ?? BarangayProfile.MagugpoPoblacion;

            float centerX = bounds.Left + (bounds.Width / 2f);

            // ---- the seal ----
            Image logo = LoadLogo();
            if (logo != null)
            {
                float scale = LogoWidth / logo.Width;
                float height = logo.Height * scale;

                graphics.DrawImage(logo, centerX - (LogoWidth / 2f), y, LogoWidth, height);
                y += height + 6f;
            }

            using (Font republicFont = new Font(AppThemeFontFamily(), 8.5f, FontStyle.Regular))
            using (Font nameFont = new Font(AppThemeFontFamily(), 15f, FontStyle.Bold))
            using (Font placeFont = new Font(AppThemeFontFamily(), 10f, FontStyle.Regular))
            using (StringFormat centred = new StringFormat())
            {
                centred.Alignment = StringAlignment.Center;

                using (Brush ink = new SolidBrush(Color.FromArgb(20, 20, 20)))
                using (Brush primary = new SolidBrush(Color.FromArgb(0x1B, 0x3B, 0x8B)))
                {
                    graphics.DrawString("Republic of the Philippines", republicFont, ink,
                        new RectangleF(bounds.Left, y, bounds.Width, 16f), centred);
                    y += 15f;

                    graphics.DrawString(profile.BarangayName.ToUpperInvariant(), nameFont, primary,
                        new RectangleF(bounds.Left, y, bounds.Width, 26f), centred);
                    y += 24f;

                    graphics.DrawString(profile.CityName + " | " + profile.ProvinceName, placeFont, ink,
                        new RectangleF(bounds.Left, y, bounds.Width, 18f), centred);
                    y += 17f;

                    graphics.DrawString("OFFICE OF THE PUNONG BARANGAY", republicFont, ink,
                        new RectangleF(bounds.Left, y, bounds.Width, 16f), centred);
                    y += 18f;
                }
            }

            using (Pen rule = new Pen(Color.FromArgb(0x1B, 0x3B, 0x8B), 1.4f))
                graphics.DrawLine(rule, bounds.Left, y, bounds.Right, y);

            using (Pen thin = new Pen(Color.FromArgb(0xF2, 0xB1, 0x1B), 0.8f))
                graphics.DrawLine(thin, bounds.Left, y + 2.2f, bounds.Right, y + 2.2f);

            return y + 22f;
        }

        private float DrawTitle(Graphics graphics, string title, Rectangle bounds, float y)
        {
            using (Font titleFont = new Font(AppThemeFontFamily(), 12.5f, FontStyle.Bold))
            using (StringFormat centred = new StringFormat())
            using (Brush ink = new SolidBrush(Color.FromArgb(15, 15, 15)))
            {
                centred.Alignment = StringAlignment.Center;

                graphics.DrawString(title, titleFont, ink,
                    new RectangleF(bounds.Left, y, bounds.Width, 22f), centred);
            }

            return y + 34f;
        }

        private float DrawWrapped(Graphics graphics, string line, Font bodyFont, Font boldFont,
                                 Rectangle bounds, float y, float lineHeight)
        {
            // A line that ends with a colon and is short is a lead-in such as
            // "TO WHOM IT MAY CONCERN:" - the barangay's papers print those in
            // bold, so I do too.
            bool bold = line.EndsWith(":", StringComparison.Ordinal) && line.Length < 60;
            Font font = bold ? boldFont : bodyFont;

            float available = bounds.Width;
            string[] words = line.Split(' ');
            System.Text.StringBuilder currentLine = new System.Text.StringBuilder();

            using (Brush ink = new SolidBrush(Color.FromArgb(20, 20, 20)))
            {
                foreach (string word in words)
                {
                    string candidate = currentLine.Length == 0 ? word : currentLine + " " + word;

                    if (graphics.MeasureString(candidate, font).Width > available && currentLine.Length > 0)
                    {
                        if (bold) CentreLine(graphics, currentLine.ToString(), font, ink, bounds, y);
                        else graphics.DrawString(currentLine.ToString(), font, ink, bounds.Left, y);

                        y += lineHeight;
                        currentLine.Length = 0;
                        currentLine.Append(word);
                    }
                    else
                    {
                        currentLine.Length = 0;
                        currentLine.Append(candidate);
                    }
                }

                if (currentLine.Length > 0)
                {
                    if (bold) CentreLine(graphics, currentLine.ToString(), font, ink, bounds, y);
                    else graphics.DrawString(currentLine.ToString(), font, ink, bounds.Left, y);
                }
            }

            return y + lineHeight;
        }

        private static void CentreLine(Graphics graphics, string text, Font font, Brush brush,
                                       Rectangle bounds, float y)
        {
            float width = graphics.MeasureString(text, font).Width;
            graphics.DrawString(text, font, brush, bounds.Left + ((bounds.Width - width) / 2f), y);
        }

        private float DrawSignature(Graphics graphics, DocumentContext context, IDocumentTemplate template,
                                    Rectangle bounds, float y, Font bodyFont, Font boldFont, Font smallFont)
        {
            BarangayProfile profile = context.Profile ?? BarangayProfile.MagugpoPoblacion;

            // I keep the signature block at the lower part of the page so every
            // document in the folder has it in the same place - that is what
            // makes a stack of them look tidy.
            float signatureTop = Math.Max(y + 24f, bounds.Bottom - 130f);

            string closing = template.GetClosingLine(context);
            if (!string.IsNullOrEmpty(closing))
                graphics.DrawString(closing, bodyFont, Brushes.Black, bounds.Left, signatureTop);

            float nameY = signatureTop + 46f;
            float centreLine = bounds.Left + 260f;

            using (Font signFont = new Font(AppThemeFontFamily(), 10.5f, FontStyle.Bold))
            {
                float width = graphics.MeasureString(profile.PunongBarangay, signFont).Width;
                graphics.DrawString(profile.PunongBarangay, signFont, Brushes.Black, centreLine - (width / 2f), nameY);

                float titleWidth = graphics.MeasureString("Punong Barangay", smallFont).Width;
                graphics.DrawString("Punong Barangay", smallFont, Brushes.Black,
                    centreLine - (titleWidth / 2f), nameY + 16f);
            }

            using (Pen line = new Pen(Color.FromArgb(90, 90, 90), 0.7f))
                graphics.DrawLine(line, centreLine - 95f, nameY - 4f, centreLine + 95f, nameY - 4f);

            graphics.DrawString("Prepared by: " + (context.Request == null ? string.Empty : context.Request.CollectedBy),
                smallFont, Brushes.Gray, bounds.Left, nameY + 40f);

            return nameY + 60f;
        }

        private void DrawFooter(Graphics graphics, DocumentContext context, Rectangle bounds, Font smallFont)
        {
            DocumentRequest request = context.Request;

            string left = request == null
                ? string.Empty
                : "Reference No. " + request.ReferenceNumber
                  + "   |   Filed " + request.DateRequested.ToString("dd MMM yyyy h:mm tt");

            if (request != null && request.IsPaid && !string.IsNullOrWhiteSpace(request.OfficialReceiptNumber))
                left += "   |   OR " + request.OfficialReceiptNumber
                      + (string.IsNullOrWhiteSpace(request.OrControlNumber)
                            ? string.Empty : " (control " + request.OrControlNumber + ")");

            float footerY = bounds.Bottom - 6f;

            using (Pen rule = new Pen(Color.FromArgb(200, 200, 200), 0.6f))
                graphics.DrawLine(rule, bounds.Left, footerY - 12f, bounds.Right, footerY - 12f);

            graphics.DrawString(left, smallFont, Brushes.Gray, bounds.Left, footerY - 8f);

            string right = AppConfig.ReportFooter + " | " + DateTime.Now.ToString("dd MMM yyyy h:mm tt");
            float width = graphics.MeasureString(right, smallFont).Width;
            graphics.DrawString(right, smallFont, Brushes.Gray, bounds.Right - width, footerY - 8f);
        }

        // ==================================================================
        //  Bits and pieces
        // ==================================================================

        /// <summary>
        /// The font the whole program uses, resolved once at startup
        /// (AppTheme.Resolve). The documents follow the same family as the
        /// screens, which is what makes the printed paper and the screen look
        /// like one system.
        /// </summary>
        private static string AppThemeFontFamily()
        {
            return UI.AppTheme.UiFamily;
        }

        /// <summary>
        /// Loads the barangay seal from the folder the program runs in.
        ///
        /// I load it from a copy of the file, not the file itself, because the
        /// Image object would otherwise hold the file open and a rebuild would
        /// fail with "the file is being used by another process". That one cost
        /// me an afternoon.
        /// </summary>
        private static Image LoadLogo()
        {
            try
            {
                string configured = AppConfig.LogoFile;

                string path = Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(AppConfig.ApplicationFolder, configured);

                if (!File.Exists(path)) return null;

                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (Image loaded = Image.FromStream(stream))
                {
                    return new Bitmap(loaded);
                }
            }
            catch (Exception error)
            {
                // A missing seal must not stop a certificate from printing.
                AppLog.Warn("The barangay seal could not be loaded: " + error.Message);
                return null;
            }
        }

        /// <summary>The body as plain lines, for the on-screen text version of
        /// a document. The word processor export and the accessibility check
        /// use it.</summary>
        public IList<string> GetPlainText(DocumentContext context)
        {
            List<string> lines = new List<string>();
            IDocumentTemplate template = _registry.Find(context.Request.DocumentType);

            BarangayProfile profile = context.Profile ?? BarangayProfile.MagugpoPoblacion;
            lines.Add(profile.BarangayName);
            lines.Add(profile.CityName + " | " + profile.ProvinceName);
            lines.Add(string.Empty);
            lines.Add(template == null ? "No wording found" : template.GetTitle(context));
            lines.Add(string.Empty);

            if (template != null) lines.AddRange(template.BuildBody(context));

            lines.Add(string.Empty);
            lines.Add(profile.PunongBarangay);
            lines.Add("Punong Barangay");
            return lines;
        }
    }
}
