namespace BarangayDocumentSystem.Forms;

partial class MainForm
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
        this.pnlHeader = new System.Windows.Forms.Panel();
        this.lblTitle = new System.Windows.Forms.Label();
        this.lblSubtitle = new System.Windows.Forms.Label();

        this.tabMain = new System.Windows.Forms.TabControl();
        this.tabResidents = new System.Windows.Forms.TabPage();
        this.tabRequests = new System.Windows.Forms.TabPage();
        this.tabDashboard = new System.Windows.Forms.TabPage();

        // Residents tab
        this.dgvResidents = new System.Windows.Forms.DataGridView();
        this.pnlResidentTop = new System.Windows.Forms.Panel();
        this.lblSearch = new System.Windows.Forms.Label();
        this.txtSearch = new System.Windows.Forms.TextBox();
        this.pnlResidentButtons = new System.Windows.Forms.Panel();
        this.btnAddResident = new System.Windows.Forms.Button();
        this.btnEditResident = new System.Windows.Forms.Button();
        this.btnDeleteResident = new System.Windows.Forms.Button();
        this.btnNewRequest = new System.Windows.Forms.Button();

        // Requests tab
        this.dgvRequests = new System.Windows.Forms.DataGridView();
        this.pnlRequestTop = new System.Windows.Forms.Panel();
        this.grpStatusFilter = new System.Windows.Forms.GroupBox();
        this.radAll = new System.Windows.Forms.RadioButton();
        this.radPending = new System.Windows.Forms.RadioButton();
        this.radProcessing = new System.Windows.Forms.RadioButton();
        this.radReady = new System.Windows.Forms.RadioButton();
        this.radReleased = new System.Windows.Forms.RadioButton();
        this.pnlRequestButtons = new System.Windows.Forms.Panel();
        this.btnProcess = new System.Windows.Forms.Button();
        this.btnReady = new System.Windows.Forms.Button();
        this.btnPay = new System.Windows.Forms.Button();
        this.btnRelease = new System.Windows.Forms.Button();
        this.btnReject = new System.Windows.Forms.Button();
        this.btnPrint = new System.Windows.Forms.Button();

        // Dashboard tab
        this.lblStats = new System.Windows.Forms.Label();
        this.txtStats = new System.Windows.Forms.TextBox();

        this.statusStrip = new System.Windows.Forms.StatusStrip();
        this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();

        this.pnlHeader.SuspendLayout();
        this.tabMain.SuspendLayout();
        this.tabResidents.SuspendLayout();
        this.tabRequests.SuspendLayout();
        this.tabDashboard.SuspendLayout();
        this.pnlResidentTop.SuspendLayout();
        this.pnlResidentButtons.SuspendLayout();
        this.pnlRequestTop.SuspendLayout();
        this.grpStatusFilter.SuspendLayout();
        this.pnlRequestButtons.SuspendLayout();
        this.statusStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.dgvResidents)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dgvRequests)).BeginInit();
        this.SuspendLayout();

        // ============================== header ============================
        this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(21, 71, 52);
        this.pnlHeader.Controls.Add(this.lblSubtitle);
        this.pnlHeader.Controls.Add(this.lblTitle);
        this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
        this.pnlHeader.Location = new System.Drawing.Point(0, 0);
        this.pnlHeader.Name = "pnlHeader";
        this.pnlHeader.Size = new System.Drawing.Size(1120, 74);
        this.pnlHeader.TabIndex = 0;

        this.lblTitle.AutoSize = true;
        this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold);
        this.lblTitle.ForeColor = System.Drawing.Color.White;
        this.lblTitle.Location = new System.Drawing.Point(18, 12);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Size = new System.Drawing.Size(553, 37);
        this.lblTitle.TabIndex = 0;
        this.lblTitle.Text = "Barangay Resident && Document Request System";

        this.lblSubtitle.AutoSize = true;
        this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 10F);
        this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(180, 215, 198);
        this.lblSubtitle.Location = new System.Drawing.Point(22, 48);
        this.lblSubtitle.Name = "lblSubtitle";
        this.lblSubtitle.Size = new System.Drawing.Size(419, 23);
        this.lblSubtitle.TabIndex = 1;
        this.lblSubtitle.Text = "Barangay Magugpo Poblacion • City of Tagum • Davao del Norte";

        // ============================== tabMain ===========================
        this.tabMain.Controls.Add(this.tabResidents);
        this.tabMain.Controls.Add(this.tabRequests);
        this.tabMain.Controls.Add(this.tabDashboard);
        this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
        this.tabMain.Location = new System.Drawing.Point(0, 74);
        this.tabMain.Name = "tabMain";
        this.tabMain.Padding = new System.Drawing.Point(14, 6);
        this.tabMain.SelectedIndex = 0;
        this.tabMain.Size = new System.Drawing.Size(1120, 622);
        this.tabMain.TabIndex = 1;

        // ============================ tabResidents ========================
        this.tabResidents.Controls.Add(this.dgvResidents);
        this.tabResidents.Controls.Add(this.pnlResidentButtons);
        this.tabResidents.Controls.Add(this.pnlResidentTop);
        this.tabResidents.Location = new System.Drawing.Point(4, 29);
        this.tabResidents.Name = "tabResidents";
        this.tabResidents.Padding = new System.Windows.Forms.Padding(10);
        this.tabResidents.Size = new System.Drawing.Size(1112, 589);
        this.tabResidents.TabIndex = 0;
        this.tabResidents.Text = "Residents";
        this.tabResidents.UseVisualStyleBackColor = true;

        this.pnlResidentTop.Controls.Add(this.txtSearch);
        this.pnlResidentTop.Controls.Add(this.lblSearch);
        this.pnlResidentTop.Dock = System.Windows.Forms.DockStyle.Top;
        this.pnlResidentTop.Location = new System.Drawing.Point(10, 10);
        this.pnlResidentTop.Name = "pnlResidentTop";
        this.pnlResidentTop.Size = new System.Drawing.Size(1092, 46);
        this.pnlResidentTop.TabIndex = 0;

        this.lblSearch.AutoSize = true;
        this.lblSearch.Location = new System.Drawing.Point(3, 12);
        this.lblSearch.Name = "lblSearch";
        this.lblSearch.Size = new System.Drawing.Size(51, 20);
        this.lblSearch.TabIndex = 0;
        this.lblSearch.Text = "Search:";

        this.txtSearch.Location = new System.Drawing.Point(60, 8);
        this.txtSearch.Name = "txtSearch";
        this.txtSearch.PlaceholderText = "name, purok, or contact number";
        this.txtSearch.Size = new System.Drawing.Size(420, 27);
        this.txtSearch.TabIndex = 1;
        this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);

        this.dgvResidents.AllowUserToAddRows = false;
        this.dgvResidents.AllowUserToDeleteRows = false;
        this.dgvResidents.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this.dgvResidents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvResidents.Dock = System.Windows.Forms.DockStyle.Fill;
        this.dgvResidents.Location = new System.Drawing.Point(10, 56);
        this.dgvResidents.MultiSelect = false;
        this.dgvResidents.Name = "dgvResidents";
        this.dgvResidents.ReadOnly = true;
        this.dgvResidents.RowHeadersVisible = false;
        this.dgvResidents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.dgvResidents.Size = new System.Drawing.Size(1092, 463);
        this.dgvResidents.TabIndex = 1;
        this.dgvResidents.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvResidents_CellDoubleClick);

        this.pnlResidentButtons.Controls.Add(this.btnNewRequest);
        this.pnlResidentButtons.Controls.Add(this.btnDeleteResident);
        this.pnlResidentButtons.Controls.Add(this.btnEditResident);
        this.pnlResidentButtons.Controls.Add(this.btnAddResident);
        this.pnlResidentButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.pnlResidentButtons.Location = new System.Drawing.Point(10, 519);
        this.pnlResidentButtons.Name = "pnlResidentButtons";
        this.pnlResidentButtons.Size = new System.Drawing.Size(1092, 60);
        this.pnlResidentButtons.TabIndex = 2;

        this.btnAddResident.Location = new System.Drawing.Point(3, 12);
        this.btnAddResident.Name = "btnAddResident";
        this.btnAddResident.Size = new System.Drawing.Size(150, 38);
        this.btnAddResident.TabIndex = 0;
        this.btnAddResident.Text = "Register Resident";
        this.btnAddResident.UseVisualStyleBackColor = true;
        this.btnAddResident.Click += new System.EventHandler(this.btnAddResident_Click);

        this.btnEditResident.Location = new System.Drawing.Point(159, 12);
        this.btnEditResident.Name = "btnEditResident";
        this.btnEditResident.Size = new System.Drawing.Size(120, 38);
        this.btnEditResident.TabIndex = 1;
        this.btnEditResident.Text = "Edit";
        this.btnEditResident.UseVisualStyleBackColor = true;
        this.btnEditResident.Click += new System.EventHandler(this.btnEditResident_Click);

        this.btnDeleteResident.Location = new System.Drawing.Point(285, 12);
        this.btnDeleteResident.Name = "btnDeleteResident";
        this.btnDeleteResident.Size = new System.Drawing.Size(120, 38);
        this.btnDeleteResident.TabIndex = 2;
        this.btnDeleteResident.Text = "Delete";
        this.btnDeleteResident.UseVisualStyleBackColor = true;
        this.btnDeleteResident.Click += new System.EventHandler(this.btnDeleteResident_Click);

        this.btnNewRequest.BackColor = System.Drawing.Color.FromArgb(21, 71, 52);
        this.btnNewRequest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnNewRequest.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
        this.btnNewRequest.ForeColor = System.Drawing.Color.White;
        this.btnNewRequest.Location = new System.Drawing.Point(430, 12);
        this.btnNewRequest.Name = "btnNewRequest";
        this.btnNewRequest.Size = new System.Drawing.Size(210, 38);
        this.btnNewRequest.TabIndex = 3;
        this.btnNewRequest.Text = "New Document Request";
        this.btnNewRequest.UseVisualStyleBackColor = false;
        this.btnNewRequest.Click += new System.EventHandler(this.btnNewRequest_Click);

        // ============================= tabRequests ========================
        this.tabRequests.Controls.Add(this.dgvRequests);
        this.tabRequests.Controls.Add(this.pnlRequestButtons);
        this.tabRequests.Controls.Add(this.pnlRequestTop);
        this.tabRequests.Location = new System.Drawing.Point(4, 29);
        this.tabRequests.Name = "tabRequests";
        this.tabRequests.Padding = new System.Windows.Forms.Padding(10);
        this.tabRequests.Size = new System.Drawing.Size(1112, 589);
        this.tabRequests.TabIndex = 1;
        this.tabRequests.Text = "Document Requests";
        this.tabRequests.UseVisualStyleBackColor = true;

        this.pnlRequestTop.Controls.Add(this.grpStatusFilter);
        this.pnlRequestTop.Dock = System.Windows.Forms.DockStyle.Top;
        this.pnlRequestTop.Location = new System.Drawing.Point(10, 10);
        this.pnlRequestTop.Name = "pnlRequestTop";
        this.pnlRequestTop.Size = new System.Drawing.Size(1092, 64);
        this.pnlRequestTop.TabIndex = 0;

        // GroupBox scopes these radios into ONE mutually exclusive group.
        this.grpStatusFilter.Controls.Add(this.radReleased);
        this.grpStatusFilter.Controls.Add(this.radReady);
        this.grpStatusFilter.Controls.Add(this.radProcessing);
        this.grpStatusFilter.Controls.Add(this.radPending);
        this.grpStatusFilter.Controls.Add(this.radAll);
        this.grpStatusFilter.Location = new System.Drawing.Point(3, 3);
        this.grpStatusFilter.Name = "grpStatusFilter";
        this.grpStatusFilter.Size = new System.Drawing.Size(700, 56);
        this.grpStatusFilter.TabIndex = 0;
        this.grpStatusFilter.TabStop = false;
        this.grpStatusFilter.Text = "Filter by status";

        this.radAll.AutoSize = true;
        this.radAll.Checked = true;
        this.radAll.Location = new System.Drawing.Point(16, 24);
        this.radAll.Name = "radAll";
        this.radAll.Size = new System.Drawing.Size(48, 24);
        this.radAll.TabIndex = 0;
        this.radAll.TabStop = true;
        this.radAll.Text = "All";
        this.radAll.UseVisualStyleBackColor = true;
        this.radAll.CheckedChanged += new System.EventHandler(this.StatusFilter_CheckedChanged);

        this.radPending.AutoSize = true;
        this.radPending.Location = new System.Drawing.Point(80, 24);
        this.radPending.Name = "radPending";
        this.radPending.Size = new System.Drawing.Size(83, 24);
        this.radPending.TabIndex = 1;
        this.radPending.Text = "Pending";
        this.radPending.UseVisualStyleBackColor = true;
        this.radPending.CheckedChanged += new System.EventHandler(this.StatusFilter_CheckedChanged);

        this.radProcessing.AutoSize = true;
        this.radProcessing.Location = new System.Drawing.Point(180, 24);
        this.radProcessing.Name = "radProcessing";
        this.radProcessing.Size = new System.Drawing.Size(105, 24);
        this.radProcessing.TabIndex = 2;
        this.radProcessing.Text = "Processing";
        this.radProcessing.UseVisualStyleBackColor = true;
        this.radProcessing.CheckedChanged += new System.EventHandler(this.StatusFilter_CheckedChanged);

        this.radReady.AutoSize = true;
        this.radReady.Location = new System.Drawing.Point(300, 24);
        this.radReady.Name = "radReady";
        this.radReady.Size = new System.Drawing.Size(146, 24);
        this.radReady.TabIndex = 3;
        this.radReady.Text = "Ready for Release";
        this.radReady.UseVisualStyleBackColor = true;
        this.radReady.CheckedChanged += new System.EventHandler(this.StatusFilter_CheckedChanged);

        this.radReleased.AutoSize = true;
        this.radReleased.Location = new System.Drawing.Point(462, 24);
        this.radReleased.Name = "radReleased";
        this.radReleased.Size = new System.Drawing.Size(88, 24);
        this.radReleased.TabIndex = 4;
        this.radReleased.Text = "Released";
        this.radReleased.UseVisualStyleBackColor = true;
        this.radReleased.CheckedChanged += new System.EventHandler(this.StatusFilter_CheckedChanged);

        this.dgvRequests.AllowUserToAddRows = false;
        this.dgvRequests.AllowUserToDeleteRows = false;
        this.dgvRequests.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this.dgvRequests.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvRequests.Dock = System.Windows.Forms.DockStyle.Fill;
        this.dgvRequests.Location = new System.Drawing.Point(10, 74);
        this.dgvRequests.MultiSelect = false;
        this.dgvRequests.Name = "dgvRequests";
        this.dgvRequests.ReadOnly = true;
        this.dgvRequests.RowHeadersVisible = false;
        this.dgvRequests.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.dgvRequests.Size = new System.Drawing.Size(1092, 445);
        this.dgvRequests.TabIndex = 1;

        this.pnlRequestButtons.Controls.Add(this.btnPrint);
        this.pnlRequestButtons.Controls.Add(this.btnReject);
        this.pnlRequestButtons.Controls.Add(this.btnRelease);
        this.pnlRequestButtons.Controls.Add(this.btnPay);
        this.pnlRequestButtons.Controls.Add(this.btnReady);
        this.pnlRequestButtons.Controls.Add(this.btnProcess);
        this.pnlRequestButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.pnlRequestButtons.Location = new System.Drawing.Point(10, 519);
        this.pnlRequestButtons.Name = "pnlRequestButtons";
        this.pnlRequestButtons.Size = new System.Drawing.Size(1092, 60);
        this.pnlRequestButtons.TabIndex = 2;

        this.btnProcess.Location = new System.Drawing.Point(3, 12);
        this.btnProcess.Name = "btnProcess";
        this.btnProcess.Size = new System.Drawing.Size(140, 38);
        this.btnProcess.TabIndex = 0;
        this.btnProcess.Text = "Start Processing";
        this.btnProcess.UseVisualStyleBackColor = true;
        this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);

        this.btnReady.Location = new System.Drawing.Point(149, 12);
        this.btnReady.Name = "btnReady";
        this.btnReady.Size = new System.Drawing.Size(140, 38);
        this.btnReady.TabIndex = 1;
        this.btnReady.Text = "Mark Ready";
        this.btnReady.UseVisualStyleBackColor = true;
        this.btnReady.Click += new System.EventHandler(this.btnReady_Click);

        this.btnPay.Location = new System.Drawing.Point(295, 12);
        this.btnPay.Name = "btnPay";
        this.btnPay.Size = new System.Drawing.Size(140, 38);
        this.btnPay.TabIndex = 2;
        this.btnPay.Text = "Record Payment";
        this.btnPay.UseVisualStyleBackColor = true;
        this.btnPay.Click += new System.EventHandler(this.btnPay_Click);

        this.btnRelease.BackColor = System.Drawing.Color.FromArgb(21, 71, 52);
        this.btnRelease.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnRelease.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
        this.btnRelease.ForeColor = System.Drawing.Color.White;
        this.btnRelease.Location = new System.Drawing.Point(441, 12);
        this.btnRelease.Name = "btnRelease";
        this.btnRelease.Size = new System.Drawing.Size(140, 38);
        this.btnRelease.TabIndex = 3;
        this.btnRelease.Text = "Release";
        this.btnRelease.UseVisualStyleBackColor = false;
        this.btnRelease.Click += new System.EventHandler(this.btnRelease_Click);

        this.btnReject.Location = new System.Drawing.Point(587, 12);
        this.btnReject.Name = "btnReject";
        this.btnReject.Size = new System.Drawing.Size(120, 38);
        this.btnReject.TabIndex = 4;
        this.btnReject.Text = "Reject";
        this.btnReject.UseVisualStyleBackColor = true;
        this.btnReject.Click += new System.EventHandler(this.btnReject_Click);

        this.btnPrint.Location = new System.Drawing.Point(730, 12);
        this.btnPrint.Name = "btnPrint";
        this.btnPrint.Size = new System.Drawing.Size(180, 38);
        this.btnPrint.TabIndex = 5;
        this.btnPrint.Text = "View / Print Document";
        this.btnPrint.UseVisualStyleBackColor = true;
        this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);

        // ============================ tabDashboard ========================
        this.tabDashboard.Controls.Add(this.txtStats);
        this.tabDashboard.Controls.Add(this.lblStats);
        this.tabDashboard.Location = new System.Drawing.Point(4, 29);
        this.tabDashboard.Name = "tabDashboard";
        this.tabDashboard.Padding = new System.Windows.Forms.Padding(10);
        this.tabDashboard.Size = new System.Drawing.Size(1112, 589);
        this.tabDashboard.TabIndex = 2;
        this.tabDashboard.Text = "Dashboard";
        this.tabDashboard.UseVisualStyleBackColor = true;

        this.lblStats.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblStats.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblStats.Location = new System.Drawing.Point(10, 10);
        this.lblStats.Name = "lblStats";
        this.lblStats.Size = new System.Drawing.Size(1092, 40);
        this.lblStats.TabIndex = 0;
        this.lblStats.Text = "Barangay Statistics";

        this.txtStats.BackColor = System.Drawing.Color.White;
        this.txtStats.Dock = System.Windows.Forms.DockStyle.Fill;
        this.txtStats.Font = new System.Drawing.Font("Consolas", 11F);
        this.txtStats.Location = new System.Drawing.Point(10, 50);
        this.txtStats.Multiline = true;
        this.txtStats.Name = "txtStats";
        this.txtStats.ReadOnly = true;
        this.txtStats.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtStats.Size = new System.Drawing.Size(1092, 529);
        this.txtStats.TabIndex = 1;

        // ============================= statusStrip ========================
        this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.lblStatus });
        this.statusStrip.Location = new System.Drawing.Point(0, 696);
        this.statusStrip.Name = "statusStrip";
        this.statusStrip.Size = new System.Drawing.Size(1120, 26);
        this.statusStrip.TabIndex = 2;

        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(60, 20);
        this.lblStatus.Text = "Ready";

        // =============================== MainForm =========================
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1120, 722);
        this.Controls.Add(this.tabMain);
        this.Controls.Add(this.pnlHeader);
        this.Controls.Add(this.statusStrip);
        this.MinimumSize = new System.Drawing.Size(1000, 700);
        this.Name = "MainForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Barangay Resident and Document Request Management System";
        this.Load += new System.EventHandler(this.MainForm_Load);

        this.pnlHeader.ResumeLayout(false);
        this.pnlHeader.PerformLayout();
        this.tabMain.ResumeLayout(false);
        this.tabResidents.ResumeLayout(false);
        this.tabRequests.ResumeLayout(false);
        this.tabDashboard.ResumeLayout(false);
        this.tabDashboard.PerformLayout();
        this.pnlResidentTop.ResumeLayout(false);
        this.pnlResidentTop.PerformLayout();
        this.pnlResidentButtons.ResumeLayout(false);
        this.pnlRequestTop.ResumeLayout(false);
        this.grpStatusFilter.ResumeLayout(false);
        this.grpStatusFilter.PerformLayout();
        this.pnlRequestButtons.ResumeLayout(false);
        this.statusStrip.ResumeLayout(false);
        this.statusStrip.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.dgvResidents)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dgvRequests)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Panel pnlHeader;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.Label lblSubtitle;

    private System.Windows.Forms.TabControl tabMain;
    private System.Windows.Forms.TabPage tabResidents;
    private System.Windows.Forms.TabPage tabRequests;
    private System.Windows.Forms.TabPage tabDashboard;

    private System.Windows.Forms.DataGridView dgvResidents;
    private System.Windows.Forms.Panel pnlResidentTop;
    private System.Windows.Forms.Label lblSearch;
    private System.Windows.Forms.TextBox txtSearch;
    private System.Windows.Forms.Panel pnlResidentButtons;
    private System.Windows.Forms.Button btnAddResident;
    private System.Windows.Forms.Button btnEditResident;
    private System.Windows.Forms.Button btnDeleteResident;
    private System.Windows.Forms.Button btnNewRequest;

    private System.Windows.Forms.DataGridView dgvRequests;
    private System.Windows.Forms.Panel pnlRequestTop;
    private System.Windows.Forms.GroupBox grpStatusFilter;
    private System.Windows.Forms.RadioButton radAll;
    private System.Windows.Forms.RadioButton radPending;
    private System.Windows.Forms.RadioButton radProcessing;
    private System.Windows.Forms.RadioButton radReady;
    private System.Windows.Forms.RadioButton radReleased;
    private System.Windows.Forms.Panel pnlRequestButtons;
    private System.Windows.Forms.Button btnProcess;
    private System.Windows.Forms.Button btnReady;
    private System.Windows.Forms.Button btnPay;
    private System.Windows.Forms.Button btnRelease;
    private System.Windows.Forms.Button btnReject;
    private System.Windows.Forms.Button btnPrint;

    private System.Windows.Forms.Label lblStats;
    private System.Windows.Forms.TextBox txtStats;

    private System.Windows.Forms.StatusStrip statusStrip;
    private System.Windows.Forms.ToolStripStatusLabel lblStatus;
}
