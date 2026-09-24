namespace BarangayDocumentSystem.Controls
{
    partial class ResidentsControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridViewCellStyle gridResidentsHeaderStyle;
        private System.Windows.Forms.DataGridViewCellStyle gridResidentsCellStyle;
        private System.Windows.Forms.DataGridViewCellStyle gridResidentsAlternateStyle;
        private System.Windows.Forms.DataGridView gridResidents;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResidentsFullName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResidentsDateOfBirth;
        private System.Windows.Forms.DataGridViewCellStyle colResidentsDateOfBirthStyle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResidentsGender;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResidentsPurok;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResidentsContactNumber;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.TableLayoutPanel tblSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.FlowLayoutPanel pnlActions;
        private System.Windows.Forms.Button btnRegister;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnRequest;
        private System.Windows.Forms.Label lblCount;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.gridResidentsHeaderStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.gridResidentsCellStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.gridResidentsAlternateStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.gridResidents = new System.Windows.Forms.DataGridView();
            this.colResidentsFullName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResidentsDateOfBirth = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResidentsDateOfBirthStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.colResidentsGender = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResidentsPurok = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResidentsContactNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.tblSearch = new System.Windows.Forms.TableLayoutPanel();
            this.lblSearch = new System.Windows.Forms.Label();
            this.pnlActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRegister = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnRequest = new System.Windows.Forms.Button();
            this.lblCount = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.gridResidents)).BeginInit();
            this.tblSearch.SuspendLayout();
            this.pnlActions.SuspendLayout();
            this.SuspendLayout();

            // gridResidentsHeaderStyle
            this.gridResidentsHeaderStyle.BackColor = System.Drawing.Color.FromArgb(31, 91, 76);
            this.gridResidentsHeaderStyle.ForeColor = System.Drawing.Color.White;
            this.gridResidentsHeaderStyle.SelectionBackColor = System.Drawing.Color.FromArgb(31, 91, 76);
            this.gridResidentsHeaderStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.False;

            // gridResidentsCellStyle
            this.gridResidentsCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(220, 237, 229);
            this.gridResidentsCellStyle.SelectionForeColor = System.Drawing.Color.Black;
            this.gridResidentsCellStyle.Padding = new System.Windows.Forms.Padding(3);

            // gridResidentsAlternateStyle
            this.gridResidentsAlternateStyle.BackColor = System.Drawing.Color.FromArgb(248, 250, 249);

            // gridResidents
            this.gridResidents.Name = "gridResidents";
            this.gridResidents.TabIndex = 3;
            this.gridResidents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridResidents.ReadOnly = true;
            this.gridResidents.AllowUserToAddRows = false;
            this.gridResidents.AllowUserToDeleteRows = false;
            this.gridResidents.AllowUserToResizeRows = false;
            this.gridResidents.AutoGenerateColumns = false;
            this.gridResidents.MultiSelect = false;
            this.gridResidents.RowHeadersVisible = false;
            this.gridResidents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridResidents.BackgroundColor = System.Drawing.Color.White;
            this.gridResidents.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridResidents.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridResidents.EnableHeadersVisualStyles = false;
            this.gridResidents.ColumnHeadersHeight = 38;
            this.gridResidents.ColumnHeadersDefaultCellStyle = this.gridResidentsHeaderStyle;
            this.gridResidents.DefaultCellStyle = this.gridResidentsCellStyle;
            this.gridResidents.AlternatingRowsDefaultCellStyle = this.gridResidentsAlternateStyle;
            this.gridResidents.RowTemplate.Height = 32;

            // colResidentsFullName
            this.colResidentsFullName.DataPropertyName = "FullName";
            this.colResidentsFullName.Name = "colResidentsFullName";
            this.colResidentsFullName.HeaderText = "Resident";
            this.colResidentsFullName.FillWeight = 160F;
            this.colResidentsFullName.MinimumWidth = 65;
            this.colResidentsFullName.ReadOnly = true;
            this.gridResidents.Columns.Add(this.colResidentsFullName);

            // colResidentsDateOfBirth
            this.colResidentsDateOfBirth.DataPropertyName = "DateOfBirth";
            this.colResidentsDateOfBirth.Name = "colResidentsDateOfBirth";
            this.colResidentsDateOfBirth.HeaderText = "Birth date";
            this.colResidentsDateOfBirth.FillWeight = 95F;
            this.colResidentsDateOfBirth.MinimumWidth = 65;
            this.colResidentsDateOfBirth.ReadOnly = true;

            // colResidentsDateOfBirthStyle
            this.colResidentsDateOfBirthStyle.Format = "yyyy-MM-dd";
            this.colResidentsDateOfBirth.DefaultCellStyle = this.colResidentsDateOfBirthStyle;
            this.gridResidents.Columns.Add(this.colResidentsDateOfBirth);

            // colResidentsGender
            this.colResidentsGender.DataPropertyName = "Gender";
            this.colResidentsGender.Name = "colResidentsGender";
            this.colResidentsGender.HeaderText = "Gender";
            this.colResidentsGender.FillWeight = 65F;
            this.colResidentsGender.MinimumWidth = 65;
            this.colResidentsGender.ReadOnly = true;
            this.gridResidents.Columns.Add(this.colResidentsGender);

            // colResidentsPurok
            this.colResidentsPurok.DataPropertyName = "Purok";
            this.colResidentsPurok.Name = "colResidentsPurok";
            this.colResidentsPurok.HeaderText = "Purok";
            this.colResidentsPurok.FillWeight = 80F;
            this.colResidentsPurok.MinimumWidth = 65;
            this.colResidentsPurok.ReadOnly = true;
            this.gridResidents.Columns.Add(this.colResidentsPurok);

            // colResidentsContactNumber
            this.colResidentsContactNumber.DataPropertyName = "ContactNumber";
            this.colResidentsContactNumber.Name = "colResidentsContactNumber";
            this.colResidentsContactNumber.HeaderText = "Contact";
            this.colResidentsContactNumber.FillWeight = 110F;
            this.colResidentsContactNumber.MinimumWidth = 65;
            this.colResidentsContactNumber.ReadOnly = true;
            this.gridResidents.Columns.Add(this.colResidentsContactNumber);

            // txtSearch
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.TabIndex = 10;
            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSearch.MaxLength = 150;
            this.txtSearch.Name = "txtSearchResidents";

            // tblSearch
            this.tblSearch.Name = "tblSearch";
            this.tblSearch.TabIndex = 11;
            this.tblSearch.ColumnCount = 2;
            this.tblSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblSearch.AutoSize = true;
            this.tblSearch.Padding = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.tblSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.tblSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // lblSearch
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.TabIndex = 12;
            this.lblSearch.Text = "Search name, purok, or contact";
            this.lblSearch.AutoSize = true;
            this.lblSearch.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.tblSearch.Controls.Add(this.lblSearch, 0, 0);
            this.tblSearch.Controls.Add(this.txtSearch, 1, 0);

            // pnlActions
            this.pnlActions.Name = "pnlActions";
            this.pnlActions.TabIndex = 13;
            this.pnlActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlActions.AutoSize = true;
            this.pnlActions.Padding = new System.Windows.Forms.Padding(0, 8, 0, 4);
            this.pnlActions.WrapContents = true;

            // btnRegister
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.TabIndex = 14;
            this.btnRegister.Text = "Register resident";
            this.btnRegister.AutoSize = true;
            this.btnRegister.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnRegister.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnRegister.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnRegister.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnRegister.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRegister.BackColor = System.Drawing.Color.FromArgb(31, 91, 76);
            this.btnRegister.ForeColor = System.Drawing.Color.White;
            this.btnRegister.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRegister.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnRegister);

            // btnEdit
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.TabIndex = 15;
            this.btnEdit.Text = "Edit";
            this.btnEdit.AutoSize = true;
            this.btnEdit.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnEdit.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnEdit.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnEdit.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnEdit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEdit.BackColor = System.Drawing.Color.White;
            this.btnEdit.ForeColor = System.Drawing.Color.FromArgb(35, 45, 42);
            this.btnEdit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnEdit.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnEdit);

            // btnDelete
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.TabIndex = 16;
            this.btnDelete.Text = "Delete";
            this.btnDelete.AutoSize = true;
            this.btnDelete.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnDelete.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnDelete.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDelete.BackColor = System.Drawing.Color.White;
            this.btnDelete.ForeColor = System.Drawing.Color.FromArgb(35, 45, 42);
            this.btnDelete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDelete.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnDelete);

            // btnRequest
            this.btnRequest.Name = "btnRequest";
            this.btnRequest.TabIndex = 17;
            this.btnRequest.Text = "File request";
            this.btnRequest.AutoSize = true;
            this.btnRequest.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnRequest.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnRequest.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnRequest.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnRequest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRequest.BackColor = System.Drawing.Color.White;
            this.btnRequest.ForeColor = System.Drawing.Color.FromArgb(35, 45, 42);
            this.btnRequest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRequest.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnRequest);

            // lblCount
            this.lblCount.Name = "lblCount";
            this.lblCount.TabIndex = 18;
            this.lblCount.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCount.Height = 36;
            this.lblCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Controls.Add(this.gridResidents);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.pnlActions);
            this.Controls.Add(this.tblSearch);
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Padding = new System.Windows.Forms.Padding(16);
            this.BackColor = System.Drawing.Color.FromArgb(242, 246, 244);
            this.Name = "ResidentsControl";
            this.Size = new System.Drawing.Size(1000, 600);
            this.gridResidents.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ResidentDoubleClick);
            this.txtSearch.TextChanged += new System.EventHandler(this.SearchChanged);
            this.btnRegister.Click += new System.EventHandler(this.RegisterResident);
            this.btnEdit.Click += new System.EventHandler(this.EditResident);
            this.btnDelete.Click += new System.EventHandler(this.DeleteResident);
            this.btnRequest.Click += new System.EventHandler(this.FileRequest);
            ((System.ComponentModel.ISupportInitialize)(this.gridResidents)).EndInit();
            this.tblSearch.ResumeLayout(false);
            this.tblSearch.PerformLayout();
            this.pnlActions.ResumeLayout(false);
            this.pnlActions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
