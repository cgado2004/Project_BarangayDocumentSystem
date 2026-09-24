using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Configuration;
using BarangayDocumentSystem.Controls;
using BarangayDocumentSystem.Helpers;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Forms
{
    public partial class MainForm : Form
    {
        private DashboardControl dashboardPage;
        private readonly Services.ReportingService reporting;
        private readonly AppSettings settings;
        private ResidentsControl residentsPage;
        private RequestsControl requestsPage;

        public MainForm() { InitializeComponent(); }

        public MainForm(ResidentService residents, RequestService requests, DocumentRenderer renderer, AppSettings settings, ReportingService reporting)
            : this()
        {
            this.reporting = reporting;
            this.settings = settings;
            lblBarangay.Text = settings.Profile.BarangayName + Environment.NewLine + settings.Profile.CityName;
            lblSession.Text = "Records are saved in MySQL (barangay_db). Citizen's Charter fee schedule.";
            dashboardPage = new DashboardControl(reporting);
            dashboardPage.SetProfile(settings.Profile);
            residentsPage = new ResidentsControl(residents, requests, renderer);
            requestsPage = new RequestsControl(residents, requests, renderer);
            pnlContent.Controls.AddRange(new Control[] { dashboardPage, residentsPage, requestsPage });
            residentsPage.DataChanged += RecordsChanged;
            requestsPage.DataChanged += RecordsChanged;
            ShowPage(dashboardPage, btnDashboard, "Dashboard");
        }

        private void RecordsChanged(object sender, EventArgs e)
        {
            dashboardPage.RefreshData();
            residentsPage.RefreshData();
            requestsPage.RefreshData();
        }

        private void NavigateDashboard(object sender, EventArgs e)
        {
            ShowPage(dashboardPage, btnDashboard, "Dashboard");
        }

        private void NavigateResidents(object sender, EventArgs e)
        {
            ShowPage(residentsPage, btnResidents, "Residents");
        }

        private void NavigateRequests(object sender, EventArgs e)
        {
            ShowPage(requestsPage, btnRequests, "Document Requests");
        }

        private void ShowPage(Control page, Button selectedButton, string title)
        {
            if (page == null) return;
            UiFeedback.Run(this, () =>
            {
                foreach (Control control in pnlContent.Controls) control.Visible = control == page;
                page.BringToFront();
                lblPageTitle.Text = title;
                foreach (var button in new[] { btnDashboard, btnResidents, btnRequests })
                {
                    bool active = button == selectedButton;
                    button.BackColor = active ? Helpers.ModernTheme.PrimaryNavy : Color.White;
                    button.ForeColor = active ? Color.White : Helpers.ModernTheme.Muted;
                    button.Invalidate();
                }
                RefreshFooter();
                if (page == dashboardPage) dashboardPage.RefreshData();
                if (page == residentsPage) residentsPage.RefreshData();
                if (page == requestsPage) requestsPage.RefreshData();
            });
        }

    }
}
