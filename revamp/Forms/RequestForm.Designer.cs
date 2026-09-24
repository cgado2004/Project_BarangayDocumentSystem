namespace BarangayDocumentSystem.Forms
{
    partial class RequestForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ComboBox cmbResident;
        private System.Windows.Forms.ComboBox cmbDocument;
        private System.Windows.Forms.TextBox txtPurpose;
        private System.Windows.Forms.TextBox txtBusinessName;
        private System.Windows.Forms.TextBox txtBusinessAddress;
        private System.Windows.Forms.TextBox txtBusinessNature;
        private System.Windows.Forms.Label lblFee;
        private System.Windows.Forms.TableLayoutPanel tblFields;
        private System.Windows.Forms.Label lblResidentCaption;
        private System.Windows.Forms.Label lblDocumentCaption;
        private System.Windows.Forms.Label lblPurposeCaption;
        private System.Windows.Forms.TableLayoutPanel businessFields;
        private System.Windows.Forms.Label lblBusinessNameCaption;
        private System.Windows.Forms.Label lblBusinessAddressCaption;
        private System.Windows.Forms.Label lblBusinessNatureCaption;
        private System.Windows.Forms.TableLayoutPanel tblContent;
        private System.Windows.Forms.Panel pnlBody;
        private System.Windows.Forms.FlowLayoutPanel pnlActions;
        private System.Windows.Forms.Button btnSubmit;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.cmbResident = new System.Windows.Forms.ComboBox();
            this.cmbDocument = new System.Windows.Forms.ComboBox();
            this.txtPurpose = new System.Windows.Forms.TextBox();
            this.txtBusinessName = new System.Windows.Forms.TextBox();
            this.txtBusinessAddress = new System.Windows.Forms.TextBox();
            this.txtBusinessNature = new System.Windows.Forms.TextBox();
            this.lblFee = new System.Windows.Forms.Label();
            this.tblFields = new System.Windows.Forms.TableLayoutPanel();
            this.lblResidentCaption = new System.Windows.Forms.Label();
            this.lblDocumentCaption = new System.Windows.Forms.Label();
            this.lblPurposeCaption = new System.Windows.Forms.Label();
            this.businessFields = new System.Windows.Forms.TableLayoutPanel();
            this.lblBusinessNameCaption = new System.Windows.Forms.Label();
            this.lblBusinessAddressCaption = new System.Windows.Forms.Label();
            this.lblBusinessNatureCaption = new System.Windows.Forms.Label();
            this.tblContent = new System.Windows.Forms.TableLayoutPanel();
            this.pnlBody = new System.Windows.Forms.Panel();
            this.pnlActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSubmit = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tblFields.SuspendLayout();
            this.businessFields.SuspendLayout();
            this.tblContent.SuspendLayout();
            this.pnlBody.SuspendLayout();
            this.pnlActions.SuspendLayout();
            this.SuspendLayout();

            // cmbResident
            this.cmbResident.Name = "cmbResident";
            this.cmbResident.TabIndex = 0;
            this.cmbResident.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbResident.DropDownWidth = 480;

            // cmbDocument
            this.cmbDocument.Name = "cmbDocument";
            this.cmbDocument.TabIndex = 1;
            this.cmbDocument.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDocument.DropDownWidth = 480;

            // txtPurpose
            this.txtPurpose.Name = "txtPurpose";
            this.txtPurpose.TabIndex = 2;
            this.txtPurpose.MaxLength = 300;

            // txtBusinessName
            this.txtBusinessName.Name = "txtBusinessName";
            this.txtBusinessName.TabIndex = 3;
            this.txtBusinessName.MaxLength = 120;

            // txtBusinessAddress
            this.txtBusinessAddress.Name = "txtBusinessAddress";
            this.txtBusinessAddress.TabIndex = 4;
            this.txtBusinessAddress.MaxLength = 250;

            // txtBusinessNature
            this.txtBusinessNature.Name = "txtBusinessNature";
            this.txtBusinessNature.TabIndex = 5;
            this.txtBusinessNature.MaxLength = 150;

            // lblFee
            this.lblFee.Name = "lblFee";
            this.lblFee.TabIndex = 6;
            this.lblFee.AutoSize = true;
            this.lblFee.MaximumSize = new System.Drawing.Size(650, 0);
            this.lblFee.Padding = new System.Windows.Forms.Padding(12);
            this.lblFee.ForeColor = System.Drawing.Color.FromArgb(31, 91, 76);

            // tblFields
            this.tblFields.Name = "tblFields";
            this.tblFields.TabIndex = 7;
            this.tblFields.ColumnCount = 2;
            this.tblFields.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblFields.AutoSize = true;
            this.tblFields.Padding = new System.Windows.Forms.Padding(12);
            this.tblFields.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddRows;
            this.tblFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
            this.tblFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // lblResidentCaption
            this.lblResidentCaption.Name = "lblResidentCaption";
            this.lblResidentCaption.TabIndex = 8;
            this.lblResidentCaption.Text = "Resident *";
            this.lblResidentCaption.AutoSize = true;
            this.lblResidentCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblResidentCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.cmbResident.Dock = System.Windows.Forms.DockStyle.Top;
            this.cmbResident.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblResidentCaption, 0, 0);
            this.tblFields.Controls.Add(this.cmbResident, 1, 0);

            // lblDocumentCaption
            this.lblDocumentCaption.Name = "lblDocumentCaption";
            this.lblDocumentCaption.TabIndex = 9;
            this.lblDocumentCaption.Text = "Document *";
            this.lblDocumentCaption.AutoSize = true;
            this.lblDocumentCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDocumentCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.cmbDocument.Dock = System.Windows.Forms.DockStyle.Top;
            this.cmbDocument.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblDocumentCaption, 0, 1);
            this.tblFields.Controls.Add(this.cmbDocument, 1, 1);

            // lblPurposeCaption
            this.lblPurposeCaption.Name = "lblPurposeCaption";
            this.lblPurposeCaption.TabIndex = 10;
            this.lblPurposeCaption.Text = "Purpose *";
            this.lblPurposeCaption.AutoSize = true;
            this.lblPurposeCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPurposeCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtPurpose.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtPurpose.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblPurposeCaption, 0, 2);
            this.tblFields.Controls.Add(this.txtPurpose, 1, 2);

            // businessFields
            this.businessFields.Name = "businessFields";
            this.businessFields.TabIndex = 11;
            this.businessFields.ColumnCount = 2;
            this.businessFields.Dock = System.Windows.Forms.DockStyle.Top;
            this.businessFields.AutoSize = true;
            this.businessFields.Padding = new System.Windows.Forms.Padding(12);
            this.businessFields.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddRows;
            this.businessFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
            this.businessFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // lblBusinessNameCaption
            this.lblBusinessNameCaption.Name = "lblBusinessNameCaption";
            this.lblBusinessNameCaption.TabIndex = 12;
            this.lblBusinessNameCaption.Text = "Business name *";
            this.lblBusinessNameCaption.AutoSize = true;
            this.lblBusinessNameCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblBusinessNameCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtBusinessName.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtBusinessName.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.businessFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.businessFields.Controls.Add(this.lblBusinessNameCaption, 0, 0);
            this.businessFields.Controls.Add(this.txtBusinessName, 1, 0);

            // lblBusinessAddressCaption
            this.lblBusinessAddressCaption.Name = "lblBusinessAddressCaption";
            this.lblBusinessAddressCaption.TabIndex = 13;
            this.lblBusinessAddressCaption.Text = "Business address *";
            this.lblBusinessAddressCaption.AutoSize = true;
            this.lblBusinessAddressCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblBusinessAddressCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtBusinessAddress.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtBusinessAddress.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.businessFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.businessFields.Controls.Add(this.lblBusinessAddressCaption, 0, 1);
            this.businessFields.Controls.Add(this.txtBusinessAddress, 1, 1);

            // lblBusinessNatureCaption
            this.lblBusinessNatureCaption.Name = "lblBusinessNatureCaption";
            this.lblBusinessNatureCaption.TabIndex = 14;
            this.lblBusinessNatureCaption.Text = "Nature of business *";
            this.lblBusinessNatureCaption.AutoSize = true;
            this.lblBusinessNatureCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblBusinessNatureCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtBusinessNature.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtBusinessNature.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.businessFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.businessFields.Controls.Add(this.lblBusinessNatureCaption, 0, 2);
            this.businessFields.Controls.Add(this.txtBusinessNature, 1, 2);

            // tblContent
            this.tblContent.Name = "tblContent";
            this.tblContent.TabIndex = 15;
            this.tblContent.ColumnCount = 1;
            this.tblContent.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblContent.AutoSize = true;
            this.tblContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblContent.Controls.Add(this.tblFields);
            this.tblContent.Controls.Add(this.businessFields);
            this.tblContent.Controls.Add(this.lblFee);

            // pnlBody
            this.pnlBody.Name = "pnlBody";
            this.pnlBody.TabIndex = 16;
            this.pnlBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBody.AutoScroll = true;
            this.pnlBody.Controls.Add(this.tblContent);

            // pnlActions
            this.pnlActions.Name = "pnlActions";
            this.pnlActions.TabIndex = 17;
            this.pnlActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlActions.AutoSize = true;
            this.pnlActions.Padding = new System.Windows.Forms.Padding(12);
            this.pnlActions.WrapContents = true;

            // btnSubmit
            this.btnSubmit.Name = "btnSubmit";
            this.btnSubmit.TabIndex = 18;
            this.btnSubmit.Text = "File request";
            this.btnSubmit.AutoSize = true;
            this.btnSubmit.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSubmit.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnSubmit.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnSubmit.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnSubmit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSubmit.BackColor = System.Drawing.Color.FromArgb(31, 91, 76);
            this.btnSubmit.ForeColor = System.Drawing.Color.White;
            this.btnSubmit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSubmit.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);

            // btnCancel
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.TabIndex = 19;
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
            this.pnlActions.Controls.Add(this.btnSubmit);
            this.pnlActions.Controls.Add(this.btnCancel);
            this.AcceptButton = this.btnSubmit;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.pnlBody);
            this.Controls.Add(this.pnlActions);

            // RequestForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(760, 550);
            this.MinimumSize = new System.Drawing.Size(760, 400);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.Name = "RequestForm";
            this.Text = "New document request";
            this.tblFields.RowCount = 3;
            this.businessFields.RowCount = 3;
            this.btnSubmit.Click += new System.EventHandler(this.SubmitRequest);
            this.btnCancel.Click += new System.EventHandler(this.CloseDialog);
            this.tblFields.ResumeLayout(false);
            this.tblFields.PerformLayout();
            this.businessFields.ResumeLayout(false);
            this.businessFields.PerformLayout();
            this.tblContent.ResumeLayout(false);
            this.tblContent.PerformLayout();
            this.pnlBody.ResumeLayout(false);
            this.pnlBody.PerformLayout();
            this.pnlActions.ResumeLayout(false);
            this.pnlActions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
