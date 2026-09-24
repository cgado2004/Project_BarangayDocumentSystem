using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Forms
{
    /// <summary>
    /// The THEME half of DocumentPreviewForm. Partial class, separate file, so Jonathan's
    /// designer file keeps compiling untouched. Applies the barangay brand:
    /// navy palette + Inter typography on every control, on load.
    /// </summary>
    public partial class DocumentPreviewForm
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            BackColor = ModernTheme.Canvas;

            // the document itself stays paper-white with readable serif-ish size
            txtDocument.BackColor = Color.White;
            txtDocument.ForeColor = ModernTheme.Ink;
            txtDocument.Font = ModernTheme.F(9.75f, false);
            txtDocument.BorderStyle = BorderStyle.FixedSingle;

            lblState.BackColor = ModernTheme.Canvas;
            lblState.ForeColor = ModernTheme.Muted;
            lblState.Font = ModernTheme.F(9f, false);
            pnlActions.BackColor = ModernTheme.Canvas;

            ModernTheme.StylePrimary(btnPrint);
            ModernTheme.StyleSecondary(btnPrintPreview);
            ModernTheme.StyleSecondary(btnClose);
        }
    }
}
