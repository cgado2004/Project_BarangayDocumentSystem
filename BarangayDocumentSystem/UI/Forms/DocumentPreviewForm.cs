// ---------------------------------------------------------------------------
//  DocumentPreviewForm.cs - the paper, before it is handed over.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services.Documents;

namespace BarangayDocumentSystem.UI.Forms
{
    /// <summary>
    /// Shows the finished document exactly as it will come out of the printer,
    /// and then prints it.
    ///
    /// Why this window exists at all: a barangay certificate is a signed paper.
    /// A wrong name on a signed paper costs somebody a trip back to the hall, so
    /// the clerk sees the whole page first - the seal, the wording, the
    /// reference number, the receipt number - and only then presses Print.
    ///
    /// The wording is also shown as plain text beside the page, because the
    /// punong barangay reads sentences, not pixels. Both come from the same
    /// template, so what is read is what is printed.
    /// </summary>
    public class DocumentPreviewForm : Form
    {
        private readonly DocumentContext _context;
        private readonly DocumentRenderer _renderer = new DocumentRenderer();
        private readonly PrintDocument _document;
        private readonly PrintPreviewControl _preview;
        private TextBox _wording;
        private CheckBox _plainText;
        private Label _note;

        public string Message { get; private set; }

        public DocumentPreviewForm(DocumentRequest request, Resident resident, IList<Dependent> dependents)
        {
            _context = new DocumentContext();
            _context.Resident = resident;
            _context.Request = request;
            _context.Dependents = dependents;
            _context.Profile = BarangayProfile.Current ?? BarangayProfile.FromConfig();

            _document = _renderer.BuildDocument(_context);

            Text = _renderer.Describe(_context);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            ClientSize = new Size(1040, 780);
            MinimumSize = new Size(820, 640);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Label heading = UiFactory.PageHeading(_context.Request.GetDocumentName());
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4);
            heading.MaximumSize = new Size(900, 0);

            _note = UiFactory.Hint(NoteText());
            _note.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 36);
            _note.Size = new Size(960, 40);

            _plainText = UiFactory.CheckBox("Show the wording as plain text, for reading", false);
            _plainText.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 80);
            _plainText.CheckedChanged += delegate (object sender, EventArgs e) { ShowWording(); };

            _preview = new PrintPreviewControl();
            _preview.Document = _document;
            _preview.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 112);
            _preview.Size = new Size(480, 580);
            _preview.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            _preview.BackColor = AppTheme.Canvas;
            _preview.Zoom = 0.7d;

            _wording = new TextBox();
            _wording.Multiline = true;
            _wording.ReadOnly = true;
            _wording.ScrollBars = ScrollBars.Vertical;
            _wording.BorderStyle = BorderStyle.FixedSingle;
            _wording.BackColor = AppTheme.Surface;
            _wording.ForeColor = AppTheme.Ink;
            _wording.Font = AppTheme.Mono;
            _wording.Location = new Point(AppTheme.PageMargin + 496, AppTheme.Gap4 + 112);
            _wording.Size = new Size(470, 580);
            _wording.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            _wording.Text = string.Join(Environment.NewLine, _renderer.GetPlainText(_context));
            _wording.Visible = false;

            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 60;
            footer.BackColor = AppTheme.Surface;

            Button print = UiFactory.PrimaryButton("Print this document");
            print.Width = 200;
            print.Location = new Point(ClientSize.Width - 200 - AppTheme.PageMargin, 12);
            print.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            print.Click += delegate (object sender, EventArgs e) { Print(); };

            Button close = UiFactory.SecondaryButton("Close");
            close.Location = new Point(print.Left - close.Width - AppTheme.Gap2, 12);
            close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            close.Click += delegate (object sender, EventArgs e) { Close(); };

            footer.Controls.Add(print);
            footer.Controls.Add(close);

            Controls.Add(heading);
            Controls.Add(_note);
            Controls.Add(_plainText);
            Controls.Add(_preview);
            Controls.Add(_wording);
            Controls.Add(footer);

            CancelButton = close;
        }

        private string NoteText()
        {
            string line = _context.Request.ReferenceNumber
                        + "  |  " + _context.Resident.GetFullName()
                        + "  |  fee " + _context.Request.GetFeeText();

            if (!_context.Request.IsReleased)
                line += "   -   NOT released yet. This is a preview: the paper is handed over only after the "
                      + "request is released at the counter.";

            if (!string.IsNullOrWhiteSpace(AppConfig.ReportFooter))
                line += Environment.NewLine + AppConfig.ReportFooter;

            return line;
        }

        private void ShowWording()
        {
            _wording.Visible = _plainText.Checked;
            _preview.Width = _plainText.Checked ? 480 : 960;
        }

        private void Print()
        {
            if (!_context.Request.IsReleased)
            {
                bool anyway = Dialog.Confirm(this, "This request has not been released yet, so this print "
                    + "is for checking only." + Environment.NewLine + Environment.NewLine
                    + "Continue and choose a printer?", "Not released yet", null);

                if (!anyway) return;
            }

            using (PrintDialog dialog = new PrintDialog())
            {
                dialog.Document = _document;
                dialog.UseEXDialog = true;

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    _document.Print();
                    Message = "Printed " + _context.Request.GetDocumentName() + " for "
                            + _context.Resident.GetFullName() + ".";
                }
                catch (Exception error)
                {
                    Dialog.FromException(this, "I could not print the document.", error);
                }
            }
        }
    }
}
