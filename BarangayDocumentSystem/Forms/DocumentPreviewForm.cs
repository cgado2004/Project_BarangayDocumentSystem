using System.Windows.Forms;
using System.Drawing;
using System;
using System.IO;
using System.Data;
using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Forms;

/// <summary>
/// Shows the rendered document and offers Copy / Save / Print.
///
/// Printing uses System.Drawing.Printing.PrintDocument, which is part of the
/// framework — no third-party package needed.
/// </summary>
public partial class DocumentPreviewForm : Form
{
    private readonly string _documentText;
    private string[] _linesToPrint = Array.Empty<string>();
    private int _lineIndex;

    public DocumentPreviewForm(string title, string documentText)
    {
        InitializeComponent();
        UiFactory.MakeButtonsFlexible(this);
        Text = $"Preview — {title}";
        _documentText = documentText;
    }

    private void DocumentPreviewForm_Load(object sender, EventArgs e)
    {
        txtDocument.Text = _documentText;
        txtDocument.SelectionStart = 0;
    }

    private void btnCopy_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_documentText)) return;

        // Requires [STAThread] on Main — the clipboard is a COM API.
        Clipboard.SetText(_documentText);
        Dialog.Info("Document copied to the clipboard.", "Copied");
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"{Text.Replace("Preview — ", "").Replace(' ', '_')}.txt"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            File.WriteAllText(dialog.FileName, _documentText);
            Dialog.Info($"Saved to:\n\n{dialog.FileName}", "Saved");
        }
        catch (Exception ex)
        {
            // File I/O can fail for many reasons (permissions, disk full,
            // path too long) — report rather than crash.
            Dialog.Error($"Could not save the file.\n\n{ex.Message}", "Save failed");
        }
    }

    private void btnPrint_Click(object sender, EventArgs e)
    {
        // .NET Framework has no Split(string) overload -- only Split(string[], ...).
        _linesToPrint = _documentText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        _lineIndex = 0;

        using var printDoc = new System.Drawing.Printing.PrintDocument();
        printDoc.PrintPage += PrintDoc_PrintPage;

        using var preview = new PrintPreviewDialog
        {
            Document = printDoc,
            Width = 900,
            Height = 700,
            StartPosition = FormStartPosition.CenterParent
        };

        try
        {
            preview.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Dialog.Warn($"Could not open the print preview.\n\n{ex.Message}\n\n"
                      + "This usually means no printer is installed on this machine.",
                        "Print failed");
        }
    }

    /// <summary>
    /// Draws one page. PrintPage fires repeatedly while HasMorePages is true,
    /// so we track our position in the line array between calls.
    /// </summary>
    private void PrintDoc_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
    {
        if (e.Graphics is null) return;

        using var font = new Font("Consolas", 10);
        float lineHeight = font.GetHeight(e.Graphics);
        float y = e.MarginBounds.Top;
        int linesPerPage = (int)(e.MarginBounds.Height / lineHeight);
        int printed = 0;

        while (printed < linesPerPage && _lineIndex < _linesToPrint.Length)
        {
            e.Graphics.DrawString(_linesToPrint[_lineIndex], font, Brushes.Black,
                                  e.MarginBounds.Left, y);
            y += lineHeight;
            _lineIndex++;
            printed++;
        }

        e.HasMorePages = _lineIndex < _linesToPrint.Length;
        if (!e.HasMorePages) _lineIndex = 0;   // reset for a second preview
    }

    private void btnClose_Click(object sender, EventArgs e) => Close();
}

/// <summary>
/// Where every document is shown and printed.
///
/// When the SAP Crystal Reports runtime is installed (the project then builds
/// with the CRYSTAL symbol, see the .csproj), the document opens in a Crystal
/// Reports viewer using Reports\DocumentReport.rpt, which has its own Print and
/// Export buttons. If the runtime or the .rpt file is missing, it falls back to
/// the plain text preview above, so printing never stops working.
/// </summary>
public static class DocumentPrinter
{
    public const string ReportFolder = "Reports";
    public const string ReportFileName = "DocumentReport.rpt";

    public static void Show(IWin32Window owner, DocumentRequest request, DocumentRenderer renderer)
    {
#if CRYSTAL
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ReportFolder, ReportFileName);

        if (File.Exists(path))
        {
            try
            {
                using var viewer = new CrystalReportForm(request.GetDocumentName(), path,
                                                         renderer.BuildReportData(request));
                viewer.ShowDialog(owner);
                return;
            }
            catch (Exception ex)
            {
                Dialog.Warn("The Crystal report could not be opened, so the plain preview is shown instead.\n\n" +
                            ex.Message, "Crystal Reports");
            }
        }
        // No .rpt yet: fall through to the built-in formatted print preview.
#endif
        try
        {
            FormattedDocumentPrinter.ShowPreview(owner, request.GetDocumentName(),
                                                 renderer.BuildReportData(request).Rows[0]);
        }
        catch (Exception ex)
        {
            // No printer installed, or drawing failed: show the plain text version.
            Dialog.Warn("The formatted print preview could not be opened, so the plain preview is shown.\n\n" +
                        ex.Message, "Print");
            using var preview = new DocumentPreviewForm(request.GetDocumentName(), renderer.Render(request));
            preview.ShowDialog(owner);
        }
    }
}

/// <summary>
/// Built-in formatted document (letterhead, title, body, signature, O.R. line),
/// drawn with System.Drawing and shown in the standard print preview, which has
/// Print and zoom buttons. Used whenever there is no Crystal report file, so
/// printing works without any designer step. Uses the same data row as Crystal.
/// </summary>
public static class FormattedDocumentPrinter
{
    public static void ShowPreview(IWin32Window owner, string title, DataRow row)
    {
        using var printDoc = new System.Drawing.Printing.PrintDocument();
        printDoc.DocumentName = title;
        printDoc.PrintPage += (s, e) => Draw(e, row);

        using var preview = new PrintPreviewDialog
        {
            Document = printDoc,
            Width = 900,
            Height = 760,
            StartPosition = FormStartPosition.CenterParent
        };
        preview.ShowDialog(owner);
    }

    private static string F(DataRow row, string column) => Convert.ToString(row[column]) ?? string.Empty;

    private static void Draw(System.Drawing.Printing.PrintPageEventArgs e, DataRow row)
    {
        var g = e.Graphics;
        if (g is null) return;

        var area = e.MarginBounds;
        float y = area.Top;
        var center = new StringFormat { Alignment = StringAlignment.Center };

        using var small = new Font("Times New Roman", 11);
        using var smallBold = new Font("Times New Roman", 12, FontStyle.Bold);
        using var big = new Font("Times New Roman", 15, FontStyle.Bold);
        using var titleFont = new Font("Times New Roman", 18, FontStyle.Bold);
        using var bodyFont = new Font("Times New Roman", 12);

        void Line(string text, Font font)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var r = new RectangleF(area.Left, y, area.Width, font.GetHeight(g) + 2);
            g.DrawString(text, font, Brushes.Black, r, center);
            y += font.GetHeight(g) + 2;
        }

        // Letterhead
        Line("Republic of the Philippines", small);
        Line("Province of " + F(row, "ProvinceName").Replace("Province of ", ""), small);
        Line(F(row, "CityName"), small);
        Line(F(row, "BarangayName"), big);
        Line("OFFICE OF THE PUNONG BARANGAY", smallBold);
        y += 6;
        g.DrawLine(Pens.Black, area.Left, y, area.Right, y);
        y += 24;

        // Title
        Line(F(row, "Title"), titleFont);
        Line(F(row, "Subtitle"), small);
        y += 18;

        // Body (wraps; paragraphs separated by blank lines)
        string body = F(row, "Body");
        string oath = F(row, "Oath");
        if (oath.Length > 0) body += "\n\n" + oath;
        var bodyRect = new RectangleF(area.Left, y, area.Width, area.Bottom - y - 170);
        g.DrawString(body, bodyFont, Brushes.Black, bodyRect);
        float bodyHeight = g.MeasureString(body, bodyFont, (int)area.Width).Height;
        y += Math.Min(bodyHeight, bodyRect.Height) + 24;

        // Issued line and signature block, right aligned
        var right = new StringFormat { Alignment = StringAlignment.Far };
        float sy = Math.Max(y, area.Bottom - 150);
        void RightLine(string text, Font font)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            g.DrawString(text, font, Brushes.Black,
                         new RectangleF(area.Left, sy, area.Width, font.GetHeight(g) + 2), right);
            sy += font.GetHeight(g) + 4;
        }
        RightLine(F(row, "IssuedLine"), small);
        sy += 20;
        RightLine(F(row, "PunongBarangay").ToUpper(), smallBold);
        RightLine("Punong Barangay", small);

        // Footer: reference, fee and O.R.
        string foot = "Ref. No.: " + F(row, "ReferenceNo");
        string fee = F(row, "FeeText");
        if (fee.Length > 0) foot += "     Fee: " + fee;
        string orNo = F(row, "ORNo");
        if (orNo.Length > 0) foot += "     O.R. No.: " + orNo;
        g.DrawString(foot, small, Brushes.Black, area.Left, area.Bottom - small.GetHeight(g));

        e.HasMorePages = false;
    }
}

#if CRYSTAL
/// <summary>
/// Hosts the Crystal Reports viewer. The report is loaded from the .rpt file and
/// bound to the one-row "Document" table from <see cref="DocumentRenderer.BuildReportData"/>.
/// </summary>
public sealed class CrystalReportForm : Form
{
    private readonly CrystalDecisions.CrystalReports.Engine.ReportDocument _report = new();

    public CrystalReportForm(string title, string reportPath, DataTable data)
    {
        Text = $"Print — {title}";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(980, 760);
        MinimizeBox = false;

        _report.Load(reportPath);
        _report.SetDataSource(data);

        var viewer = new CrystalDecisions.Windows.Forms.CrystalReportViewer
        {
            Dock = DockStyle.Fill,
            ReportSource = _report,
            ShowGroupTreeButton = false,
            ShowParameterPanelButton = false,
            ShowCopyButton = false
        };
        Controls.Add(viewer);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // The report holds a print-engine job; release it when the window closes.
        _report.Close();
        _report.Dispose();
        base.OnFormClosed(e);
    }
}
#endif

