using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Forms
{
    /// <summary>
    /// The THEME half of RequestForm. Partial class, separate file, so Jonathan's
    /// designer file keeps compiling untouched. Applies the barangay brand:
    /// navy palette + Inter typography on every control, on load.
    /// </summary>
    public partial class RequestForm
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
            tblContent.BackColor = ModernTheme.Canvas;
            tblFields.BackColor = ModernTheme.Canvas;

            foreach (var caption in new[] { lblResidentCaption, lblDocumentCaption, lblPurposeCaption,
                lblBusinessNameCaption, lblBusinessAddressCaption, lblBusinessNatureCaption })
                ModernTheme.StyleCaption(caption);

            foreach (Control field in new Control[] { cmbResident, cmbDocument, txtPurpose,
                txtBusinessName, txtBusinessAddress, txtBusinessNature, cmbScope })
                ModernTheme.StyleText(field);

            // the live fee read-out: gold highlight, ink text
            lblFee.BackColor = ModernTheme.GoldSoft;
            lblFee.ForeColor = ModernTheme.Ink;
            lblFee.Font = ModernTheme.F(10.5f, true);

            ModernTheme.StylePrimary(btnSubmit);
            ModernTheme.StyleSecondary(btnCancel);
        }
    }
}
