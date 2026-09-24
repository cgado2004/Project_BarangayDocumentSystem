namespace BarangayDocumentSystem.Forms
{
    partial class ResidentForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtFirstName;
        private System.Windows.Forms.TextBox txtMiddleName;
        private System.Windows.Forms.TextBox txtLastName;
        private System.Windows.Forms.TextBox txtSuffix;
        private System.Windows.Forms.TextBox txtPurok;
        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.TextBox txtContact;
        private System.Windows.Forms.TextBox txtOccupation;
        private System.Windows.Forms.DateTimePicker dtpBirth;
        private System.Windows.Forms.DateTimePicker dtpResidency;
        private System.Windows.Forms.ComboBox cmbGender;
        private System.Windows.Forms.ComboBox cmbCivilStatus;
        private System.Windows.Forms.CheckBox chkVoter;
        private System.Windows.Forms.CheckBox chkSenior;
        private System.Windows.Forms.CheckBox chkPwd;
        private System.Windows.Forms.CheckBox chkIndigent;
        private System.Windows.Forms.CheckBox chkStudent;
        private System.Windows.Forms.CheckBox chkSoloParent;
        private System.Windows.Forms.CheckBox chkJobseekerUsed;
        private System.Windows.Forms.FlowLayoutPanel pnlClassifications;
        private System.Windows.Forms.TableLayoutPanel tblFields;
        private System.Windows.Forms.Label lblFirstNameCaption;
        private System.Windows.Forms.Label lblMiddleNameCaption;
        private System.Windows.Forms.Label lblLastNameCaption;
        private System.Windows.Forms.Label lblSuffixCaption;
        private System.Windows.Forms.Label lblBirthCaption;
        private System.Windows.Forms.Label lblGenderCaption;
        private System.Windows.Forms.Label lblCivilStatusCaption;
        private System.Windows.Forms.Label lblPurokCaption;
        private System.Windows.Forms.Label lblAddressCaption;
        private System.Windows.Forms.Label lblContactCaption;
        private System.Windows.Forms.Label lblOccupationCaption;
        private System.Windows.Forms.Label lblResidencyCaption;
        private System.Windows.Forms.Label lblVoterCaption;
        private System.Windows.Forms.Label lblClassificationsCaption;
        private System.Windows.Forms.Label lblJobseekerUsedCaption;
        private System.Windows.Forms.Panel pnlBody;
        private System.Windows.Forms.FlowLayoutPanel pnlActions;
        private System.Windows.Forms.Button btnSaveResident;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtFirstName = new System.Windows.Forms.TextBox();
            this.txtMiddleName = new System.Windows.Forms.TextBox();
            this.txtLastName = new System.Windows.Forms.TextBox();
            this.txtSuffix = new System.Windows.Forms.TextBox();
            this.txtPurok = new System.Windows.Forms.TextBox();
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.txtContact = new System.Windows.Forms.TextBox();
            this.txtOccupation = new System.Windows.Forms.TextBox();
            this.dtpBirth = new System.Windows.Forms.DateTimePicker();
            this.dtpResidency = new System.Windows.Forms.DateTimePicker();
            this.cmbGender = new System.Windows.Forms.ComboBox();
            this.cmbCivilStatus = new System.Windows.Forms.ComboBox();
            this.chkVoter = new System.Windows.Forms.CheckBox();
            this.chkSenior = new System.Windows.Forms.CheckBox();
            this.chkPwd = new System.Windows.Forms.CheckBox();
            this.chkIndigent = new System.Windows.Forms.CheckBox();
            this.chkStudent = new System.Windows.Forms.CheckBox();
            this.chkSoloParent = new System.Windows.Forms.CheckBox();
            this.chkJobseekerUsed = new System.Windows.Forms.CheckBox();
            this.pnlClassifications = new System.Windows.Forms.FlowLayoutPanel();
            this.tblFields = new System.Windows.Forms.TableLayoutPanel();
            this.lblFirstNameCaption = new System.Windows.Forms.Label();
            this.lblMiddleNameCaption = new System.Windows.Forms.Label();
            this.lblLastNameCaption = new System.Windows.Forms.Label();
            this.lblSuffixCaption = new System.Windows.Forms.Label();
            this.lblBirthCaption = new System.Windows.Forms.Label();
            this.lblGenderCaption = new System.Windows.Forms.Label();
            this.lblCivilStatusCaption = new System.Windows.Forms.Label();
            this.lblPurokCaption = new System.Windows.Forms.Label();
            this.lblAddressCaption = new System.Windows.Forms.Label();
            this.lblContactCaption = new System.Windows.Forms.Label();
            this.lblOccupationCaption = new System.Windows.Forms.Label();
            this.lblResidencyCaption = new System.Windows.Forms.Label();
            this.lblVoterCaption = new System.Windows.Forms.Label();
            this.lblClassificationsCaption = new System.Windows.Forms.Label();
            this.lblJobseekerUsedCaption = new System.Windows.Forms.Label();
            this.pnlBody = new System.Windows.Forms.Panel();
            this.pnlActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSaveResident = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pnlClassifications.SuspendLayout();
            this.tblFields.SuspendLayout();
            this.pnlBody.SuspendLayout();
            this.pnlActions.SuspendLayout();
            this.SuspendLayout();

            // txtFirstName
            this.txtFirstName.Name = "txtFirstName";
            this.txtFirstName.TabIndex = 0;
            this.txtFirstName.MaxLength = 80;

            // txtMiddleName
            this.txtMiddleName.Name = "txtMiddleName";
            this.txtMiddleName.TabIndex = 1;
            this.txtMiddleName.MaxLength = 80;

            // txtLastName
            this.txtLastName.Name = "txtLastName";
            this.txtLastName.TabIndex = 2;
            this.txtLastName.MaxLength = 80;

            // txtSuffix
            this.txtSuffix.Name = "txtSuffix";
            this.txtSuffix.TabIndex = 3;
            this.txtSuffix.MaxLength = 20;

            // txtPurok
            this.txtPurok.Name = "txtPurok";
            this.txtPurok.TabIndex = 4;
            this.txtPurok.MaxLength = 60;

            // txtAddress
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.TabIndex = 5;
            this.txtAddress.MaxLength = 250;

            // txtContact
            this.txtContact.Name = "txtContact";
            this.txtContact.TabIndex = 6;
            this.txtContact.MaxLength = 15;

            // txtOccupation
            this.txtOccupation.Name = "txtOccupation";
            this.txtOccupation.TabIndex = 7;
            this.txtOccupation.MaxLength = 100;

            // dtpBirth
            this.dtpBirth.Name = "dtpBirth";
            this.dtpBirth.TabIndex = 8;
            this.dtpBirth.Format = System.Windows.Forms.DateTimePickerFormat.Short;

            // dtpResidency
            this.dtpResidency.Name = "dtpResidency";
            this.dtpResidency.TabIndex = 9;
            this.dtpResidency.Format = System.Windows.Forms.DateTimePickerFormat.Short;

            // cmbGender
            this.cmbGender.Name = "cmbGender";
            this.cmbGender.TabIndex = 10;
            this.cmbGender.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            // cmbCivilStatus
            this.cmbCivilStatus.Name = "cmbCivilStatus";
            this.cmbCivilStatus.TabIndex = 11;
            this.cmbCivilStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            // chkVoter
            this.chkVoter.Name = "chkVoter";
            this.chkVoter.TabIndex = 12;
            this.chkVoter.Text = "Registered voter";
            this.chkVoter.AutoSize = true;

            // chkSenior
            this.chkSenior.Name = "chkSenior";
            this.chkSenior.TabIndex = 13;
            this.chkSenior.Text = "Senior citizen";
            this.chkSenior.AutoSize = true;

            // chkPwd
            this.chkPwd.Name = "chkPwd";
            this.chkPwd.TabIndex = 14;
            this.chkPwd.Text = "Person with disability";
            this.chkPwd.AutoSize = true;

            // chkIndigent
            this.chkIndigent.Name = "chkIndigent";
            this.chkIndigent.TabIndex = 15;
            this.chkIndigent.Text = "Indigent";
            this.chkIndigent.AutoSize = true;

            // chkStudent
            this.chkStudent.Name = "chkStudent";
            this.chkStudent.TabIndex = 16;
            this.chkStudent.Text = "Student";
            this.chkStudent.AutoSize = true;

            // chkSoloParent
            this.chkSoloParent.Name = "chkSoloParent";
            this.chkSoloParent.TabIndex = 17;
            this.chkSoloParent.Text = "Solo parent";
            this.chkSoloParent.AutoSize = true;

            // chkJobseekerUsed
            this.chkJobseekerUsed.Name = "chkJobseekerUsed";
            this.chkJobseekerUsed.TabIndex = 18;
            this.chkJobseekerUsed.Text = "Previously used first-time jobseeker benefit";
            this.chkJobseekerUsed.AutoSize = true;

            // pnlClassifications
            this.pnlClassifications.Name = "pnlClassifications";
            this.pnlClassifications.TabIndex = 19;
            this.pnlClassifications.AutoSize = true;
            this.pnlClassifications.WrapContents = true;
            this.pnlClassifications.Controls.Add(this.chkSenior);
            this.pnlClassifications.Controls.Add(this.chkPwd);
            this.pnlClassifications.Controls.Add(this.chkIndigent);
            this.pnlClassifications.Controls.Add(this.chkStudent);
            this.pnlClassifications.Controls.Add(this.chkSoloParent);

            // tblFields
            this.tblFields.Name = "tblFields";
            this.tblFields.TabIndex = 20;
            this.tblFields.ColumnCount = 2;
            this.tblFields.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblFields.AutoSize = true;
            this.tblFields.Padding = new System.Windows.Forms.Padding(12);
            this.tblFields.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddRows;
            this.tblFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
            this.tblFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // lblFirstNameCaption
            this.lblFirstNameCaption.Name = "lblFirstNameCaption";
            this.lblFirstNameCaption.TabIndex = 21;
            this.lblFirstNameCaption.Text = "First name *";
            this.lblFirstNameCaption.AutoSize = true;
            this.lblFirstNameCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblFirstNameCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtFirstName.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtFirstName.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblFirstNameCaption, 0, 0);
            this.tblFields.Controls.Add(this.txtFirstName, 1, 0);

            // lblMiddleNameCaption
            this.lblMiddleNameCaption.Name = "lblMiddleNameCaption";
            this.lblMiddleNameCaption.TabIndex = 22;
            this.lblMiddleNameCaption.Text = "Middle name";
            this.lblMiddleNameCaption.AutoSize = true;
            this.lblMiddleNameCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblMiddleNameCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtMiddleName.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtMiddleName.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblMiddleNameCaption, 0, 1);
            this.tblFields.Controls.Add(this.txtMiddleName, 1, 1);

            // lblLastNameCaption
            this.lblLastNameCaption.Name = "lblLastNameCaption";
            this.lblLastNameCaption.TabIndex = 23;
            this.lblLastNameCaption.Text = "Last name *";
            this.lblLastNameCaption.AutoSize = true;
            this.lblLastNameCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblLastNameCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtLastName.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtLastName.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblLastNameCaption, 0, 2);
            this.tblFields.Controls.Add(this.txtLastName, 1, 2);

            // lblSuffixCaption
            this.lblSuffixCaption.Name = "lblSuffixCaption";
            this.lblSuffixCaption.TabIndex = 24;
            this.lblSuffixCaption.Text = "Suffix";
            this.lblSuffixCaption.AutoSize = true;
            this.lblSuffixCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSuffixCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtSuffix.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtSuffix.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblSuffixCaption, 0, 3);
            this.tblFields.Controls.Add(this.txtSuffix, 1, 3);

            // lblBirthCaption
            this.lblBirthCaption.Name = "lblBirthCaption";
            this.lblBirthCaption.TabIndex = 25;
            this.lblBirthCaption.Text = "Date of birth *";
            this.lblBirthCaption.AutoSize = true;
            this.lblBirthCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblBirthCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.dtpBirth.Dock = System.Windows.Forms.DockStyle.Top;
            this.dtpBirth.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblBirthCaption, 0, 4);
            this.tblFields.Controls.Add(this.dtpBirth, 1, 4);

            // lblGenderCaption
            this.lblGenderCaption.Name = "lblGenderCaption";
            this.lblGenderCaption.TabIndex = 26;
            this.lblGenderCaption.Text = "Gender *";
            this.lblGenderCaption.AutoSize = true;
            this.lblGenderCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblGenderCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.cmbGender.Dock = System.Windows.Forms.DockStyle.Top;
            this.cmbGender.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblGenderCaption, 0, 5);
            this.tblFields.Controls.Add(this.cmbGender, 1, 5);

            // lblCivilStatusCaption
            this.lblCivilStatusCaption.Name = "lblCivilStatusCaption";
            this.lblCivilStatusCaption.TabIndex = 27;
            this.lblCivilStatusCaption.Text = "Civil status *";
            this.lblCivilStatusCaption.AutoSize = true;
            this.lblCivilStatusCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCivilStatusCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.cmbCivilStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.cmbCivilStatus.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblCivilStatusCaption, 0, 6);
            this.tblFields.Controls.Add(this.cmbCivilStatus, 1, 6);

            // lblPurokCaption
            this.lblPurokCaption.Name = "lblPurokCaption";
            this.lblPurokCaption.TabIndex = 28;
            this.lblPurokCaption.Text = "Purok *";
            this.lblPurokCaption.AutoSize = true;
            this.lblPurokCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPurokCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtPurok.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtPurok.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblPurokCaption, 0, 7);
            this.tblFields.Controls.Add(this.txtPurok, 1, 7);

            // lblAddressCaption
            this.lblAddressCaption.Name = "lblAddressCaption";
            this.lblAddressCaption.TabIndex = 29;
            this.lblAddressCaption.Text = "Address *";
            this.lblAddressCaption.AutoSize = true;
            this.lblAddressCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAddressCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtAddress.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtAddress.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblAddressCaption, 0, 8);
            this.tblFields.Controls.Add(this.txtAddress, 1, 8);

            // lblContactCaption
            this.lblContactCaption.Name = "lblContactCaption";
            this.lblContactCaption.TabIndex = 30;
            this.lblContactCaption.Text = "Contact number *";
            this.lblContactCaption.AutoSize = true;
            this.lblContactCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblContactCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtContact.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtContact.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblContactCaption, 0, 9);
            this.tblFields.Controls.Add(this.txtContact, 1, 9);

            // lblOccupationCaption
            this.lblOccupationCaption.Name = "lblOccupationCaption";
            this.lblOccupationCaption.TabIndex = 31;
            this.lblOccupationCaption.Text = "Occupation";
            this.lblOccupationCaption.AutoSize = true;
            this.lblOccupationCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOccupationCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtOccupation.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtOccupation.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblOccupationCaption, 0, 10);
            this.tblFields.Controls.Add(this.txtOccupation, 1, 10);

            // lblResidencyCaption
            this.lblResidencyCaption.Name = "lblResidencyCaption";
            this.lblResidencyCaption.TabIndex = 32;
            this.lblResidencyCaption.Text = "Resident since *";
            this.lblResidencyCaption.AutoSize = true;
            this.lblResidencyCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblResidencyCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.dtpResidency.Dock = System.Windows.Forms.DockStyle.Top;
            this.dtpResidency.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblResidencyCaption, 0, 11);
            this.tblFields.Controls.Add(this.dtpResidency, 1, 11);

            // lblVoterCaption
            this.lblVoterCaption.Name = "lblVoterCaption";
            this.lblVoterCaption.TabIndex = 33;
            this.lblVoterCaption.Text = "Voter status";
            this.lblVoterCaption.AutoSize = true;
            this.lblVoterCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblVoterCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.chkVoter.Dock = System.Windows.Forms.DockStyle.Top;
            this.chkVoter.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblVoterCaption, 0, 12);
            this.tblFields.Controls.Add(this.chkVoter, 1, 12);

            // lblClassificationsCaption
            this.lblClassificationsCaption.Name = "lblClassificationsCaption";
            this.lblClassificationsCaption.TabIndex = 34;
            this.lblClassificationsCaption.Text = "Classifications";
            this.lblClassificationsCaption.AutoSize = true;
            this.lblClassificationsCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblClassificationsCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.pnlClassifications.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlClassifications.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblClassificationsCaption, 0, 13);
            this.tblFields.Controls.Add(this.pnlClassifications, 1, 13);

            // lblJobseekerUsedCaption
            this.lblJobseekerUsedCaption.Name = "lblJobseekerUsedCaption";
            this.lblJobseekerUsedCaption.TabIndex = 35;
            this.lblJobseekerUsedCaption.Text = "Jobseeker history";
            this.lblJobseekerUsedCaption.AutoSize = true;
            this.lblJobseekerUsedCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblJobseekerUsedCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.chkJobseekerUsed.Dock = System.Windows.Forms.DockStyle.Top;
            this.chkJobseekerUsed.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblJobseekerUsedCaption, 0, 14);
            this.tblFields.Controls.Add(this.chkJobseekerUsed, 1, 14);

            // pnlBody
            this.pnlBody.Name = "pnlBody";
            this.pnlBody.TabIndex = 36;
            this.pnlBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBody.AutoScroll = true;
            this.pnlBody.Controls.Add(this.tblFields);

            // pnlActions
            this.pnlActions.Name = "pnlActions";
            this.pnlActions.TabIndex = 37;
            this.pnlActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlActions.AutoSize = true;
            this.pnlActions.Padding = new System.Windows.Forms.Padding(12);
            this.pnlActions.WrapContents = true;

            // btnSaveResident
            this.btnSaveResident.Name = "btnSaveResident";
            this.btnSaveResident.TabIndex = 38;
            this.btnSaveResident.Text = "Save resident";
            this.btnSaveResident.AutoSize = true;
            this.btnSaveResident.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSaveResident.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnSaveResident.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnSaveResident.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnSaveResident.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveResident.BackColor = System.Drawing.Color.FromArgb(31, 91, 76);
            this.btnSaveResident.ForeColor = System.Drawing.Color.White;
            this.btnSaveResident.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSaveResident.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);

            // btnCancel
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.TabIndex = 39;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.AutoSize = true;
            this.btnCancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCancel.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnCancel.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(35, 45, 42);
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.pnlActions.Controls.Add(this.btnSaveResident);
            this.pnlActions.Controls.Add(this.btnCancel);
            this.AcceptButton = this.btnSaveResident;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.pnlBody);
            this.Controls.Add(this.pnlActions);

            // ResidentForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(680, 700);
            this.MinimumSize = new System.Drawing.Size(680, 400);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.Name = "ResidentForm";
            this.Text = "Resident";
            this.tblFields.RowCount = 15;
            this.txtContact.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ContactKeyPress);
            this.btnSaveResident.Click += new System.EventHandler(this.SaveResident);
            this.btnCancel.Click += new System.EventHandler(this.CloseDialog);
            this.pnlClassifications.ResumeLayout(false);
            this.pnlClassifications.PerformLayout();
            this.tblFields.ResumeLayout(false);
            this.tblFields.PerformLayout();
            this.pnlBody.ResumeLayout(false);
            this.pnlBody.PerformLayout();
            this.pnlActions.ResumeLayout(false);
            this.pnlActions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
