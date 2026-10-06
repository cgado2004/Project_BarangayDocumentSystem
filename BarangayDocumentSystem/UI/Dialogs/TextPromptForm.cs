// ---------------------------------------------------------------------------
//  TextPromptForm.cs - the one small window that asks for a sentence.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BarangayDocumentSystem.UI.Dialogs
{
    /// <summary>
    /// Asks for one thing before something is written: a reason, a name, a
    /// number.
    ///
    /// I wrote this instead of using the framework's own input box because the
    /// framework's box cannot say WHY it is asking. "Reason:" gets you "n/a";
    /// "Why is this resident being taken off the active list? Six months from
    /// now this sentence is the answer to that question" gets you a sentence
    /// somebody can act on. The question is always shown above the box, in the
    /// caller's words.
    /// </summary>
    public class TextPromptForm : Form
    {
        private TextBox _answer;
        private readonly bool _mustBeFilledIn;

        private TextPromptForm(string title, string question, string initial, bool multiline, bool mustBeFilledIn)
        {
            _mustBeFilledIn = mustBeFilledIn;

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            int height = multiline ? 380 : 280;
            ClientSize = new Size(560, height);

            Label heading = UiFactory.PageHeading(title);
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4);
            heading.MaximumSize = new Size(500, 0);

            Label words = UiFactory.Caption(question);
            words.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 40);
            words.Size = new Size(500, multiline ? 60 : 48);

            _answer = multiline
                ? (TextBox)UiFactory.TextBox(string.Empty, 500)
                : UiFactory.TextBox(string.Empty, 200);

            _answer.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 96);
            _answer.Width = 500;

            if (multiline)
            {
                _answer.Multiline = true;
                _answer.Height = 140;
                _answer.ScrollBars = ScrollBars.Vertical;
            }

            _answer.Text = initial ?? string.Empty;
            _answer.Font = new Font(AppTheme.UiFamily, 11f, FontStyle.Regular);

            Label required = UiFactory.Hint(mustBeFilledIn
                ? "This cannot be left blank."
                : "You can leave this blank if there is nothing to say.");
            required.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 96 + (multiline ? 148 : 34));
            required.Width = 500;
            required.ForeColor = mustBeFilledIn ? AppTheme.Danger : AppTheme.MutedSoft;

            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 60;
            footer.BackColor = AppTheme.Surface;

            Button ok = UiFactory.PrimaryButton("Continue");
            ok.Location = new Point(ClientSize.Width - ok.Width - AppTheme.PageMargin, 12);
            ok.Click += delegate (object sender, EventArgs e) { Accept(); };

            Button cancel = UiFactory.SecondaryButton("Cancel");
            cancel.Location = new Point(ok.Left - cancel.Width - AppTheme.Gap2, 12);
            cancel.Click += delegate (object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.Add(ok);
            footer.Controls.Add(cancel);

            Controls.Add(heading);
            Controls.Add(words);
            Controls.Add(_answer);
            Controls.Add(required);
            Controls.Add(footer);

            AcceptButton = ok;
            CancelButton = cancel;

            Shown += delegate (object sender, EventArgs e)
            {
                _answer.Focus();
                _answer.SelectAll();
            };
        }

        private void Accept()
        {
            if (_mustBeFilledIn && string.IsNullOrWhiteSpace(_answer.Text))
            {
                Dialog.Warn(this, "Please type a short sentence - this one is needed.", "Nothing typed yet");
                _answer.Focus();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Asks for a sentence and hands it back, or null when Cancel is
        /// pressed. Every caller treats null as "do nothing", which is the safe
        /// way round for a window that stands between a clerk and a record.
        /// </summary>
        public static string Ask(IWin32Window owner, string title, string question, string initial,
                                 bool multiline, bool mustBeFilledIn)
        {
            using (TextPromptForm form = new TextPromptForm(title, question, initial, multiline, mustBeFilledIn))
                return form.ShowDialog(owner) == DialogResult.OK ? form._answer.Text.Trim() : null;
        }

        public static string Ask(IWin32Window owner, string title, string question)
        {
            return Ask(owner, title, question, string.Empty, false, true);
        }
    }
}
