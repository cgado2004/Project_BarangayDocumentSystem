namespace BarangayDocumentSystem.App;

/// <summary>
/// The designer half of my main form.
///
/// I keep every control declaration and every layout instruction in this file,
/// exactly the way Visual Studio expects. That matters for two reasons: the
/// form opens properly in the WinForms designer so my group-mates can see and
/// drag the controls, and Form1.cs is left holding only the behaviour.
/// </summary>
partial class Form1
{
    /// <summary>The designer's container for components that need disposing.</summary>
    private System.ComponentModel.IContainer components = null;

    // ---- navigation rail ----
    private System.Windows.Forms.Panel pnlSidebar;
    private System.Windows.Forms.PictureBox picLogo;
    private System.Windows.Forms.Label lblBrandTop;
    private System.Windows.Forms.Label lblBrandSub;
    private BarangayDocumentSystem.App.Controls.NavButton btnNavDashboard;
    private BarangayDocumentSystem.App.Controls.NavButton btnNavResidents;
    private BarangayDocumentSystem.App.Controls.NavButton btnNavRequests;
    private System.Windows.Forms.Label lblSidebarFooter;

    // ---- header ----
    private System.Windows.Forms.Panel pnlHeader;
    private System.Windows.Forms.Label lblPageTitle;
    private System.Windows.Forms.Label lblPageSubtitle;

    // ---- content host ----
    private System.Windows.Forms.Panel pnlContent;

    // ---- dashboard ----
    private System.Windows.Forms.Panel pnlDashboard;
    private BarangayDocumentSystem.App.Controls.HeroBanner heroBanner;
    private System.Windows.Forms.FlowLayoutPanel flowStats;
    private System.Windows.Forms.TableLayoutPanel tblBreakdown;
    private BarangayDocumentSystem.App.Controls.Card cardPurok;
    private System.Windows.Forms.Label lblPurokTitle;
    private System.Windows.Forms.FlowLayoutPanel flowPuroks;
    private BarangayDocumentSystem.App.Controls.Card cardDocTypes;
    private System.Windows.Forms.Label lblDocTypesTitle;
    private System.Windows.Forms.FlowLayoutPanel flowDocTypes;

    // ---- status bar ----
    private System.Windows.Forms.Panel pnlStatus;
    private System.Windows.Forms.Label lblStatus;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// I lay the whole window out here. Visual Studio reads this method when
    /// it draws the designer surface, so everything visible is declared in one
    /// readable place.
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();

        this.pnlSidebar = new System.Windows.Forms.Panel();
        this.picLogo = new System.Windows.Forms.PictureBox();
        this.lblBrandTop = new System.Windows.Forms.Label();
        this.lblBrandSub = new System.Windows.Forms.Label();
        this.btnNavDashboard = new BarangayDocumentSystem.App.Controls.NavButton();
        this.btnNavResidents = new BarangayDocumentSystem.App.Controls.NavButton();
        this.btnNavRequests = new BarangayDocumentSystem.App.Controls.NavButton();
        this.lblSidebarFooter = new System.Windows.Forms.Label();

        this.pnlHeader = new System.Windows.Forms.Panel();
        this.lblPageTitle = new System.Windows.Forms.Label();
        this.lblPageSubtitle = new System.Windows.Forms.Label();

        this.pnlContent = new System.Windows.Forms.Panel();
        this.pnlDashboard = new System.Windows.Forms.Panel();
        this.heroBanner = new BarangayDocumentSystem.App.Controls.HeroBanner();
        this.flowStats = new System.Windows.Forms.FlowLayoutPanel();
        this.tblBreakdown = new System.Windows.Forms.TableLayoutPanel();
        this.cardPurok = new BarangayDocumentSystem.App.Controls.Card();
        this.lblPurokTitle = new System.Windows.Forms.Label();
        this.flowPuroks = new System.Windows.Forms.FlowLayoutPanel();
        this.cardDocTypes = new BarangayDocumentSystem.App.Controls.Card();
        this.lblDocTypesTitle = new System.Windows.Forms.Label();
        this.flowDocTypes = new System.Windows.Forms.FlowLayoutPanel();

        this.pnlStatus = new System.Windows.Forms.Panel();
        this.lblStatus = new System.Windows.Forms.Label();

        ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
        this.pnlSidebar.SuspendLayout();
        this.pnlHeader.SuspendLayout();
        this.pnlContent.SuspendLayout();
        this.pnlDashboard.SuspendLayout();
        this.tblBreakdown.SuspendLayout();
        this.cardPurok.SuspendLayout();
        this.cardDocTypes.SuspendLayout();
        this.pnlStatus.SuspendLayout();
        this.SuspendLayout();

        //
        // picLogo - the barangay seal, top of the rail
        //
        this.picLogo.Name = "picLogo";
        this.picLogo.Location = new System.Drawing.Point(24, 26);
        this.picLogo.Size = new System.Drawing.Size(64, 64);
        this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.picLogo.BackColor = System.Drawing.Color.Transparent;
        this.picLogo.TabStop = false;

        //
        // lblBrandTop
        //
        this.lblBrandTop.Name = "lblBrandTop";
        this.lblBrandTop.AutoSize = false;
        this.lblBrandTop.Location = new System.Drawing.Point(98, 34);
        this.lblBrandTop.Size = new System.Drawing.Size(140, 24);
        this.lblBrandTop.Text = "BARANGAY";
        this.lblBrandTop.BackColor = System.Drawing.Color.Transparent;

        //
        // lblBrandSub
        //
        this.lblBrandSub.Name = "lblBrandSub";
        this.lblBrandSub.AutoSize = false;
        this.lblBrandSub.Location = new System.Drawing.Point(98, 56);
        this.lblBrandSub.Size = new System.Drawing.Size(140, 36);
        this.lblBrandSub.Text = "Magugpo\r\nPoblacion";
        this.lblBrandSub.BackColor = System.Drawing.Color.Transparent;

        //
        // btnNavDashboard
        //
        this.btnNavDashboard.Name = "btnNavDashboard";
        this.btnNavDashboard.Location = new System.Drawing.Point(14, 118);
        this.btnNavDashboard.Size = new System.Drawing.Size(220, 48);
        this.btnNavDashboard.Text = "Dashboard";
        this.btnNavDashboard.Glyph = "\u25A6";
        this.btnNavDashboard.Active = true;
        this.btnNavDashboard.Anchor = ((System.Windows.Forms.AnchorStyles)(
            System.Windows.Forms.AnchorStyles.Top
            | System.Windows.Forms.AnchorStyles.Left
            | System.Windows.Forms.AnchorStyles.Right));
        this.btnNavDashboard.Click += new System.EventHandler(this.btnNavDashboard_Click);

        //
        // btnNavResidents
        //
        this.btnNavResidents.Name = "btnNavResidents";
        this.btnNavResidents.Location = new System.Drawing.Point(14, 172);
        this.btnNavResidents.Size = new System.Drawing.Size(220, 48);
        this.btnNavResidents.Text = "Residents";
        this.btnNavResidents.Glyph = "\u25C9";
        this.btnNavResidents.Anchor = ((System.Windows.Forms.AnchorStyles)(
            System.Windows.Forms.AnchorStyles.Top
            | System.Windows.Forms.AnchorStyles.Left
            | System.Windows.Forms.AnchorStyles.Right));
        this.btnNavResidents.Click += new System.EventHandler(this.btnNavResidents_Click);

        //
        // btnNavRequests
        //
        this.btnNavRequests.Name = "btnNavRequests";
        this.btnNavRequests.Location = new System.Drawing.Point(14, 226);
        this.btnNavRequests.Size = new System.Drawing.Size(220, 48);
        this.btnNavRequests.Text = "Requests";
        this.btnNavRequests.Glyph = "\u25A4";
        this.btnNavRequests.Anchor = ((System.Windows.Forms.AnchorStyles)(
            System.Windows.Forms.AnchorStyles.Top
            | System.Windows.Forms.AnchorStyles.Left
            | System.Windows.Forms.AnchorStyles.Right));
        this.btnNavRequests.Click += new System.EventHandler(this.btnNavRequests_Click);

        //
        // lblSidebarFooter
        //
        this.lblSidebarFooter.Name = "lblSidebarFooter";
        this.lblSidebarFooter.AutoSize = false;
        this.lblSidebarFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.lblSidebarFooter.Height = 56;
        this.lblSidebarFooter.Padding = new System.Windows.Forms.Padding(24, 0, 12, 0);
        this.lblSidebarFooter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.lblSidebarFooter.Text = "City of Tagum\r\nDavao del Norte";
        this.lblSidebarFooter.BackColor = System.Drawing.Color.Transparent;

        //
        // pnlSidebar
        //
        this.pnlSidebar.Name = "pnlSidebar";
        this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Left;
        this.pnlSidebar.Width = 248;
        this.pnlSidebar.Controls.Add(this.lblSidebarFooter);
        this.pnlSidebar.Controls.Add(this.btnNavRequests);
        this.pnlSidebar.Controls.Add(this.btnNavResidents);
        this.pnlSidebar.Controls.Add(this.btnNavDashboard);
        this.pnlSidebar.Controls.Add(this.lblBrandSub);
        this.pnlSidebar.Controls.Add(this.lblBrandTop);
        this.pnlSidebar.Controls.Add(this.picLogo);

        //
        // lblPageTitle
        //
        this.lblPageTitle.Name = "lblPageTitle";
        this.lblPageTitle.AutoSize = false;
        this.lblPageTitle.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblPageTitle.Height = 42;
        this.lblPageTitle.Text = "Dashboard";
        this.lblPageTitle.BackColor = System.Drawing.Color.Transparent;

        //
        // lblPageSubtitle
        //
        this.lblPageSubtitle.Name = "lblPageSubtitle";
        this.lblPageSubtitle.AutoSize = false;
        this.lblPageSubtitle.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblPageSubtitle.Height = 24;
        this.lblPageSubtitle.Text = "Live figures for the barangay office";
        this.lblPageSubtitle.BackColor = System.Drawing.Color.Transparent;

        //
        // pnlHeader
        //
        this.pnlHeader.Name = "pnlHeader";
        this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
        this.pnlHeader.Height = 82;
        this.pnlHeader.Padding = new System.Windows.Forms.Padding(28, 18, 28, 0);
        this.pnlHeader.BackColor = System.Drawing.Color.Transparent;
        this.pnlHeader.Controls.Add(this.lblPageSubtitle);
        this.pnlHeader.Controls.Add(this.lblPageTitle);

        //
        // heroBanner - the seal, the barangay name, the Punong Barangay
        //
        this.heroBanner.Name = "heroBanner";
        this.heroBanner.Dock = System.Windows.Forms.DockStyle.Top;
        this.heroBanner.Height = 158;
        this.heroBanner.Margin = new System.Windows.Forms.Padding(0, 0, 0, 18);

        //
        // flowStats - the clickable statistic cards
        //
        this.flowStats.Name = "flowStats";
        this.flowStats.Dock = System.Windows.Forms.DockStyle.Top;
        this.flowStats.AutoSize = true;
        this.flowStats.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.flowStats.WrapContents = true;
        this.flowStats.BackColor = System.Drawing.Color.Transparent;
        this.flowStats.Padding = new System.Windows.Forms.Padding(0, 18, 0, 0);

        //
        // lblPurokTitle
        //
        this.lblPurokTitle.Name = "lblPurokTitle";
        this.lblPurokTitle.AutoSize = false;
        this.lblPurokTitle.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblPurokTitle.Height = 32;
        this.lblPurokTitle.Text = "Residents by purok — click one to filter";
        this.lblPurokTitle.BackColor = System.Drawing.Color.Transparent;

        //
        // flowPuroks
        //
        this.flowPuroks.Name = "flowPuroks";
        this.flowPuroks.Dock = System.Windows.Forms.DockStyle.Fill;
        this.flowPuroks.AutoScroll = true;
        this.flowPuroks.WrapContents = true;
        this.flowPuroks.BackColor = System.Drawing.Color.Transparent;

        //
        // cardPurok
        //
        this.cardPurok.Name = "cardPurok";
        this.cardPurok.Dock = System.Windows.Forms.DockStyle.Fill;
        this.cardPurok.Margin = new System.Windows.Forms.Padding(0, 0, 9, 0);
        this.cardPurok.Controls.Add(this.flowPuroks);
        this.cardPurok.Controls.Add(this.lblPurokTitle);

        //
        // lblDocTypesTitle
        //
        this.lblDocTypesTitle.Name = "lblDocTypesTitle";
        this.lblDocTypesTitle.AutoSize = false;
        this.lblDocTypesTitle.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblDocTypesTitle.Height = 32;
        this.lblDocTypesTitle.Text = "Requests by document type";
        this.lblDocTypesTitle.BackColor = System.Drawing.Color.Transparent;

        //
        // flowDocTypes
        //
        this.flowDocTypes.Name = "flowDocTypes";
        this.flowDocTypes.Dock = System.Windows.Forms.DockStyle.Fill;
        this.flowDocTypes.AutoScroll = true;
        this.flowDocTypes.WrapContents = false;
        this.flowDocTypes.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this.flowDocTypes.BackColor = System.Drawing.Color.Transparent;

        //
        // cardDocTypes
        //
        this.cardDocTypes.Name = "cardDocTypes";
        this.cardDocTypes.Dock = System.Windows.Forms.DockStyle.Fill;
        this.cardDocTypes.Margin = new System.Windows.Forms.Padding(9, 0, 0, 0);
        this.cardDocTypes.Controls.Add(this.flowDocTypes);
        this.cardDocTypes.Controls.Add(this.lblDocTypesTitle);

        //
        // tblBreakdown - two equal columns that resize with the window
        //
        this.tblBreakdown.Name = "tblBreakdown";
        this.tblBreakdown.Dock = System.Windows.Forms.DockStyle.Fill;
        this.tblBreakdown.ColumnCount = 2;
        this.tblBreakdown.RowCount = 1;
        this.tblBreakdown.BackColor = System.Drawing.Color.Transparent;
        this.tblBreakdown.Padding = new System.Windows.Forms.Padding(0, 18, 0, 0);
        this.tblBreakdown.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
            System.Windows.Forms.SizeType.Percent, 50F));
        this.tblBreakdown.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
            System.Windows.Forms.SizeType.Percent, 50F));
        this.tblBreakdown.Controls.Add(this.cardPurok, 0, 0);
        this.tblBreakdown.Controls.Add(this.cardDocTypes, 1, 0);

        //
        // pnlDashboard
        //
        this.pnlDashboard.Name = "pnlDashboard";
        this.pnlDashboard.Dock = System.Windows.Forms.DockStyle.Fill;
        this.pnlDashboard.AutoScroll = true;
        this.pnlDashboard.BackColor = System.Drawing.Color.Transparent;
        this.pnlDashboard.Padding = new System.Windows.Forms.Padding(28, 0, 28, 20);
        this.pnlDashboard.Controls.Add(this.tblBreakdown);
        this.pnlDashboard.Controls.Add(this.flowStats);
        this.pnlDashboard.Controls.Add(this.heroBanner);

        //
        // pnlContent
        //
        this.pnlContent.Name = "pnlContent";
        this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
        this.pnlContent.BackColor = System.Drawing.Color.Transparent;
        this.pnlContent.Controls.Add(this.pnlDashboard);

        //
        // lblStatus
        //
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
        this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.lblStatus.Padding = new System.Windows.Forms.Padding(28, 0, 0, 0);
        this.lblStatus.Text = "Ready";
        this.lblStatus.BackColor = System.Drawing.Color.Transparent;

        //
        // pnlStatus
        //
        this.pnlStatus.Name = "pnlStatus";
        this.pnlStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.pnlStatus.Height = 34;
        this.pnlStatus.Controls.Add(this.lblStatus);

        //
        // Form1
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.ClientSize = new System.Drawing.Size(1280, 800);
        this.MinimumSize = new System.Drawing.Size(1020, 660);
        this.Name = "Form1";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Barangay Magugpo Poblacion — Resident and Document Request Management System";
        this.Controls.Add(this.pnlContent);
        this.Controls.Add(this.pnlHeader);
        this.Controls.Add(this.pnlStatus);
        this.Controls.Add(this.pnlSidebar);

        ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
        this.pnlSidebar.ResumeLayout(false);
        this.pnlHeader.ResumeLayout(false);
        this.pnlContent.ResumeLayout(false);
        this.pnlDashboard.ResumeLayout(false);
        this.pnlDashboard.PerformLayout();
        this.tblBreakdown.ResumeLayout(false);
        this.cardPurok.ResumeLayout(false);
        this.cardDocTypes.ResumeLayout(false);
        this.pnlStatus.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
