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
        this.grpGender = new System.Windows.Forms.GroupBox();
        this.radMale = new System.Windows.Forms.RadioButton();
        this.radFemale = new System.Windows.Forms.RadioButton();
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
        this.grpGender.SuspendLayout();
        this.grpClassification.SuspendLayout();
        this.SuspendLayout();

        int L = 22, C = 150, W = 240;

        this.lblFirstName.AutoSize = true;
        this.lblFirstName.Location = new System.Drawing.Point(L, 24);
        this.lblFirstName.Name = "lblFirstName";
        this.lblFirstName.Size = new System.Drawing.Size(82, 20);
        this.lblFirstName.TabIndex = 0;
        this.lblFirstName.Text = "First name:";

        this.txtFirstName.Location = new System.Drawing.Point(C, 21);
        this.txtFirstName.MaxLength = 50;
        this.txtFirstName.Name = "txtFirstName";
        this.txtFirstName.Size = new System.Drawing.Size(W, 27);
        this.txtFirstName.TabIndex = 1;

        this.lblMiddleName.AutoSize = true;
        this.lblMiddleName.Location = new System.Drawing.Point(410, 24);
        this.lblMiddleName.Name = "lblMiddleName";
        this.lblMiddleName.Size = new System.Drawing.Size(95, 20);
        this.lblMiddleName.TabIndex = 2;
        this.lblMiddleName.Text = "Middle name:";

        this.txtMiddleName.Location = new System.Drawing.Point(512, 21);
        this.txtMiddleName.MaxLength = 50;
        this.txtMiddleName.Name = "txtMiddleName";
        this.txtMiddleName.Size = new System.Drawing.Size(180, 27);
        this.txtMiddleName.TabIndex = 3;

        this.lblLastName.AutoSize = true;
        this.lblLastName.Location = new System.Drawing.Point(L, 60);
        this.lblLastName.Name = "lblLastName";
        this.lblLastName.Size = new System.Drawing.Size(80, 20);
        this.lblLastName.TabIndex = 4;
        this.lblLastName.Text = "Last name:";

        this.txtLastName.Location = new System.Drawing.Point(C, 57);
        this.txtLastName.MaxLength = 50;
        this.txtLastName.Name = "txtLastName";
        this.txtLastName.Size = new System.Drawing.Size(W, 27);
        this.txtLastName.TabIndex = 5;

        this.lblSuffix.AutoSize = true;
        this.lblSuffix.Location = new System.Drawing.Point(410, 60);
        this.lblSuffix.Name = "lblSuffix";
        this.lblSuffix.Size = new System.Drawing.Size(52, 20);
        this.lblSuffix.TabIndex = 6;
        this.lblSuffix.Text = "Suffix:";

        this.txtSuffix.Location = new System.Drawing.Point(512, 57);
        this.txtSuffix.MaxLength = 10;
        this.txtSuffix.Name = "txtSuffix";
        CueBanner.Set(this.txtSuffix, "Jr., Sr., III");
        this.txtSuffix.Size = new System.Drawing.Size(180, 27);
        this.txtSuffix.TabIndex = 7;

        this.lblBirth.AutoSize = true;
        this.lblBirth.Location = new System.Drawing.Point(L, 96);
        this.lblBirth.Name = "lblBirth";
        this.lblBirth.Size = new System.Drawing.Size(93, 20);
        this.lblBirth.TabIndex = 8;
        this.lblBirth.Text = "Date of birth:";

        this.dtpBirth.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this.dtpBirth.Location = new System.Drawing.Point(C, 92);
        this.dtpBirth.MinDate = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
        this.dtpBirth.Name = "dtpBirth";
        this.dtpBirth.Size = new System.Drawing.Size(W, 27);
        this.dtpBirth.TabIndex = 9;

        this.grpGender.Controls.Add(this.radFemale);
        this.grpGender.Controls.Add(this.radMale);
        this.grpGender.Location = new System.Drawing.Point(410, 86);
        this.grpGender.Name = "grpGender";
        this.grpGender.Size = new System.Drawing.Size(282, 56);
        this.grpGender.TabIndex = 10;
        this.grpGender.TabStop = false;
        this.grpGender.Text = "Gender";

        this.radMale.AutoSize = true;
        this.radMale.Checked = true;
        this.radMale.Location = new System.Drawing.Point(18, 24);
        this.radMale.Name = "radMale";
        this.radMale.Size = new System.Drawing.Size(64, 24);
        this.radMale.TabIndex = 0;
        this.radMale.TabStop = true;
        this.radMale.Text = "Male";
        this.radMale.UseVisualStyleBackColor = true;

        this.radFemale.AutoSize = true;
        this.radFemale.Location = new System.Drawing.Point(110, 24);
        this.radFemale.Name = "radFemale";
        this.radFemale.Size = new System.Drawing.Size(83, 24);
        this.radFemale.TabIndex = 1;
        this.radFemale.Text = "Female";
        this.radFemale.UseVisualStyleBackColor = true;

        this.lblCivilStatus.AutoSize = true;
        this.lblCivilStatus.Location = new System.Drawing.Point(L, 132);
        this.lblCivilStatus.Name = "lblCivilStatus";
        this.lblCivilStatus.Size = new System.Drawing.Size(83, 20);
        this.lblCivilStatus.TabIndex = 11;
        this.lblCivilStatus.Text = "Civil status:";

        this.cmbCivilStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbCivilStatus.Location = new System.Drawing.Point(C, 129);
        this.cmbCivilStatus.Name = "cmbCivilStatus";
        this.cmbCivilStatus.Size = new System.Drawing.Size(W, 28);
        this.cmbCivilStatus.TabIndex = 12;

        this.lblPurok.AutoSize = true;
        this.lblPurok.Location = new System.Drawing.Point(L, 168);
        this.lblPurok.Name = "lblPurok";
        this.lblPurok.Size = new System.Drawing.Size(50, 20);
        this.lblPurok.TabIndex = 13;
        this.lblPurok.Text = "Purok:";

        this.cmbPurok.Location = new System.Drawing.Point(C, 165);
        this.cmbPurok.Name = "cmbPurok";
        this.cmbPurok.Size = new System.Drawing.Size(W, 28);
        this.cmbPurok.TabIndex = 14;

        this.lblAddress.AutoSize = true;
        this.lblAddress.Location = new System.Drawing.Point(410, 168);
        this.lblAddress.Name = "lblAddress";
        this.lblAddress.Size = new System.Drawing.Size(65, 20);
        this.lblAddress.TabIndex = 15;
        this.lblAddress.Text = "Address:";

        this.txtAddress.Location = new System.Drawing.Point(512, 165);
        this.txtAddress.MaxLength = 150;
        this.txtAddress.Name = "txtAddress";
        CueBanner.Set(this.txtAddress, "house no. and street");
        this.txtAddress.Size = new System.Drawing.Size(180, 27);
        this.txtAddress.TabIndex = 16;

        this.lblContact.AutoSize = true;
        this.lblContact.Location = new System.Drawing.Point(L, 204);
        this.lblContact.Name = "lblContact";
        this.lblContact.Size = new System.Drawing.Size(119, 20);
        this.lblContact.TabIndex = 17;
        this.lblContact.Text = "Contact number:";

        this.txtContact.Location = new System.Drawing.Point(C, 201);
        this.txtContact.MaxLength = 15;
        this.txtContact.Name = "txtContact";
        CueBanner.Set(this.txtContact, "09XXXXXXXXX");
        this.txtContact.Size = new System.Drawing.Size(W, 27);
        this.txtContact.TabIndex = 18;
        this.txtContact.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtContact_KeyPress);

        this.lblOccupation.AutoSize = true;
        this.lblOccupation.Location = new System.Drawing.Point(410, 204);
        this.lblOccupation.Name = "lblOccupation";
        this.lblOccupation.Size = new System.Drawing.Size(88, 20);
        this.lblOccupation.TabIndex = 19;
        this.lblOccupation.Text = "Occupation:";

        this.txtOccupation.Location = new System.Drawing.Point(512, 201);
        this.txtOccupation.MaxLength = 60;
        this.txtOccupation.Name = "txtOccupation";
        this.txtOccupation.Size = new System.Drawing.Size(180, 27);
        this.txtOccupation.TabIndex = 20;

        this.lblResidency.AutoSize = true;
        this.lblResidency.Location = new System.Drawing.Point(L, 240);
        this.lblResidency.Name = "lblResidency";
        this.lblResidency.Size = new System.Drawing.Size(110, 20);
        this.lblResidency.TabIndex = 21;
        this.lblResidency.Text = "Resident since:";

        this.dtpResidency.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this.dtpResidency.Location = new System.Drawing.Point(C, 236);
        this.dtpResidency.MinDate = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
        this.dtpResidency.Name = "dtpResidency";
        this.dtpResidency.Size = new System.Drawing.Size(W, 27);
        this.dtpResidency.TabIndex = 22;

        this.chkVoter.AutoSize = true;
        this.chkVoter.Location = new System.Drawing.Point(412, 239);
        this.chkVoter.Name = "chkVoter";
        this.chkVoter.Size = new System.Drawing.Size(153, 24);
        this.chkVoter.TabIndex = 23;
        this.chkVoter.Text = "Registered voter";
        this.chkVoter.UseVisualStyleBackColor = true;

        this.grpClassification.Controls.Add(this.chkSoloParent);
        this.grpClassification.Controls.Add(this.chkStudent);
        this.grpClassification.Controls.Add(this.chkIndigent);
        this.grpClassification.Controls.Add(this.chkPwd);
        this.grpClassification.Controls.Add(this.chkSenior);
        this.grpClassification.Location = new System.Drawing.Point(22, 276);
        this.grpClassification.Name = "grpClassification";
        this.grpClassification.Size = new System.Drawing.Size(670, 92);
        this.grpClassification.TabIndex = 24;
        this.grpClassification.TabStop = false;
        this.grpClassification.Text = "Classification (affects document fees)";

        // CheckBoxes, not RadioButtons — a resident can hold several at once.
        this.chkSenior.AutoSize = true;
        this.chkSenior.Location = new System.Drawing.Point(18, 26);
        this.chkSenior.Name = "chkSenior";
        this.chkSenior.Size = new System.Drawing.Size(178, 24);
        this.chkSenior.TabIndex = 0;
        this.chkSenior.Text = "Senior Citizen (RA 9994)";
        this.chkSenior.UseVisualStyleBackColor = true;

        this.chkPwd.AutoSize = true;
        this.chkPwd.Location = new System.Drawing.Point(230, 26);
        this.chkPwd.Name = "chkPwd";
        this.chkPwd.Size = new System.Drawing.Size(139, 24);
        this.chkPwd.TabIndex = 1;
        this.chkPwd.Text = "PWD (RA 10754)";
        this.chkPwd.UseVisualStyleBackColor = true;

        this.chkIndigent.AutoSize = true;
        this.chkIndigent.Location = new System.Drawing.Point(410, 26);
        this.chkIndigent.Name = "chkIndigent";
        this.chkIndigent.Size = new System.Drawing.Size(84, 24);
        this.chkIndigent.TabIndex = 2;
        this.chkIndigent.Text = "Indigent";
        this.chkIndigent.UseVisualStyleBackColor = true;

        this.chkStudent.AutoSize = true;
        this.chkStudent.Location = new System.Drawing.Point(18, 56);
        this.chkStudent.Name = "chkStudent";
        this.chkStudent.Size = new System.Drawing.Size(80, 24);
        this.chkStudent.TabIndex = 3;
        this.chkStudent.Text = "Student";
        this.chkStudent.UseVisualStyleBackColor = true;

        this.chkSoloParent.AutoSize = true;
        this.chkSoloParent.Location = new System.Drawing.Point(230, 56);
        this.chkSoloParent.Name = "chkSoloParent";
        this.chkSoloParent.Size = new System.Drawing.Size(104, 24);
        this.chkSoloParent.TabIndex = 4;
        this.chkSoloParent.Text = "Solo Parent";
        this.chkSoloParent.UseVisualStyleBackColor = true;

        this.lblNote.ForeColor = System.Drawing.Color.FromArgb(90, 107, 130);
        this.lblNote.Location = new System.Drawing.Point(22, 376);
        this.lblNote.Name = "lblNote";
        this.lblNote.Size = new System.Drawing.Size(670, 44);
        this.lblNote.TabIndex = 25;
        this.lblNote.Text = "\"Resident since\" drives the six-month residency test required by RA 11261 " +
                            "for a first-time jobseeker certificate.";

        this.btnSave.BackColor = System.Drawing.Color.FromArgb(21, 71, 52);
        this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
        this.btnSave.ForeColor = System.Drawing.Color.White;
        this.btnSave.Location = new System.Drawing.Point(462, 428);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(110, 40);
        this.btnSave.TabIndex = 26;
        this.btnSave.Text = "Save";
        this.btnSave.UseVisualStyleBackColor = false;
        this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(582, 428);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(110, 40);
        this.btnCancel.TabIndex = 27;
        this.btnCancel.Text = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = true;
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

        this.AcceptButton = this.btnSave;
        this.CancelButton = this.btnCancel;
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(716, 488);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnSave);
        this.Controls.Add(this.lblNote);
        this.Controls.Add(this.grpClassification);
        this.Controls.Add(this.chkVoter);
        this.Controls.Add(this.dtpResidency);
        this.Controls.Add(this.lblResidency);
        this.Controls.Add(this.txtOccupation);
        this.Controls.Add(this.lblOccupation);
        this.Controls.Add(this.txtContact);
        this.Controls.Add(this.lblContact);
        this.Controls.Add(this.txtAddress);
        this.Controls.Add(this.lblAddress);
        this.Controls.Add(this.cmbPurok);
        this.Controls.Add(this.lblPurok);
        this.Controls.Add(this.cmbCivilStatus);
        this.Controls.Add(this.lblCivilStatus);
        this.Controls.Add(this.grpGender);
        this.Controls.Add(this.dtpBirth);
        this.Controls.Add(this.lblBirth);
        this.Controls.Add(this.txtSuffix);
        this.Controls.Add(this.lblSuffix);
        this.Controls.Add(this.txtLastName);
        this.Controls.Add(this.lblLastName);
        this.Controls.Add(this.txtMiddleName);
        this.Controls.Add(this.lblMiddleName);
        this.Controls.Add(this.txtFirstName);
        this.Controls.Add(this.lblFirstName);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ResidentForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Register Resident";
        this.Load += new System.EventHandler(this.ResidentForm_Load);
        this.grpGender.ResumeLayout(false);
        this.grpGender.PerformLayout();
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
    private System.Windows.Forms.GroupBox grpGender;
    private System.Windows.Forms.RadioButton radMale;
    private System.Windows.Forms.RadioButton radFemale;
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
