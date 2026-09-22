using System;
using System.Drawing.Printing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Printing;

namespace BarangayDocumentSystem.Forms
{
    public partial class DocumentPreviewForm : Form
    {
        private readonly string documentText;
        private readonly bool canPrint;

        public DocumentPreviewForm() { InitializeComponent(); }

        public DocumentPreviewForm(DocumentRequest request, string text) : this()
        {
            documentText = text;
            canPrint = request.Status == RequestStatus.Released;
            Text = request.ReferenceNumber + " - " + request.DocumentName;
            txtDocument.Text = text;
            btnPrint.Enabled = canPrint;
            btnPrintPreview.Enabled = canPrint;
            lblState.Text = canPrint ? "Released document - finalized text." :
                "Draft preview. Release the request before printing the document.";
        }

        private void PrintPreview(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                RequirePrinter();
                using (var job = new DocumentPrintJob(Text, documentText))
                using (var preview = new PrintPreviewDialog())
                {
                    preview.Document = job;
                    preview.Width = 1000;
                    preview.Height = 760;
                    // The standard preview toolbar includes Print, so drafts use the text preview only.
                    if (!canPrint) throw new InvalidOperationException("Release this request before opening the print preview.");
                    preview.ShowDialog(this);
                }
            });
        }

        private void PrintDocument(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                if (!canPrint) throw new InvalidOperationException("Release this request before printing.");
                RequirePrinter();
                using (var job = new DocumentPrintJob(Text, documentText))
                using (var dialog = new PrintDialog { Document = job, UseEXDialog = true, AllowSomePages = false })
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK) job.Print();
                }
            });
        }

        private static void RequirePrinter()
        {
            if (PrinterSettings.InstalledPrinters.Count == 0)
                throw new InvalidOperationException("No printer is installed. Install a printer or Microsoft Print to PDF to use printing.");
        }
    }
}
