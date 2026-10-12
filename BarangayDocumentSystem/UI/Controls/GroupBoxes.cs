// ---------------------------------------------------------------------------
//  GroupBoxes.cs - the group boxes the barangay asked for.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BarangayDocumentSystem.UI.Controls
{
    /// <summary>
    /// A group box with a clean flat frame.
    ///
    /// The review asked for the name to be shown inside a group box, so this is
    /// a real group box - a Windows one - with the dated engraved border
    /// replaced by a flat one that matches the rest of the interface. I kept
    /// the class name honest (it is still a GroupBox) because that is what the
    /// person reviewing the screen will look for.
    /// </summary>
    public class FlatGroupBox : GroupBox
    {
        public FlatGroupBox(string caption)
        {
            Text = caption;
            Font = AppTheme.SmallBold;
            ForeColor = AppTheme.Primary;
            BackColor = AppTheme.Surface;
            Padding = new Padding(AppTheme.Gap3, AppTheme.Gap4, AppTheme.Gap3, AppTheme.Gap3);
            Margin = new Padding(0, 0, AppTheme.Gap3, AppTheme.Gap2);

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.Smooth(e.Graphics);

            // A flat frame with the caption sitting on the top edge.
            int captionHeight = Font.Height;
            int top = captionHeight / 2;

            using (Pen pen = new Pen(AppTheme.Border, 1f))
            {
                e.Graphics.DrawLine(pen, 0, top, AppTheme.Gap1 + 2, top);
                e.Graphics.DrawLine(pen, AppTheme.Gap1 + TextRenderer.MeasureText(Text, Font).Width + AppTheme.Gap2,
                    top, Width - 1, top);
                e.Graphics.DrawLine(pen, 0, top, 0, Height - 1);
                e.Graphics.DrawLine(pen, Width - 1, top, Width - 1, Height - 1);
                e.Graphics.DrawLine(pen, 0, Height - 1, Width - 1, Height - 1);
            }

            using (Brush caption = new SolidBrush(AppTheme.Primary))
                e.Graphics.DrawString(Text, Font, caption, AppTheme.Gap1, 0);
        }
    }

    /// <summary>
    /// The group box used wherever a person's name is shown - the resident
    /// form, the new request form, the payment screen. One name reads the same
    /// way everywhere: Last, First, Middle, Suffix, in capitals on the line the
    /// clerk is reading.
    /// </summary>
    public class NameGroupBox : FlatGroupBox
    {
        private readonly Label _name;
        private readonly Label _note;

        public NameGroupBox(string caption)
            : base(caption)
        {
            Height = 78;
            Padding = new Padding(AppTheme.Gap3, AppTheme.Gap4, AppTheme.Gap3, AppTheme.Gap2);

            _name = new Label();
            _name.Font = new Font(AppTheme.UiFamily, 13f, FontStyle.Bold);
            _name.ForeColor = AppTheme.Ink;
            _name.AutoSize = false;
            _name.Dock = DockStyle.Top;
            _name.Height = 26;
            _name.TextAlign = ContentAlignment.MiddleLeft;

            _note = new Label();
            _note.Font = AppTheme.Small;
            _note.ForeColor = AppTheme.Muted;
            _note.AutoSize = false;
            _note.Dock = DockStyle.Top;
            _note.Height = 20;
            _note.TextAlign = ContentAlignment.MiddleLeft;

            // Added in reverse so the dock order comes out name, then note.
            Controls.Add(_note);
            Controls.Add(_name);
        }

        public string NameText
        {
            get { return _name.Text; }
            set { _name.Text = value; }
        }

        public string NoteText
        {
            get { return _note.Text; }
            set { _note.Text = value; }
        }
    }
}
