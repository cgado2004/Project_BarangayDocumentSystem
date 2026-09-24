using System.Windows.Forms;
using System.Drawing;
using System;
using System.IO;
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
