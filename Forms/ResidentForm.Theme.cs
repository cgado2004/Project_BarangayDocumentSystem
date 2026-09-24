using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Forms
{
    /// <summary>
    /// The THEME half of ResidentForm. Partial class, separate file, so Jonathan's
    /// designer file keeps compiling untouched. Applies the barangay brand:
    /// navy palette + Inter typography on every control, on load.
    /// </summary>
    public partial class ResidentForm
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
            pnlClassifications.BackColor = ModernTheme.Canvas;
            pnlActions.BackColor = ModernTheme.Canvas;

            foreach (var caption in new[] { lblFirstNameCaption, lblMiddleNameCaption, lblLastNameCaption,
                lblSuffixCaption, lblBirthCaption, lblGenderCaption, lblCivilStatusCaption,
                lblPurokCaption, lblAddressCaption, lblContactCaption, lblOccupationCaption,
                lblResidencyCaption, lblJobseekerUsedCaption })
                ModernTheme.StyleCaption(caption);
            ModernTheme.StyleSectionHeader(lblClassificationsCaption);

            foreach (Control field in new Control[] { txtFirstName, txtMiddleName, txtLastName,
                txtSuffix, txtPurok, txtAddress, txtContact, txtOccupation,
                dtpBirth, dtpResidency, cmbGender, cmbCivilStatus })
                ModernTheme.StyleText(field);

            foreach (var check in new[] { chkVoter, chkSenior, chkPwd, chkIndigent,
                chkStudent, chkSoloParent, chkJobseekerUsed })
                ModernTheme.StyleCheck(check);

            ModernTheme.StylePrimary(btnSaveResident);
            ModernTheme.StyleSecondary(btnCancel);
        }
    }
}
