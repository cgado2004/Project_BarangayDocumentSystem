using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Views;
using BarangayDocumentSystem.CustomControls;
namespace BarangayDocumentSystem;

/// <summary>
/// The designer half of my main window.
///
/// I keep every control declaration and every layout instruction in this
/// file, exactly the way Visual Studio expects. That matters for two
/// reasons: the form opens properly in the WinForms designer so my
/// group-mates can see and drag the controls (v3.1 core fix 1), and
/// MainShell.cs is left holding only the behaviour.
///
/// The navigation rail is the NavigationSidebar control, which has a
/// parameterless constructor precisely so it can sit in a designer file.
/// </summary>
partial class MainShell
{
    private System.ComponentModel.IContainer components = null;

    private BarangayDocumentSystem.CustomControls.NavigationSidebar sidebar;
    private System.Windows.Forms.Panel pnlHeader;
    private System.Windows.Forms.Label lblPageTitle;
    private System.Windows.Forms.Label lblPageSubtitle;
    private System.Windows.Forms.Panel pnlContent;
    private System.Windows.Forms.Panel pnlStatus;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.Label lblStatusRight;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
            _dashboardView?.Dispose();
            _residentsView?.Dispose();
            _requestsView?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.sidebar = new BarangayDocumentSystem.CustomControls.NavigationSidebar();
        this.pnlHeader = new System.Windows.Forms.Panel();
        this.lblPageTitle = new System.Windows.Forms.Label();
        this.lblPageSubtitle = new System.Windows.Forms.Label();
        this.pnlContent = new System.Windows.Forms.Panel();
        this.pnlStatus = new System.Windows.Forms.Panel();
        this.lblStatus = new System.Windows.Forms.Label();
        this.lblStatusRight = new System.Windows.Forms.Label();

        this.pnlHeader.SuspendLayout();
        this.pnlContent.SuspendLayout();
        this.pnlStatus.SuspendLayout();
        this.SuspendLayout();

        //
        // sidebar - the navigation rail
        //
        this.sidebar.Name = "sidebar";
        this.sidebar.Dock = System.Windows.Forms.DockStyle.Left;
        this.sidebar.Width = 248;
        this.sidebar.Navigate += new System.EventHandler<string>(this.OnSidebarNavigate);

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
        // pnlContent - the host of whichever view is open
        //
        this.pnlContent.Name = "pnlContent";
        this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
        this.pnlContent.BackColor = System.Drawing.Color.Transparent;

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
        // lblStatusRight - storage and DPI readout on the other end of the bar
        //
        this.lblStatusRight.Name = "lblStatusRight";
        this.lblStatusRight.Dock = System.Windows.Forms.DockStyle.Right;
        this.lblStatusRight.Width = 340;
        this.lblStatusRight.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        this.lblStatusRight.Padding = new System.Windows.Forms.Padding(0, 0, 28, 0);
        this.lblStatusRight.Text = "";
        this.lblStatusRight.BackColor = System.Drawing.Color.Transparent;

        //
        // pnlStatus
        //
        this.pnlStatus.Name = "pnlStatus";
        this.pnlStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.pnlStatus.Height = 34;
        this.pnlStatus.Controls.Add(this.lblStatusRight);
        this.pnlStatus.Controls.Add(this.lblStatus);

        //
        // MainShell
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.ClientSize = new System.Drawing.Size(1280, 800);
        this.MinimumSize = new System.Drawing.Size(1020, 660);
        this.Name = "MainShell";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Barangay Magugpo Poblacion — Resident and Document Request Management System";
        this.Controls.Add(this.pnlContent);
        this.Controls.Add(this.pnlHeader);
        this.Controls.Add(this.pnlStatus);
        this.Controls.Add(this.sidebar);

        this.pnlHeader.ResumeLayout(false);
        this.pnlContent.ResumeLayout(false);
        this.pnlStatus.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
