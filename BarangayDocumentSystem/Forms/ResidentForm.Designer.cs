using System.Windows.Forms;
using System.Drawing;
using System;
using BarangayDocumentSystem.UIHelpers;
namespace BarangayDocumentSystem.Forms;

partial class ResidentForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        this.lblFirstName = new System.Windows.Forms.Label();
        this.txtFirstName = new System.Windows.Forms.TextBox();
        this.lblMiddleName = new System.Windows.Forms.Label();
        this.txtMiddleName = new System.Windows.Forms.TextBox();
        this.lblLastName = new System.Windows.Forms.Label();
        this.txtLastName = new System.Windows.Forms.TextBox();
        this.lblSuffix = new System.Windows.Forms.Label();
        this.txtSuffix = new System.Windows.Forms.TextBox();
        this.lblBirth = new System.Windows.Forms.Label();
        this.dtpBirth = new System.Windows.Forms.DateTimePicker();
        this.lblGender = new System.Windows.Forms.Label();
        this.radMale = new System.Windows.Forms.RadioButton();
        this.radFemale = new System.Windows.Forms.RadioButton();
        this.radOther = new System.Windows.Forms.RadioButton();
        this.lblCivilStatus = new System.Windows.Forms.Label();
        this.cmbCivilStatus = new System.Windows.Forms.ComboBox();
        this.lblPurok = new System.Windows.Forms.Label();
        this.cmbPurok = new System.Windows.Forms.ComboBox();
        this.lblAddress = new System.Windows.Forms.Label();
        this.txtAddress = new System.Windows.Forms.TextBox();
        this.lblContact = new System.Windows.Forms.Label();
        this.txtContact = new System.Windows.Forms.TextBox();
        this.lblOccupation = new System.Windows.Forms.Label();
        this.txtOccupation = new System.Windows.Forms.TextBox();
        this.lblResidency = new System.Windows.Forms.Label();
        this.dtpResidency = new System.Windows.Forms.DateTimePicker();
        this.chkVoter = new System.Windows.Forms.CheckBox();
        this.grpClassification = new System.Windows.Forms.GroupBox();
        this.chkSenior = new System.Windows.Forms.CheckBox();
        this.chkPwd = new System.Windows.Forms.CheckBox();
        this.chkIndigent = new System.Windows.Forms.CheckBox();
        this.chkStudent = new System.Windows.Forms.CheckBox();
        this.chkSoloParent = new System.Windows.Forms.CheckBox();
        this.lblNote = new System.Windows.Forms.Label();
        this.btnSave = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();
        this.grpClassification.SuspendLayout();
        this.SuspendLayout();

        var labelFont = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular);
        var inputFont = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular);
        var buttonFont = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);

        const int L = 36;
        const int LC = 180;
        const int LW = 300;
        const int R = 520;
        const int RC = 650;
        const int RW = 260;

        this.lblFirstName.AutoSize = true;
        this.lblFirstName.Font = labelFont;
        this.lblFirstName.Location = new System.Drawing.Point(L, 32);
        this.lblFirstName.Name = "lblFirstName";
        this.lblFirstName.Text = "First name";

        this.txtFirstName.Font = inputFont;
        this.txtFirstName.Location = new System.Drawing.Point(LC, 28);
        this.txtFirstName.MaxLength = 50;
        this.txtFirstName.Name = "txtFirstName";
        this.txtFirstName.Size = new System.Drawing.Size(LW, 30);
        this.txtFirstName.TabIndex = 1;

        this.lblLastName.AutoSize = true;
        this.lblLastName.Font = labelFont;
        this.lblLastName.Location = new System.Drawing.Point(L, 84);
        this.lblLastName.Name = "lblLastName";
        this.lblLastName.Text = "Last name";

        this.txtLastName.Font = inputFont;
        this.txtLastName.Location = new System.Drawing.Point(LC, 80);
        this.txtLastName.MaxLength = 50;
        this.txtLastName.Name = "txtLastName";
        this.txtLastName.Size = new System.Drawing.Size(LW, 30);
        this.txtLastName.TabIndex = 3;

        this.lblBirth.AutoSize = true;
        this.lblBirth.Font = labelFont;
        this.lblBirth.Location = new System.Drawing.Point(L, 136);
        this.lblBirth.Name = "lblBirth";
        this.lblBirth.Text = "Date of birth";

        this.dtpBirth.Font = inputFont;
        this.dtpBirth.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this.dtpBirth.Location = new System.Drawing.Point(LC, 132);
        this.dtpBirth.MinDate = new System.DateTime(1900, 1, 1);
        this.dtpBirth.Name = "dtpBirth";
        this.dtpBirth.Size = new System.Drawing.Size(LW, 30);
        this.dtpBirth.TabIndex = 5;

        this.lblCivilStatus.AutoSize = true;
        this.lblCivilStatus.Font = labelFont;
        this.lblCivilStatus.Location = new System.Drawing.Point(L, 188);
        this.lblCivilStatus.Name = "lblCivilStatus";
        this.lblCivilStatus.Text = "Civil status";

        this.cmbCivilStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbCivilStatus.Font = inputFont;
        this.cmbCivilStatus.Location = new System.Drawing.Point(LC, 184);
        this.cmbCivilStatus.Name = "cmbCivilStatus";
        this.cmbCivilStatus.Size = new System.Drawing.Size(LW, 30);
        this.cmbCivilStatus.TabIndex = 7;

        this.lblPurok.AutoSize = true;
        this.lblPurok.Font = labelFont;
        this.lblPurok.Location = new System.Drawing.Point(L, 240);
        this.lblPurok.Name = "lblPurok";
        this.lblPurok.Text = "Purok";

        this.cmbPurok.Font = inputFont;
        this.cmbPurok.Location = new System.Drawing.Point(LC, 236);
        this.cmbPurok.Name = "cmbPurok";
        this.cmbPurok.Size = new System.Drawing.Size(LW, 30);
        this.cmbPurok.TabIndex = 9;

        this.lblContact.AutoSize = true;
        this.lblContact.Font = labelFont;
        this.lblContact.Location = new System.Drawing.Point(L, 292);
        this.lblContact.Name = "lblContact";
        this.lblContact.Text = "Contact number";

        this.txtContact.Font = inputFont;
        this.txtContact.Location = new System.Drawing.Point(LC, 288);
        this.txtContact.MaxLength = 15;
        this.txtContact.Name = "txtContact";
        CueBanner.Set(this.txtContact, "09XXXXXXXXX");
        this.txtContact.Size = new System.Drawing.Size(LW, 30);
        this.txtContact.TabIndex = 11;
        this.txtContact.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtContact_KeyPress);

        this.lblResidency.AutoSize = true;
        this.lblResidency.Font = labelFont;
        this.lblResidency.Location = new System.Drawing.Point(L, 344);
        this.lblResidency.Name = "lblResidency";
        this.lblResidency.Text = "Resident since";

        this.dtpResidency.Font = inputFont;
        this.dtpResidency.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this.dtpResidency.Location = new System.Drawing.Point(LC, 340);
        this.dtpResidency.MinDate = new System.DateTime(1900, 1, 1);
        this.dtpResidency.Name = "dtpResidency";
        this.dtpResidency.Size = new System.Drawing.Size(LW, 30);
        this.dtpResidency.TabIndex = 13;
        this.lblMiddleName.AutoSize = true;
        this.lblMiddleName.Font = labelFont;
        this.lblMiddleName.Location = new System.Drawing.Point(R, 32);
        this.lblMiddleName.Name = "lblMiddleName";
        this.lblMiddleName.Text = "Middle name";

        this.txtMiddleName.Font = inputFont;
        this.txtMiddleName.Location = new System.Drawing.Point(RC, 28);
        this.txtMiddleName.MaxLength = 50;
        this.txtMiddleName.Name = "txtMiddleName";
        this.txtMiddleName.Size = new System.Drawing.Size(RW, 30);
        this.txtMiddleName.TabIndex = 2;

        this.lblSuffix.AutoSize = true;
        this.lblSuffix.Font = labelFont;
        this.lblSuffix.Location = new System.Drawing.Point(R, 84);
        this.lblSuffix.Name = "lblSuffix";
        this.lblSuffix.Text = "Suffix";

        this.txtSuffix.Font = inputFont;
        this.txtSuffix.Location = new System.Drawing.Point(RC, 80);
        this.txtSuffix.MaxLength = 10;
        this.txtSuffix.Name = "txtSuffix";
        CueBanner.Set(this.txtSuffix, "Jr., Sr., III");
        this.txtSuffix.Size = new System.Drawing.Size(RW, 30);
        this.txtSuffix.TabIndex = 4;

        this.lblGender.AutoSize = true;
        this.lblGender.Font = labelFont;
        this.lblGender.Location = new System.Drawing.Point(R, 136);
        this.lblGender.Name = "lblGender";
        this.lblGender.Text = "Gender";

        this.radMale.AutoSize = true;
        this.radMale.Checked = true;
        this.radMale.Font = inputFont;
        this.radMale.Location = new System.Drawing.Point(RC, 136);
        this.radMale.Name = "radMale";
        this.radMale.Size = new System.Drawing.Size(65, 24);
        this.radMale.TabIndex = 0;
        this.radMale.TabStop = true;
        this.radMale.Text = "Male";
        this.radMale.UseVisualStyleBackColor = true;

        this.radFemale.AutoSize = true;
        this.radFemale.Font = inputFont;
        this.radFemale.Location = new System.Drawing.Point(RC + 72, 136);
        this.radFemale.Name = "radFemale";
        this.radFemale.Size = new System.Drawing.Size(82, 24);
        this.radFemale.TabIndex = 1;
        this.radFemale.Text = "Female";
        this.radFemale.UseVisualStyleBackColor = true;

        this.lblAddress.AutoSize = true;
        this.lblAddress.Font = labelFont;
        this.lblAddress.Location = new System.Drawing.Point(R, 204);
        this.lblAddress.Name = "lblAddress";
        this.lblAddress.Text = "Address";

        this.txtAddress.Font = inputFont;
        this.txtAddress.Location = new System.Drawing.Point(RC, 200);
        this.txtAddress.MaxLength = 150;
        this.txtAddress.Multiline = true;
        this.txtAddress.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtAddress.Name = "txtAddress";
        CueBanner.Set(this.txtAddress, "house no. and street");
        this.txtAddress.Size = new System.Drawing.Size(RW, 90);
        this.txtAddress.TabIndex = 6;

        this.lblOccupation.AutoSize = true;
        this.lblOccupation.Font = labelFont;
        this.lblOccupation.Location = new System.Drawing.Point(R, 308);
        this.lblOccupation.Name = "lblOccupation";
        this.lblOccupation.Text = "Occupation";

        this.txtOccupation.Font = inputFont;
        this.txtOccupation.Location = new System.Drawing.Point(RC, 304);
        this.txtOccupation.MaxLength = 60;
        this.txtOccupation.Name = "txtOccupation";
        this.txtOccupation.Size = new System.Drawing.Size(RW, 30);
        this.txtOccupation.TabIndex = 8;

        this.chkVoter.AutoSize = true;
        this.chkVoter.Font = inputFont;
        this.chkVoter.Location = new System.Drawing.Point(R, 348);
        this.chkVoter.Name = "chkVoter";
        this.chkVoter.Size = new System.Drawing.Size(160, 24);
        this.chkVoter.TabIndex = 10;
        this.chkVoter.Text = "Registered voter";
        this.chkVoter.UseVisualStyleBackColor = true;
        this.grpClassification.Controls.Add(this.chkSoloParent);
        this.grpClassification.Controls.Add(this.chkStudent);
        this.grpClassification.Controls.Add(this.chkIndigent);
        this.grpClassification.Controls.Add(this.chkPwd);
        this.grpClassification.Controls.Add(this.chkSenior);
        this.grpClassification.Font = labelFont;
        this.grpClassification.Location = new System.Drawing.Point(36, 410);
        this.grpClassification.Name = "grpClassification";
        this.grpClassification.Size = new System.Drawing.Size(874, 106);
        this.grpClassification.TabIndex = 14;
        this.grpClassification.TabStop = false;
        this.grpClassification.Text = "Classification (affects document fees)";

        this.chkSenior.AutoSize = true;
        this.chkSenior.Font = inputFont;
        this.chkSenior.Location = new System.Drawing.Point(20, 30);
        this.chkSenior.Name = "chkSenior";
        this.chkSenior.Size = new System.Drawing.Size(200, 24);
        this.chkSenior.TabIndex = 0;
        this.chkSenior.Text = "Senior Citizen (RA 9994)";
        this.chkSenior.UseVisualStyleBackColor = true;

        this.chkPwd.AutoSize = true;
        this.chkPwd.Font = inputFont;
        this.chkPwd.Location = new System.Drawing.Point(260, 30);
        this.chkPwd.Name = "chkPwd";
        this.chkPwd.Size = new System.Drawing.Size(170, 24);
        this.chkPwd.TabIndex = 1;
        this.chkPwd.Text = "PWD (RA 10754)";
        this.chkPwd.UseVisualStyleBackColor = true;

        this.chkIndigent.AutoSize = true;
        this.chkIndigent.Font = inputFont;
        this.chkIndigent.Location = new System.Drawing.Point(470, 30);
        this.chkIndigent.Name = "chkIndigent";
        this.chkIndigent.Size = new System.Drawing.Size(100, 24);
        this.chkIndigent.TabIndex = 2;
        this.chkIndigent.Text = "Indigent";
        this.chkIndigent.UseVisualStyleBackColor = true;

        this.chkStudent.AutoSize = true;
        this.chkStudent.Font = inputFont;
        this.chkStudent.Location = new System.Drawing.Point(20, 64);
        this.chkStudent.Name = "chkStudent";
        this.chkStudent.Size = new System.Drawing.Size(90, 24);
        this.chkStudent.TabIndex = 3;
        this.chkStudent.Text = "Student";
        this.chkStudent.UseVisualStyleBackColor = true;

        this.chkSoloParent.AutoSize = true;
        this.chkSoloParent.Font = inputFont;
        this.chkSoloParent.Location = new System.Drawing.Point(260, 64);
        this.chkSoloParent.Name = "chkSoloParent";
        this.chkSoloParent.Size = new System.Drawing.Size(120, 24);
        this.chkSoloParent.TabIndex = 4;
        this.chkSoloParent.Text = "Solo Parent";
        this.chkSoloParent.UseVisualStyleBackColor = true;
        this.lblNote.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Italic);
        this.lblNote.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
        this.lblNote.Location = new System.Drawing.Point(36, 528);
        this.lblNote.Name = "lblNote";
        this.lblNote.Size = new System.Drawing.Size(874, 46);
        this.lblNote.TabIndex = 15;
        this.lblNote.Text = "\"Resident since\" drives the six month residency test required by RA 11261 " +
                            "for a first-time jobseeker certificate.";

        this.btnSave.BackColor = System.Drawing.Color.FromArgb(11, 37, 69);
        this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnSave.Font = buttonFont;
        this.btnSave.ForeColor = System.Drawing.Color.White;
        this.btnSave.Location = new System.Drawing.Point(690, 586);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(110, 42);
        this.btnSave.TabIndex = 16;
        this.btnSave.Text = "Save";
        this.btnSave.UseVisualStyleBackColor = false;
        this.btnSave.FlatAppearance.BorderSize = 0;
        this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
        this.btnCancel.BackColor = System.Drawing.Color.White;
        this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnCancel.Font = buttonFont;
        this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(31, 39, 51);
        this.btnCancel.Location = new System.Drawing.Point(810, 586);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(100, 42);
        this.btnCancel.TabIndex = 17;
        this.btnCancel.Text = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = false;
        this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 191, 168);
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
        this.AcceptButton = this.btnSave;
        this.CancelButton = this.btnCancel;
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.Color.FromArgb(243, 239, 231);
        this.ClientSize = new System.Drawing.Size(946, 660);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnSave);
        this.Controls.Add(this.lblNote);
        this.Controls.Add(this.grpClassification);
        this.Controls.Add(this.chkVoter);
        this.Controls.Add(this.txtOccupation);
        this.Controls.Add(this.lblOccupation);
        this.Controls.Add(this.txtAddress);
        this.Controls.Add(this.lblAddress);
        this.Controls.Add(this.radOther);
        this.Controls.Add(this.radFemale);
        this.Controls.Add(this.radMale);
        this.Controls.Add(this.lblGender);
        this.Controls.Add(this.txtSuffix);
        this.Controls.Add(this.lblSuffix);
        this.Controls.Add(this.txtMiddleName);
        this.Controls.Add(this.lblMiddleName);
        this.Controls.Add(this.dtpResidency);
        this.Controls.Add(this.lblResidency);
        this.Controls.Add(this.txtContact);
        this.Controls.Add(this.lblContact);
        this.Controls.Add(this.cmbPurok);
        this.Controls.Add(this.lblPurok);
        this.Controls.Add(this.cmbCivilStatus);
        this.Controls.Add(this.lblCivilStatus);
        this.Controls.Add(this.dtpBirth);
        this.Controls.Add(this.lblBirth);
        this.Controls.Add(this.txtLastName);
        this.Controls.Add(this.lblLastName);
        this.Controls.Add(this.txtFirstName);
        this.Controls.Add(this.lblFirstName);
        this.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Regular);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ResidentForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Register Resident";
        this.Load += new System.EventHandler(this.ResidentForm_Load);
        this.grpClassification.ResumeLayout(false);
        this.grpClassification.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label lblFirstName;
    private System.Windows.Forms.TextBox txtFirstName;
    private System.Windows.Forms.Label lblMiddleName;
    private System.Windows.Forms.TextBox txtMiddleName;
    private System.Windows.Forms.Label lblLastName;
    private System.Windows.Forms.TextBox txtLastName;
    private System.Windows.Forms.Label lblSuffix;
    private System.Windows.Forms.TextBox txtSuffix;
    private System.Windows.Forms.Label lblBirth;
    private System.Windows.Forms.DateTimePicker dtpBirth;
    private System.Windows.Forms.Label lblGender;
    private System.Windows.Forms.RadioButton radMale;
    private System.Windows.Forms.RadioButton radFemale;
    private System.Windows.Forms.RadioButton radOther;
    private System.Windows.Forms.Label lblCivilStatus;
    private System.Windows.Forms.ComboBox cmbCivilStatus;
    private System.Windows.Forms.Label lblPurok;
    private System.Windows.Forms.ComboBox cmbPurok;
    private System.Windows.Forms.Label lblAddress;
    private System.Windows.Forms.TextBox txtAddress;
    private System.Windows.Forms.Label lblContact;
    private System.Windows.Forms.TextBox txtContact;
    private System.Windows.Forms.Label lblOccupation;
    private System.Windows.Forms.TextBox txtOccupation;
    private System.Windows.Forms.Label lblResidency;
    private System.Windows.Forms.DateTimePicker dtpResidency;
    private System.Windows.Forms.CheckBox chkVoter;
    private System.Windows.Forms.GroupBox grpClassification;
    private System.Windows.Forms.CheckBox chkSenior;
    private System.Windows.Forms.CheckBox chkPwd;
    private System.Windows.Forms.CheckBox chkIndigent;
    private System.Windows.Forms.CheckBox chkStudent;
    private System.Windows.Forms.CheckBox chkSoloParent;
    private System.Windows.Forms.Label lblNote;
    private System.Windows.Forms.Button btnSave;
    private System.Windows.Forms.Button btnCancel;
}