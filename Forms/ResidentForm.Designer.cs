using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Forms
{
    partial class ResidentForm
    {
        private TextBox txtFirstName, txtMiddleName, txtLastName, txtSuffix;
        private TextBox txtPurok, txtAddress, txtContact, txtOccupation;
        private DateTimePicker dtpBirth, dtpResidency;
        private ComboBox cmbGender, cmbCivilStatus;
        private CheckBox chkVoter, chkSenior, chkPwd, chkIndigent, chkStudent, chkSoloParent, chkJobseekerUsed;

        private void InitializeComponent()
        {
            UiLayout.PrepareDialog(this, "Resident", 680, 700);
            txtFirstName = new TextBox { Name = "txtFirstName", MaxLength = 80 };
            txtMiddleName = new TextBox { MaxLength = 80 };
            txtLastName = new TextBox { Name = "txtLastName", MaxLength = 80 };
            txtSuffix = new TextBox { MaxLength = 20 };
            txtPurok = new TextBox { MaxLength = 60 };
            txtAddress = new TextBox { MaxLength = 250 };
            txtContact = new TextBox { MaxLength = 15 };
            txtContact.KeyPress += ContactKeyPress;
            txtOccupation = new TextBox { MaxLength = 100 };
            dtpBirth = new DateTimePicker { Format = DateTimePickerFormat.Short };
            dtpResidency = new DateTimePicker { Format = DateTimePickerFormat.Short };
            cmbGender = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCivilStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            chkVoter = new CheckBox { Text = "Registered voter", AutoSize = true };
            chkSenior = new CheckBox { Text = "Senior citizen", AutoSize = true };
            chkPwd = new CheckBox { Text = "Person with disability", AutoSize = true };
            chkIndigent = new CheckBox { Text = "Indigent", AutoSize = true };
            chkStudent = new CheckBox { Text = "Student", AutoSize = true };
            chkSoloParent = new CheckBox { Text = "Solo parent", AutoSize = true };
            chkJobseekerUsed = new CheckBox { Text = "Previously used first-time jobseeker benefit", AutoSize = true };
            var classifications = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
            classifications.Controls.AddRange(new Control[] { chkSenior, chkPwd, chkIndigent, chkStudent, chkSoloParent });
            var table = UiLayout.Fields();
            UiLayout.Field(table, "First name *", txtFirstName);
            UiLayout.Field(table, "Middle name", txtMiddleName);
            UiLayout.Field(table, "Last name *", txtLastName);
            UiLayout.Field(table, "Suffix", txtSuffix);
            UiLayout.Field(table, "Date of birth *", dtpBirth);
            UiLayout.Field(table, "Gender *", cmbGender);
            UiLayout.Field(table, "Civil status *", cmbCivilStatus);
            UiLayout.Field(table, "Purok *", txtPurok);
            UiLayout.Field(table, "Address *", txtAddress);
            UiLayout.Field(table, "Contact number *", txtContact);
            UiLayout.Field(table, "Occupation", txtOccupation);
            UiLayout.Field(table, "Resident since *", dtpResidency);
            UiLayout.Field(table, "Voter status", chkVoter);
            UiLayout.Field(table, "Classifications", classifications);
            UiLayout.Field(table, "Jobseeker history", chkJobseekerUsed);
            var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            body.Controls.Add(table);
            var actions = UiLayout.Actions();
            actions.Dock = DockStyle.Bottom;
            actions.Padding = new Padding(12);
            var save = UiLayout.Button("Save resident", SaveResident, true);
            save.Name = "btnSaveResident";
            var cancel = UiLayout.Button("Cancel", (sender, args) => Close());
            cancel.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(save);
            actions.Controls.Add(cancel);
            AcceptButton = save;
            CancelButton = cancel;
            Controls.Add(body);
            Controls.Add(actions);
        }
    }
}
