// ---------------------------------------------------------------------------
//  ReportPrinter.cs - printing a report when Crystal Reports is not there.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;

namespace BarangayDocumentSystem.Services.Reports
{
    /// <summary>
    /// Draws a report on paper with the ordinary Windows printing.
    ///
    /// Why this exists next to Crystal Reports: the barangay asked for Crystal
    /// Reports, and where the runtime is installed the .rpt file does the
    /// laying out. But a report that can only be printed on one machine is not
    /// much use in an office that has one computer - so the same rows also come
    /// out of here: a title, the period, the columns of the table, the totals
    /// line, and a page number at the bottom. The columns repeat at the top of
    /// every page, because a table that loses its headings on page two has to
    /// be guessed at.
    ///
    /// A4 landscape, because these are wide tables and the barangay loads A4.
    /// </summary>
    public class ReportPrinter
    {
        private readonly ReportResult _result;
        private readonly string _printedBy;
        private int _rowIndex;
        private int _pageNumber;
        private float[] _columnWidths;

        public ReportPrinter(ReportResult result, string printedBy)
        {
            _result = result;
            _printedBy = string.IsNullOrWhiteSpace(printedBy) ? "-" : printedBy;
        }

        // ==================================================================
        //  Building the document
        // ==================================================================

        public PrintDocument BuildDocument()
        {
            if (_result == null) throw new ArgumentNullException("result");

            PrintDocument document = new PrintDocument();
            document.DocumentName = _result.Title;
            document.DefaultPageSettings.Landscape = true;
            document.DefaultPageSettings.Margins = new Margins(60, 60, 60, 60);

            document.PrintPage += delegate (object sender, PrintPageEventArgs e)
            {
                e.HasMorePages = DrawPage(e.Graphics, e.MarginBounds);
            };

            return document;
        }

        /// <summary>Hands the report to the printer, asking first unless the
        /// caller says not to. Every print in this program goes through a
        /// dialog, because paper and ink are real money in a barangay hall.</summary>
        public void Print(bool showDialog, IWin32Window owner)
        {
            using (PrintDocument document = BuildDocument())
            {
                if (showDialog)
                {
                    using (PrintDialog dialog = new PrintDialog())
                    {
                        dialog.Document = document;
                        dialog.UseEXDialog = true;

                        if (dialog.ShowDialog(owner) != DialogResult.OK) return;
                    }
                }

                _rowIndex = 0;
                _pageNumber = 0;
                document.Print();
            }
        }

        public string Describe()
        {
            if (_result == null) return "No report";

            int rows = _result.Table == null ? 0 : _result.Table.Rows.Count;
            return _result.Title + " - " + _result.Subtitle + " - " + rows + " row(s)";
        }

        // ==================================================================
        //  Drawing one page
        // ==================================================================

        private bool DrawPage(Graphics graphics, Rectangle bounds)
        {
            _pageNumber++;

            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            string family = string.IsNullOrEmpty(UI.AppTheme.UiFamily)
                ? FontFamily.GenericSansSerif.Name
                : UI.AppTheme.UiFamily;

            using (Font heading = new Font(family, 15f, FontStyle.Bold))
            using (Font sub = new Font(family, 10f, FontStyle.Regular))
            using (Font small = new Font(family, 8.5f, FontStyle.Regular))
            using (Font headerFont = new Font(family, 8.5f, FontStyle.Bold))
            using (Brush ink = new SolidBrush(Color.Black))
            using (Brush muted = new SolidBrush(Color.FromArgb(0x55, 0x5F, 0x70)))
            using (Pen rule = new Pen(Color.FromArgb(0x99, 0xA3, 0xB0), 1f))
            {
                float y = bounds.Top;

                graphics.DrawString(_result.Title, heading, ink, bounds.Left, y);
                y += heading.GetHeight(graphics) + 4;

                graphics.DrawString(_result.Subtitle, sub, muted, bounds.Left, y);
                y += sub.GetHeight(graphics) + 4;

                string printed = "Printed " + DateTime.Now.ToString("dd MMMM yyyy, h:mm tt")
                               + " by " + _printedBy
                               + "   |   Page " + _pageNumber;

                graphics.DrawString(printed, small, muted, bounds.Left, y);
                y += small.GetHeight(graphics) + 8;

                graphics.DrawLine(rule, bounds.Left, y, bounds.Right, y);
                y += 6;

                DataTable table = _result.Table;
                if (table == null || table.Columns.Count == 0)
                {
                    graphics.DrawString("This report has no columns to print.", sub, ink, bounds.Left, y);
                    return false;
                }

                if (_columnWidths == null) _columnWidths = WorkOutWidths(table, bounds.Width);

                // The column headings are drawn on every page, on purpose.
                float x = bounds.Left;
                for (int c = 0; c < table.Columns.Count; c++)
                {
                    string caption = HeadingFor(table.Columns[c].ColumnName, c);
                    graphics.DrawString(caption, headerFont, ink,
                        new RectangleF(x, y, _columnWidths[c] - 6, 26));
                    x += _columnWidths[c];
                }

                y += 22;
                graphics.DrawLine(rule, bounds.Left, y, bounds.Right, y);
                y += 4;

                float rowHeight = headerFont.GetHeight(graphics) + 6;
                float bottomLimit = bounds.Bottom - 40;

                while (_rowIndex < table.Rows.Count)
                {
                    if (y + rowHeight > bottomLimit)
                    {
                        DrawFooter(graphics, small, muted, bounds);
                        return true;    // another page
                    }

                    x = bounds.Left;
                    for (int c = 0; c < table.Columns.Count; c++)
                    {
                        object value = table.Rows[_rowIndex][c];
                        string text = Format(value, table.Columns[c].DataType);

                        graphics.DrawString(text, small, ink,
                            new RectangleF(x, y, _columnWidths[c] - 6, rowHeight + 4));
                        x += _columnWidths[c];
                    }

                    y += rowHeight;
                    _rowIndex++;
                }

                if (!string.IsNullOrWhiteSpace(_result.Totals))
                {
                    y += 6;
                    graphics.DrawLine(rule, bounds.Left, y, bounds.Right, y);
                    y += 6;

                    using (Font totalFont = new Font(family, 10f, FontStyle.Bold))
                        graphics.DrawString(_result.Totals, totalFont, ink, bounds.Left, y);
                }

                DrawFooter(graphics, small, muted, bounds);
                return false;
            }
        }

        private void DrawFooter(Graphics graphics, Font font, Brush brush, Rectangle bounds)
        {
            string footer = string.IsNullOrWhiteSpace(AppConfig.ReportFooter)
                ? AppConfig.BarangayName + " - " + AppConfig.CityName
                : AppConfig.ReportFooter;

            graphics.DrawString(footer, font, brush, bounds.Left, bounds.Bottom - 22);
        }

        // ==================================================================
        //  Formatting
        // ==================================================================

        /// <summary>Proportional column widths. I size each column by the
        /// longest word it has to hold, capped, so one long remarks column does
        /// not squeeze the date column into a sliver.</summary>
        private static float[] WorkOutWidths(DataTable table, float totalWidth)
        {
            float[] weights = new float[table.Columns.Count];
            float sum = 0f;

            for (int c = 0; c < table.Columns.Count; c++)
            {
                int longest = Math.Max(HeadingFor(table.Columns[c].ColumnName, c).Length, 4);

                foreach (DataRow row in table.Rows)
                {
                    string text = Convert.ToString(row[c]);
                    if (text != null && text.Length > longest) longest = Math.Min(text.Length, 34);
                }

                weights[c] = Math.Max(6, Math.Min(longest, 34));
                sum += weights[c];
            }

            float[] widths = new float[weights.Length];
            for (int c = 0; c < weights.Length; c++)
                widths[c] = Math.Max(60f, totalWidth * (weights[c] / sum));

            return widths;
        }

        private static string HeadingFor(string columnName, int index)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return "#" + (index + 1);

            return columnName.Replace("_", " ").ToUpperInvariant();
        }

        private static string Format(object value, Type type)
        {
            if (value == null || value == DBNull.Value) return string.Empty;

            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                return Convert.ToDecimal(value).ToString("#,##0.00");

            if (type == typeof(DateTime))
                return Convert.ToDateTime(value).ToString("dd MMM yyyy");

            string text = Convert.ToString(value);
            return text.Length > 40 ? text.Substring(0, 38) + ".." : text;
        }
    }
}
