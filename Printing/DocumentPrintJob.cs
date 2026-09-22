using System.Drawing;
using System.Drawing.Printing;

namespace BarangayDocumentSystem.Printing
{
    public class DocumentPrintJob : PrintDocument
    {
        private readonly string documentText;
        private readonly Font printFont = new Font("Segoe UI", 11F);
        private int offset;

        public DocumentPrintJob(string title, string text)
        {
            DocumentName = title;
            documentText = text;
            DefaultPageSettings.Margins = new Margins(70, 70, 65, 65);
        }

        protected override void OnBeginPrint(PrintEventArgs e)
        {
            offset = 0;
            base.OnBeginPrint(e);
        }

        protected override void OnPrintPage(PrintPageEventArgs e)
        {
            offset += TextPaginator.DrawPage(e.Graphics, printFont, documentText, offset, e.MarginBounds);
            e.HasMorePages = offset < documentText.Length;
            base.OnPrintPage(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) printFont.Dispose();
            base.Dispose(disposing);
        }
    }
}
