using System;
using System.Drawing;

namespace BarangayDocumentSystem.Printing
{
    public static class TextPaginator
    {
        public static int DrawPage(Graphics graphics, Font font, string text, int offset, RectangleF bounds)
        {
            if (offset >= text.Length) return 0;
            string remaining = text.Substring(offset);
            using (var format = new StringFormat(StringFormat.GenericTypographic))
            {
                format.FormatFlags |= StringFormatFlags.LineLimit;
                int charactersFitted;
                int linesFilled;
                graphics.MeasureString(remaining, font, bounds.Size, format, out charactersFitted, out linesFilled);
                if (charactersFitted <= 0)
                    throw new InvalidOperationException("The selected paper size or margins leave no space for document text.");
                graphics.DrawString(remaining.Substring(0, charactersFitted), font, Brushes.Black, bounds, format);
                return charactersFitted;
            }
        }
    }
}
