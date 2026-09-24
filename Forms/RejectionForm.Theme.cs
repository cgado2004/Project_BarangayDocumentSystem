using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Forms
{
    /// <summary>
    /// The THEME half of RejectionForm. Partial class, separate file, so Jonathan's
    /// designer file keeps compiling untouched. Applies the barangay brand:
    /// navy palette + Inter typography on every control, on load.
    /// </summary>
    public partial class RejectionForm
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            BackColor = ModernTheme.Canvas;
            pnlBody.BackColor = ModernTheme.Canvas;
            tblFields.BackColor = ModernTheme.Canvas;

            ModernTheme.StyleCaption(lblReasonCaption);
            ModernTheme.StyleText(txtReason);

            lblSummary.BackColor = ModernTheme.Canvas;
            lblSummary.ForeColor = ModernTheme.SlateInk;
            lblSummary.Font = ModernTheme.F(10f, false);

            // rejecting is destructive: the confirm button wears crimson, not navy
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.BackColor = ModernTheme.Crimson;
            btnSave.ForeColor = Color.White;
            btnSave.Font = ModernTheme.F(9.5f, true);
            btnSave.Cursor = Cursors.Hand;
            ModernTheme.StyleSecondary(btnCancel);
        }
    }
}
