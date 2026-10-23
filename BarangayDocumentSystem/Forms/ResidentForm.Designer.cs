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

    // Layout: two tabs. "Personal details" holds the resident's own information in
    // two evenly spaced columns plus the classification box. "Dependents" holds
    // the list of people who depend on the resident. Save and Cancel stay visible
    // below the tabs.
    private void InitializeComponent()
    {
        this.tabs = new System.Windows.Forms.TabControl();
        this.tabPersonal = new System.Windows.Forms.TabPage();
        this.tabDependents = new System.Windows.Forms.TabPage();
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
        this.lblCivilStatus = new System.Windows.Forms.Label();
        this.cmbCivilStatus = new System.Windows.Forms.ComboBox();
        this.lblPurok = new System.Windows.Forms.Label();
        this.cmbPurok = new System.Windows.Forms.ComboBox();
        this.lblHouseNumber = new System.Windows.Forms.Label();
        this.txtHouseNumber = new System.Windows.Forms.TextBox();
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
        this.chkSoloParent = new System.Windows.Forms.CheckBox();
        this.lblNote = new System.Windows.Forms.Label();
        this.dgvDependents = new System.Windows.Forms.DataGridView();
        this.btnAddDependent = new System.Windows.Forms.Button();
        this.btnEditDependent = new System.Windows.Forms.Button();
        this.btnRemoveDependent = new System.Windows.Forms.Button();
        this.lblDependentsHint = new System.Windows.Forms.Label();
        this.btnSave = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();
        this.tabs.SuspendLayout();
        this.tabPersonal.SuspendLayout();
        this.tabDependents.SuspendLayout();
        this.grpClassification.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.dgvDependents)).BeginInit();
        this.SuspendLayout();

        var labelFont = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular);
        var inputFont = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular);
        var buttonFont = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
        var navy = System.Drawing.Color.FromArgb(11, 37, 69);
        var line = System.Drawing.Color.FromArgb(200, 191, 168);
        var ink = System.Drawing.Color.FromArgb(31, 39, 51);

        // Column positions inside the "Personal details" tab, and the row pitch.
        const int L = 32;     // left labels
        const int LC = 190;   // left inputs
        const int LW = 260;
        const int R = 500;    // right labels
        const int RC = 640;   // right inputs
        const int RW = 250;
        const int Y0 = 24;    // first row
        const int P = 50;     // distance between rows

        // --- small local helpers so every row is built the same way ---------
        void Caption(Label label, string name, string text, int x, int row)
        {
            label.AutoSize = true;
            label.Font = labelFont;
            label.Location = new System.Drawing.Point(x, Y0 + P * row + 4);
            label.Name = name;
            label.Text = text;
        }
        void Field(Control box, string name, int x, int row, int width, int tab)
        {
            box.Font = inputFont;
            box.Location = new System.Drawing.Point(x, Y0 + P * row);
            box.Name = name;
            box.Size = new System.Drawing.Size(width, 30);
            box.TabIndex = tab;
        }

        // ---- left column ----
        Caption(this.lblFirstName, "lblFirstName", "First name", L, 0);
        Field(this.txtFirstName, "txtFirstName", LC, 0, LW, 1);
        this.txtFirstName.MaxLength = 50;

        Caption(this.lblLastName, "lblLastName", "Last name", L, 1);
        Field(this.txtLastName, "txtLastName", LC, 1, LW, 3);
        this.txtLastName.MaxLength = 50;

        Caption(this.lblBirth, "lblBirth", "Date of birth", L, 2);
        Field(this.dtpBirth, "dtpBirth", LC, 2, LW, 5);
        this.dtpBirth.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this.dtpBirth.MinDate = new System.DateTime(1900, 1, 1);

        Caption(this.lblCivilStatus, "lblCivilStatus", "Civil status", L, 3);
        Field(this.cmbCivilStatus, "cmbCivilStatus", LC, 3, LW, 7);
        this.cmbCivilStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

        Caption(this.lblPurok, "lblPurok", "Purok", L, 4);
        Field(this.cmbPurok, "cmbPurok", LC, 4, LW, 9);

        Caption(this.lblContact, "lblContact", "Contact number", L, 5);
        Field(this.txtContact, "txtContact", LC, 5, LW, 11);
        this.txtContact.MaxLength = 15;
        CueBanner.Set(this.txtContact, "09XXXXXXXXX");
        this.txtContact.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtContact_KeyPress);

        // ---- right column ----
        Caption(this.lblMiddleName, "lblMiddleName", "Middle name", R, 0);
        Field(this.txtMiddleName, "txtMiddleName", RC, 0, RW, 2);
        this.txtMiddleName.MaxLength = 50;

        Caption(this.lblSuffix, "lblSuffix", "Suffix", R, 1);
        Field(this.txtSuffix, "txtSuffix", RC, 1, RW, 4);
        this.txtSuffix.MaxLength = 10;
        CueBanner.Set(this.txtSuffix, "Jr., Sr., III");

        Caption(this.lblGender, "lblGender", "Gender", R, 2);

        this.radMale.AutoSize = true;
        this.radMale.Checked = true;
        this.radMale.Font = inputFont;
        this.radMale.Location = new System.Drawing.Point(RC, Y0 + P * 2 + 3);
        this.radMale.Name = "radMale";
        this.radMale.TabIndex = 0;
        this.radMale.TabStop = true;
        this.radMale.Text = "Male";
        this.radMale.UseVisualStyleBackColor = true;

        this.radFemale.AutoSize = true;
        this.radFemale.Font = inputFont;
        this.radFemale.Location = new System.Drawing.Point(RC + 90, Y0 + P * 2 + 3);
        this.radFemale.Name = "radFemale";
        this.radFemale.TabIndex = 1;
        this.radFemale.Text = "Female";
        this.radFemale.UseVisualStyleBackColor = true;

        Caption(this.lblOccupation, "lblOccupation", "Occupation", R, 3);
        Field(this.txtOccupation, "txtOccupation", RC, 3, RW, 8);
        this.txtOccupation.MaxLength = 60;

        Caption(this.lblHouseNumber, "lblHouseNumber", "House number", R, 4);
        Field(this.txtHouseNumber, "txtHouseNumber", RC, 4, RW, 6);
        this.txtHouseNumber.MaxLength = 30;
        CueBanner.Set(this.txtHouseNumber, "e.g. 123");

        Caption(this.lblResidency, "lblResidency", "Resident since", R, 5);
        Field(this.dtpResidency, "dtpResidency", RC, 5, RW, 12);
        this.dtpResidency.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this.dtpResidency.MinDate = new System.DateTime(1900, 1, 1);

        // ---- voter, classification, note ----
        this.chkVoter.AutoSize = true;
        this.chkVoter.Font = inputFont;
        this.chkVoter.Location = new System.Drawing.Point(L, Y0 + P * 6 + 2);
        this.chkVoter.Name = "chkVoter";
        this.chkVoter.TabIndex = 13;
        this.chkVoter.Text = "Registered voter";
        this.chkVoter.UseVisualStyleBackColor = true;

        this.grpClassification.Controls.Add(this.chkSoloParent);
        this.grpClassification.Controls.Add(this.chkIndigent);
        this.grpClassification.Controls.Add(this.chkPwd);
        this.grpClassification.Controls.Add(this.chkSenior);
        this.grpClassification.Font = labelFont;
        this.grpClassification.Location = new System.Drawing.Point(L, Y0 + P * 6 + 44);
        this.grpClassification.Name = "grpClassification";
        this.grpClassification.Size = new System.Drawing.Size(842, 84);
        this.grpClassification.TabIndex = 14;
        this.grpClassification.TabStop = false;
        this.grpClassification.Text = "Classification (affects document fees)";

        void Check(CheckBox box, string name, string text, int x, int tab)
        {
            box.AutoSize = true;
            box.Font = inputFont;
            box.Location = new System.Drawing.Point(x, 38);
            box.Name = name;
            box.TabIndex = tab;
            box.Text = text;
            box.UseVisualStyleBackColor = true;
        }
        Check(this.chkSenior, "chkSenior", "Senior Citizen (RA 9994)", 24, 0);
        Check(this.chkPwd, "chkPwd", "PWD (RA 10754)", 280, 1);
        Check(this.chkIndigent, "chkIndigent", "Indigent", 490, 2);
        Check(this.chkSoloParent, "chkSoloParent", "Solo Parent", 660, 3);

        this.lblNote.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Italic);
        this.lblNote.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
        this.lblNote.Location = new System.Drawing.Point(L, Y0 + P * 6 + 142);
        this.lblNote.Name = "lblNote";
        this.lblNote.Size = new System.Drawing.Size(842, 40);
        this.lblNote.TabIndex = 15;
        this.lblNote.Text = "\"Resident since\" drives the six month residency test required by RA 11261 " +
                            "for a first-time jobseeker certificate.";

        // ---- dependents tab ----
        this.dgvDependents.Location = new System.Drawing.Point(24, 24);
        this.dgvDependents.Name = "dgvDependents";
        this.dgvDependents.Size = new System.Drawing.Size(716, 380);
        this.dgvDependents.TabIndex = 0;
        this.dgvDependents.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvDependents_CellDoubleClick);

        void SideButton(Button button, string name, string text, int top, bool primary, int tab)
        {
            button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button.Font = buttonFont;
            button.Location = new System.Drawing.Point(760, top);
            button.Name = name;
            button.Size = new System.Drawing.Size(114, 42);
            button.TabIndex = tab;
            button.Text = text;
            button.UseVisualStyleBackColor = false;
            button.BackColor = primary ? navy : System.Drawing.Color.White;
            button.ForeColor = primary ? System.Drawing.Color.White : ink;
            if (primary) button.FlatAppearance.BorderSize = 0;
            else button.FlatAppearance.BorderColor = line;
        }
        SideButton(this.btnAddDependent, "btnAddDependent", "Add", 24, true, 1);
        SideButton(this.btnEditDependent, "btnEditDependent", "Edit", 78, false, 2);
        SideButton(this.btnRemoveDependent, "btnRemoveDependent", "Remove", 132, false, 3);
        this.btnAddDependent.Click += new System.EventHandler(this.btnAddDependent_Click);
        this.btnEditDependent.Click += new System.EventHandler(this.btnEditDependent_Click);
        this.btnRemoveDependent.Click += new System.EventHandler(this.btnRemoveDependent_Click);

        this.lblDependentsHint.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Italic);
        this.lblDependentsHint.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
        this.lblDependentsHint.Location = new System.Drawing.Point(24, 418);
        this.lblDependentsHint.Name = "lblDependentsHint";
        this.lblDependentsHint.Size = new System.Drawing.Size(850, 40);
        this.lblDependentsHint.TabIndex = 4;
        this.lblDependentsHint.Text = "Dependents are the children, spouse, parents or others who depend on this resident. " +
                                      "They are saved together with the resident when you press Save.";

        // ---- tabs ----
        this.tabPersonal.BackColor = System.Drawing.Color.FromArgb(243, 239, 231);
        this.tabPersonal.Name = "tabPersonal";
        this.tabPersonal.Text = "Personal details";
        this.tabPersonal.Controls.Add(this.lblNote);
        this.tabPersonal.Controls.Add(this.grpClassification);
        this.tabPersonal.Controls.Add(this.chkVoter);
        this.tabPersonal.Controls.Add(this.dtpResidency);
        this.tabPersonal.Controls.Add(this.lblResidency);
        this.tabPersonal.Controls.Add(this.txtContact);
        this.tabPersonal.Controls.Add(this.lblContact);
        this.tabPersonal.Controls.Add(this.txtHouseNumber);
        this.tabPersonal.Controls.Add(this.lblHouseNumber);
        this.tabPersonal.Controls.Add(this.cmbPurok);
        this.tabPersonal.Controls.Add(this.lblPurok);
        this.tabPersonal.Controls.Add(this.txtOccupation);
        this.tabPersonal.Controls.Add(this.lblOccupation);
        this.tabPersonal.Controls.Add(this.cmbCivilStatus);
        this.tabPersonal.Controls.Add(this.lblCivilStatus);
        this.tabPersonal.Controls.Add(this.radFemale);
        this.tabPersonal.Controls.Add(this.radMale);
        this.tabPersonal.Controls.Add(this.lblGender);
        this.tabPersonal.Controls.Add(this.dtpBirth);
        this.tabPersonal.Controls.Add(this.lblBirth);
        this.tabPersonal.Controls.Add(this.txtSuffix);
        this.tabPersonal.Controls.Add(this.lblSuffix);
        this.tabPersonal.Controls.Add(this.txtLastName);
        this.tabPersonal.Controls.Add(this.lblLastName);
        this.tabPersonal.Controls.Add(this.txtMiddleName);
        this.tabPersonal.Controls.Add(this.lblMiddleName);
        this.tabPersonal.Controls.Add(this.txtFirstName);
        this.tabPersonal.Controls.Add(this.lblFirstName);

        this.tabDependents.BackColor = System.Drawing.Color.FromArgb(243, 239, 231);
        this.tabDependents.Name = "tabDependents";
        this.tabDependents.Text = "Dependents";
        this.tabDependents.Controls.Add(this.lblDependentsHint);
        this.tabDependents.Controls.Add(this.btnRemoveDependent);
        this.tabDependents.Controls.Add(this.btnEditDependent);
        this.tabDependents.Controls.Add(this.btnAddDependent);
        this.tabDependents.Controls.Add(this.dgvDependents);

        this.tabs.Controls.Add(this.tabPersonal);
        this.tabs.Controls.Add(this.tabDependents);
        this.tabs.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Regular);
        this.tabs.ItemSize = new System.Drawing.Size(160, 32);
        this.tabs.Location = new System.Drawing.Point(20, 16);
        this.tabs.Name = "tabs";
        this.tabs.SelectedIndex = 0;
        this.tabs.Size = new System.Drawing.Size(906, 544);
        this.tabs.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
        this.tabs.TabIndex = 0;

        // ---- Save / Cancel ----
        this.btnSave.BackColor = navy;
        this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnSave.Font = buttonFont;
        this.btnSave.ForeColor = System.Drawing.Color.White;
        this.btnSave.Location = new System.Drawing.Point(706, 576);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(110, 42);
        this.btnSave.TabIndex = 1;
        this.btnSave.Text = "Save";
        this.btnSave.UseVisualStyleBackColor = false;
        this.btnSave.FlatAppearance.BorderSize = 0;
        this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

        this.btnCancel.BackColor = System.Drawing.Color.White;
        this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnCancel.Font = buttonFont;
        this.btnCancel.ForeColor = ink;
        this.btnCancel.Location = new System.Drawing.Point(826, 576);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(100, 42);
        this.btnCancel.TabIndex = 2;
        this.btnCancel.Text = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = false;
        this.btnCancel.FlatAppearance.BorderColor = line;
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

        this.AcceptButton = this.btnSave;
        this.CancelButton = this.btnCancel;
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.FromArgb(243, 239, 231);
        this.ClientSize = new System.Drawing.Size(946, 636);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnSave);
        this.Controls.Add(this.tabs);
        this.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Regular);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ResidentForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Register Resident";
        this.Load += new System.EventHandler(this.ResidentForm_Load);
        ((System.ComponentModel.ISupportInitialize)(this.dgvDependents)).EndInit();
        this.grpClassification.ResumeLayout(false);
        this.grpClassification.PerformLayout();
        this.tabDependents.ResumeLayout(false);
        this.tabPersonal.ResumeLayout(false);
        this.tabPersonal.PerformLayout();
        this.tabs.ResumeLayout(false);
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TabControl tabs;
    private System.Windows.Forms.TabPage tabPersonal;
    private System.Windows.Forms.TabPage tabDependents;
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
    private System.Windows.Forms.Label lblCivilStatus;
    private System.Windows.Forms.ComboBox cmbCivilStatus;
    private System.Windows.Forms.Label lblPurok;
    private System.Windows.Forms.ComboBox cmbPurok;
    private System.Windows.Forms.Label lblHouseNumber;
    private System.Windows.Forms.TextBox txtHouseNumber;
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
    private System.Windows.Forms.CheckBox chkSoloParent;
    private System.Windows.Forms.Label lblNote;
    private System.Windows.Forms.DataGridView dgvDependents;
    private System.Windows.Forms.Button btnAddDependent;
    private System.Windows.Forms.Button btnEditDependent;
    private System.Windows.Forms.Button btnRemoveDependent;
    private System.Windows.Forms.Label lblDependentsHint;
    private System.Windows.Forms.Button btnSave;
    private System.Windows.Forms.Button btnCancel;
}
