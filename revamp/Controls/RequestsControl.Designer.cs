namespace BarangayDocumentSystem.Controls
{
    partial class RequestsControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridViewCellStyle gridRequestsHeaderStyle;
        private System.Windows.Forms.DataGridViewCellStyle gridRequestsCellStyle;
        private System.Windows.Forms.DataGridViewCellStyle gridRequestsAlternateStyle;
        private System.Windows.Forms.DataGridView gridRequests;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRequestsReferenceNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRequestsResidentName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRequestsDocumentName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRequestsStatusText;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRequestsFee;
        private System.Windows.Forms.DataGridViewCellStyle colRequestsFeeStyle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRequestsPaymentText;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.TableLayoutPanel tblSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.FlowLayoutPanel pnlActions;
        private System.Windows.Forms.Button btnNewRequest;
        private System.Windows.Forms.Button btnProcess;
        private System.Windows.Forms.Button btnReady;
        private System.Windows.Forms.Button btnPay;
        private System.Windows.Forms.Button btnRelease;
        private System.Windows.Forms.Button btnReject;
        private System.Windows.Forms.Button btnPreview;
        private System.Windows.Forms.TextBox txtDetails;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.gridRequestsHeaderStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.gridRequestsCellStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.gridRequestsAlternateStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.gridRequests = new System.Windows.Forms.DataGridView();
            this.colRequestsReferenceNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRequestsResidentName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRequestsDocumentName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRequestsStatusText = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRequestsFee = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRequestsFeeStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.colRequestsPaymentText = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.cmbStatus = new System.Windows.Forms.ComboBox();
            this.tblSearch = new System.Windows.Forms.TableLayoutPanel();
            this.lblSearch = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.pnlActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnNewRequest = new System.Windows.Forms.Button();
            this.btnProcess = new System.Windows.Forms.Button();
            this.btnReady = new System.Windows.Forms.Button();
            this.btnPay = new System.Windows.Forms.Button();
            this.btnRelease = new System.Windows.Forms.Button();
            this.btnReject = new System.Windows.Forms.Button();
            this.btnPreview = new System.Windows.Forms.Button();
            this.txtDetails = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.gridRequests)).BeginInit();
            this.tblSearch.SuspendLayout();
            this.pnlActions.SuspendLayout();
            this.SuspendLayout();

            // gridRequestsHeaderStyle
            this.gridRequestsHeaderStyle.BackColor = System.Drawing.Color.FromArgb(31, 91, 76);
            this.gridRequestsHeaderStyle.ForeColor = System.Drawing.Color.White;
            this.gridRequestsHeaderStyle.SelectionBackColor = System.Drawing.Color.FromArgb(31, 91, 76);
            this.gridRequestsHeaderStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.False;

            // gridRequestsCellStyle
            this.gridRequestsCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(220, 237, 229);
            this.gridRequestsCellStyle.SelectionForeColor = System.Drawing.Color.Black;
            this.gridRequestsCellStyle.Padding = new System.Windows.Forms.Padding(3);

            // gridRequestsAlternateStyle
            this.gridRequestsAlternateStyle.BackColor = System.Drawing.Color.FromArgb(248, 250, 249);

            // gridRequests
            this.gridRequests.Name = "gridRequests";
            this.gridRequests.TabIndex = 3;
            this.gridRequests.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridRequests.ReadOnly = true;
            this.gridRequests.AllowUserToAddRows = false;
            this.gridRequests.AllowUserToDeleteRows = false;
            this.gridRequests.AllowUserToResizeRows = false;
            this.gridRequests.AutoGenerateColumns = false;
            this.gridRequests.MultiSelect = false;
            this.gridRequests.RowHeadersVisible = false;
            this.gridRequests.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridRequests.BackgroundColor = System.Drawing.Color.White;
            this.gridRequests.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridRequests.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridRequests.EnableHeadersVisualStyles = false;
            this.gridRequests.ColumnHeadersHeight = 38;
            this.gridRequests.ColumnHeadersDefaultCellStyle = this.gridRequestsHeaderStyle;
            this.gridRequests.DefaultCellStyle = this.gridRequestsCellStyle;
            this.gridRequests.AlternatingRowsDefaultCellStyle = this.gridRequestsAlternateStyle;
            this.gridRequests.RowTemplate.Height = 32;

            // colRequestsReferenceNumber
            this.colRequestsReferenceNumber.DataPropertyName = "ReferenceNumber";
            this.colRequestsReferenceNumber.Name = "colRequestsReferenceNumber";
            this.colRequestsReferenceNumber.HeaderText = "Reference";
            this.colRequestsReferenceNumber.FillWeight = 95F;
            this.colRequestsReferenceNumber.MinimumWidth = 65;
            this.colRequestsReferenceNumber.ReadOnly = true;
            this.gridRequests.Columns.Add(this.colRequestsReferenceNumber);

            // colRequestsResidentName
            this.colRequestsResidentName.DataPropertyName = "ResidentName";
            this.colRequestsResidentName.Name = "colRequestsResidentName";
            this.colRequestsResidentName.HeaderText = "Resident";
            this.colRequestsResidentName.FillWeight = 130F;
            this.colRequestsResidentName.MinimumWidth = 65;
            this.colRequestsResidentName.ReadOnly = true;
            this.gridRequests.Columns.Add(this.colRequestsResidentName);

            // colRequestsDocumentName
            this.colRequestsDocumentName.DataPropertyName = "DocumentName";
            this.colRequestsDocumentName.Name = "colRequestsDocumentName";
            this.colRequestsDocumentName.HeaderText = "Document";
            this.colRequestsDocumentName.FillWeight = 180F;
            this.colRequestsDocumentName.MinimumWidth = 65;
            this.colRequestsDocumentName.ReadOnly = true;
            this.gridRequests.Columns.Add(this.colRequestsDocumentName);

            // colRequestsStatusText
            this.colRequestsStatusText.DataPropertyName = "StatusText";
            this.colRequestsStatusText.Name = "colRequestsStatusText";
            this.colRequestsStatusText.HeaderText = "Status";
            this.colRequestsStatusText.FillWeight = 115F;
            this.colRequestsStatusText.MinimumWidth = 65;
            this.colRequestsStatusText.ReadOnly = true;
            this.gridRequests.Columns.Add(this.colRequestsStatusText);

            // colRequestsFee
            this.colRequestsFee.DataPropertyName = "Fee";
            this.colRequestsFee.Name = "colRequestsFee";
            this.colRequestsFee.HeaderText = "Fee (PHP)";
            this.colRequestsFee.FillWeight = 65F;
            this.colRequestsFee.MinimumWidth = 65;
            this.colRequestsFee.ReadOnly = true;

            // colRequestsFeeStyle
            this.colRequestsFeeStyle.Format = "N2";
            this.colRequestsFee.DefaultCellStyle = this.colRequestsFeeStyle;
            this.gridRequests.Columns.Add(this.colRequestsFee);

            // colRequestsPaymentText
            this.colRequestsPaymentText.DataPropertyName = "PaymentText";
            this.colRequestsPaymentText.Name = "colRequestsPaymentText";
            this.colRequestsPaymentText.HeaderText = "Payment";
            this.colRequestsPaymentText.FillWeight = 70F;
            this.colRequestsPaymentText.MinimumWidth = 65;
            this.colRequestsPaymentText.ReadOnly = true;
            this.gridRequests.Columns.Add(this.colRequestsPaymentText);

            // txtSearch
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.TabIndex = 11;
            this.txtSearch.MaxLength = 150;
            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;

            // cmbStatus
            this.cmbStatus.Name = "cmbStatus";
            this.cmbStatus.TabIndex = 12;
            this.cmbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbStatus.Items.AddRange(new object[] { "All statuses", "Pending", "Processing", "Ready for Release", "Released", "Rejected" });

            // tblSearch
            this.tblSearch.Name = "tblSearch";
            this.tblSearch.TabIndex = 13;
            this.tblSearch.ColumnCount = 4;
            this.tblSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblSearch.AutoSize = true;
            this.tblSearch.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.tblSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 65F));
            this.tblSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 65F));
            this.tblSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 190F));

            // lblSearch
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.TabIndex = 14;
            this.lblSearch.Text = "Search";
            this.lblSearch.AutoSize = true;
            this.lblSearch.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);

            // lblStatus
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.TabIndex = 15;
            this.lblStatus.Text = "Status";
            this.lblStatus.AutoSize = true;
            this.lblStatus.Margin = new System.Windows.Forms.Padding(8, 5, 0, 0);
            this.tblSearch.Controls.Add(this.lblSearch, 0, 0);
            this.tblSearch.Controls.Add(this.txtSearch, 1, 0);
            this.tblSearch.Controls.Add(this.lblStatus, 2, 0);
            this.tblSearch.Controls.Add(this.cmbStatus, 3, 0);

            // pnlActions
            this.pnlActions.Name = "pnlActions";
            this.pnlActions.TabIndex = 16;
            this.pnlActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlActions.AutoSize = true;
            this.pnlActions.Padding = new System.Windows.Forms.Padding(0, 8, 0, 4);
            this.pnlActions.WrapContents = true;

            // btnNewRequest
            this.btnNewRequest.Name = "btnNewRequest";
            this.btnNewRequest.TabIndex = 17;
            this.btnNewRequest.Text = "New request";
            this.btnNewRequest.AutoSize = true;
            this.btnNewRequest.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnNewRequest.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnNewRequest.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnNewRequest.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnNewRequest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNewRequest.BackColor = System.Drawing.Color.FromArgb(31, 91, 76);
            this.btnNewRequest.ForeColor = System.Drawing.Color.White;
            this.btnNewRequest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNewRequest.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnNewRequest);

            // btnProcess
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.TabIndex = 18;
            this.btnProcess.Text = "Start processing";
            this.btnProcess.AutoSize = true;
            this.btnProcess.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnProcess.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnProcess.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnProcess.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnProcess.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnProcess.BackColor = System.Drawing.Color.White;
            this.btnProcess.ForeColor = System.Drawing.Color.FromArgb(35, 45, 42);
            this.btnProcess.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnProcess.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnProcess);
            this.btnProcess.Enabled = false;

            // btnReady
            this.btnReady.Name = "btnReady";
            this.btnReady.TabIndex = 19;
            this.btnReady.Text = "Mark ready";
            this.btnReady.AutoSize = true;
            this.btnReady.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnReady.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnReady.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnReady.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnReady.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReady.BackColor = System.Drawing.Color.White;
            this.btnReady.ForeColor = System.Drawing.Color.FromArgb(35, 45, 42);
            this.btnReady.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReady.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnReady);
            this.btnReady.Enabled = false;

            // btnPay
            this.btnPay.Name = "btnPay";
            this.btnPay.TabIndex = 20;
            this.btnPay.Text = "Record payment";
            this.btnPay.AutoSize = true;
            this.btnPay.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPay.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnPay.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnPay.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnPay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPay.BackColor = System.Drawing.Color.White;
            this.btnPay.ForeColor = System.Drawing.Color.FromArgb(35, 45, 42);
            this.btnPay.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPay.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnPay);
            this.btnPay.Enabled = false;

            // btnRelease
            this.btnRelease.Name = "btnRelease";
            this.btnRelease.TabIndex = 21;
            this.btnRelease.Text = "Release";
            this.btnRelease.AutoSize = true;
            this.btnRelease.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnRelease.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnRelease.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnRelease.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnRelease.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRelease.BackColor = System.Drawing.Color.FromArgb(31, 91, 76);
            this.btnRelease.ForeColor = System.Drawing.Color.White;
            this.btnRelease.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRelease.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnRelease);
            this.btnRelease.Enabled = false;

            // btnReject
            this.btnReject.Name = "btnReject";
            this.btnReject.TabIndex = 22;
            this.btnReject.Text = "Reject";
            this.btnReject.AutoSize = true;
            this.btnReject.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnReject.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnReject.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnReject.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnReject.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReject.BackColor = System.Drawing.Color.White;
            this.btnReject.ForeColor = System.Drawing.Color.FromArgb(35, 45, 42);
            this.btnReject.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReject.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnReject);
            this.btnReject.Enabled = false;

            // btnPreview
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.TabIndex = 23;
            this.btnPreview.Text = "View / print";
            this.btnPreview.AutoSize = true;
            this.btnPreview.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPreview.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnPreview.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnPreview.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnPreview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPreview.BackColor = System.Drawing.Color.White;
            this.btnPreview.ForeColor = System.Drawing.Color.FromArgb(35, 45, 42);
            this.btnPreview.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPreview.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnPreview);
            this.btnPreview.Enabled = false;

            // txtDetails
            this.txtDetails.Name = "txtDetails";
            this.txtDetails.TabIndex = 24;
            this.txtDetails.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtDetails.Height = 116;
            this.txtDetails.Multiline = true;
            this.txtDetails.ReadOnly = true;
            this.txtDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDetails.BackColor = System.Drawing.Color.White;
            this.txtDetails.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.gridRequests);
            this.Controls.Add(this.txtDetails);
            this.Controls.Add(this.pnlActions);
            this.Controls.Add(this.tblSearch);
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Padding = new System.Windows.Forms.Padding(16);
            this.BackColor = System.Drawing.Color.FromArgb(242, 246, 244);
            this.Name = "RequestsControl";
            this.Size = new System.Drawing.Size(1000, 600);
            this.btnNewRequest.Click += new System.EventHandler(this.NewRequest);
            this.btnProcess.Click += new System.EventHandler(this.ProcessRequest);
            this.btnReady.Click += new System.EventHandler(this.ReadyRequest);
            this.btnPay.Click += new System.EventHandler(this.RecordPayment);
            this.btnRelease.Click += new System.EventHandler(this.ReleaseRequest);
            this.btnReject.Click += new System.EventHandler(this.RejectRequest);
            this.btnPreview.Click += new System.EventHandler(this.PreviewDocument);
            this.gridRequests.SelectionChanged += new System.EventHandler(this.RequestSelectionChanged);
            this.txtSearch.TextChanged += new System.EventHandler(this.FilterChanged);
            this.cmbStatus.SelectedIndexChanged += new System.EventHandler(this.FilterChanged);
            ((System.ComponentModel.ISupportInitialize)(this.gridRequests)).EndInit();
            this.tblSearch.ResumeLayout(false);
            this.tblSearch.PerformLayout();
            this.pnlActions.ResumeLayout(false);
            this.pnlActions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
