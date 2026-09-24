using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Forms
{
    /// <summary>
    /// The THEME half of PaymentForm. Partial class, separate file, so Jonathan's
    /// designer file keeps compiling untouched. Applies the barangay brand:
    /// navy palette + Inter typography on every control, on load.
    /// </summary>
    public partial class PaymentForm
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

            ModernTheme.StyleCaption(lblReceiptCaption);
            ModernTheme.StyleText(txtReceipt);

            lblSummary.BackColor = ModernTheme.Canvas;
            lblSummary.ForeColor = ModernTheme.SlateInk;
            lblSummary.Font = ModernTheme.F(10f, false);

            ModernTheme.StylePrimary(btnSave);
            ModernTheme.StyleSecondary(btnCancel);
        }
    }
}
