using System;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Forms
{
    public partial class ResidentForm : Form
    {
        private readonly ResidentService service;
        private readonly int residentId;
        private readonly int version;
        private readonly bool benefitAlreadyUsed;

        public ResidentForm()
        {
            InitializeComponent();
        }

        public ResidentForm(ResidentService service, Resident existing = null) : this()
        {
            this.service = service;
            var resident = existing ?? new Resident();
            residentId = resident.ResidentId;
            version = resident.Version;
            benefitAlreadyUsed = resident.HasUsedJobseekerBenefit;
            Text = existing == null ? "Register resident" : "Edit resident";
            txtFirstName.Text = resident.FirstName;
            txtMiddleName.Text = resident.MiddleName;
            txtLastName.Text = resident.LastName;
            txtSuffix.Text = resident.Suffix;
            dtpBirth.Value = resident.DateOfBirth;
            dtpResidency.Value = resident.DateOfResidency;
            cmbGender.DataSource = Enum.GetValues(typeof(Gender));
            cmbGender.SelectedItem = resident.Gender;
            cmbCivilStatus.DataSource = Enum.GetValues(typeof(CivilStatus));
            cmbCivilStatus.SelectedItem = resident.CivilStatus;
            txtPurok.Text = resident.Purok;
            txtAddress.Text = resident.Address;
            txtContact.Text = resident.ContactNumber;
            txtOccupation.Text = resident.Occupation;
            chkVoter.Checked = resident.IsRegisteredVoter;
            chkSenior.Checked = resident.IsSeniorCitizen;
            chkPwd.Checked = resident.IsPersonWithDisability;
            chkIndigent.Checked = resident.IsIndigent;
            chkStudent.Checked = resident.IsStudent;
            chkSoloParent.Checked = resident.IsSoloParent;
            chkJobseekerUsed.Checked = benefitAlreadyUsed;
            chkJobseekerUsed.Enabled = !benefitAlreadyUsed;
        }

        private void CloseDialog(object sender, EventArgs e) { Close(); }

        private void SaveResident(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                if (service == null) return;
                if (cmbGender.SelectedItem == null || cmbCivilStatus.SelectedItem == null)
                    throw new ArgumentException("Select a gender and civil status.");
                var resident = new Resident
                {
                    ResidentId = residentId, Version = version, FirstName = txtFirstName.Text, MiddleName = txtMiddleName.Text,
                    LastName = txtLastName.Text, Suffix = txtSuffix.Text, DateOfBirth = dtpBirth.Value.Date,
                    DateOfResidency = dtpResidency.Value.Date, Gender = (Gender)cmbGender.SelectedItem,
                    CivilStatus = (CivilStatus)cmbCivilStatus.SelectedItem, Purok = txtPurok.Text,
                    Address = txtAddress.Text, ContactNumber = txtContact.Text, Occupation = txtOccupation.Text,
                    IsRegisteredVoter = chkVoter.Checked, IsSeniorCitizen = chkSenior.Checked,
                    IsPersonWithDisability = chkPwd.Checked, IsIndigent = chkIndigent.Checked,
                    IsStudent = chkStudent.Checked, IsSoloParent = chkSoloParent.Checked,
                    HasUsedJobseekerBenefit = benefitAlreadyUsed || chkJobseekerUsed.Checked
                };
                service.Save(resident);
                DialogResult = DialogResult.OK;
                Close();
            });
        }

        private void ContactKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && (e.KeyChar < '0' || e.KeyChar > '9')) e.Handled = true;
        }
    }
}
