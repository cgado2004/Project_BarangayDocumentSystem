namespace BarangayDocumentSystem.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel pnlSidebar;
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.Panel pnlBody;
        private System.Windows.Forms.Label lblBarangay;
        private System.Windows.Forms.Label lblPageTitle;
        private System.Windows.Forms.Label lblSession;
        private System.Windows.Forms.Button btnDashboard;
        private System.Windows.Forms.Button btnResidents;
        private System.Windows.Forms.Button btnRequests;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlSidebar = new System.Windows.Forms.Panel();
            this.pnlBody = new System.Windows.Forms.Panel();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.lblBarangay = new System.Windows.Forms.Label();
            this.lblPageTitle = new System.Windows.Forms.Label();
            this.lblSession = new System.Windows.Forms.Label();
            this.btnDashboard = new System.Windows.Forms.Button();
            this.btnResidents = new System.Windows.Forms.Button();
            this.btnRequests = new System.Windows.Forms.Button();
            this.SuspendLayout();
            this.pnlSidebar.SuspendLayout();
            this.pnlBody.SuspendLayout();
            this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(31, 74, 64);
            this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSidebar.Width = 210;
            this.pnlSidebar.Padding = new System.Windows.Forms.Padding(12);
            this.lblBarangay.Text = "BARANGAY\r\nDocument System";
            this.lblBarangay.ForeColor = System.Drawing.Color.White;
            this.lblBarangay.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblBarangay.Height = 132;
            this.lblBarangay.Padding = new System.Windows.Forms.Padding(6, 20, 6, 12);
            this.lblBarangay.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnDashboard.Text = "Dashboard";
            this.btnDashboard.Name = "btnDashboard";
            this.btnDashboard.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnDashboard.Height = 52;
            this.btnDashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDashboard.FlatAppearance.BorderSize = 0;
            this.btnDashboard.ForeColor = System.Drawing.Color.White;
            this.btnDashboard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDashboard.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.btnDashboard.Click += new System.EventHandler(this.NavigateDashboard);
            this.btnResidents.Text = "Residents";
            this.btnResidents.Name = "btnResidents";
            this.btnResidents.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnResidents.Height = 52;
            this.btnResidents.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResidents.FlatAppearance.BorderSize = 0;
            this.btnResidents.ForeColor = System.Drawing.Color.White;
            this.btnResidents.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnResidents.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.btnResidents.Click += new System.EventHandler(this.NavigateResidents);
            this.btnRequests.Text = "Document requests";
            this.btnRequests.Name = "btnRequests";
            this.btnRequests.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnRequests.Height = 52;
            this.btnRequests.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRequests.FlatAppearance.BorderSize = 0;
            this.btnRequests.ForeColor = System.Drawing.Color.White;
            this.btnRequests.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnRequests.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.btnRequests.Click += new System.EventHandler(this.NavigateRequests);
            this.pnlSidebar.Controls.Add(this.btnRequests);
            this.pnlSidebar.Controls.Add(this.btnResidents);
            this.pnlSidebar.Controls.Add(this.btnDashboard);
            this.pnlSidebar.Controls.Add(this.lblBarangay);
            this.pnlBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBody.BackColor = System.Drawing.Color.FromArgb(242, 246, 244);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPageTitle.Text = "Dashboard";
            this.lblPageTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPageTitle.Height = 72;
            this.lblPageTitle.Padding = new System.Windows.Forms.Padding(20, 12, 0, 0);
            this.lblPageTitle.Font = new System.Drawing.Font("Segoe UI", 23F);
            this.lblPageTitle.ForeColor = System.Drawing.Color.FromArgb(31, 74, 64);
            this.lblSession.Text = "Session-only records";
            this.lblSession.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblSession.Height = 48;
            this.lblSession.Padding = new System.Windows.Forms.Padding(20, 8, 12, 0);
            this.lblSession.BackColor = System.Drawing.Color.FromArgb(228, 238, 231);
            this.pnlBody.Controls.Add(this.pnlContent);
            this.pnlBody.Controls.Add(this.lblPageTitle);
            this.pnlBody.Controls.Add(this.lblSession);
            this.Controls.Add(this.pnlBody);
            this.Controls.Add(this.pnlSidebar);
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 780);
            this.MinimumSize = new System.Drawing.Size(1120, 700);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Name = "MainForm";
            this.Text = "Barangay Resident and Document Request Management System";
            this.FormClosing += MainFormClosing;
            this.pnlSidebar.ResumeLayout(false);
            this.pnlBody.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
