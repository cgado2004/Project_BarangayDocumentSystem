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
        private readonly ResidentService residents;
        private DashboardControl dashboardPage;
        private ResidentsControl residentsPage;
        private RequestsControl requestsPage;

        public MainForm() { InitializeComponent(); }

        public MainForm(ResidentService residents, RequestService requests, DocumentRenderer renderer, AppSettings settings)
            : this()
        {
            this.residents = residents;
            lblBarangay.Text = settings.Profile.BarangayName + Environment.NewLine + settings.Profile.CityName;
            lblSession.Text = "Session-only records: data resets when the app closes. Classroom fee schedule." +
                (settings.LoadSampleData ? " Sample records loaded." : "");
            dashboardPage = new DashboardControl(residents, requests);
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
                    button.BackColor = button == selectedButton ? Color.FromArgb(57, 120, 100) : Color.FromArgb(31, 74, 64);
                if (page == dashboardPage) dashboardPage.RefreshData();
                if (page == residentsPage) residentsPage.RefreshData();
                if (page == requestsPage) requestsPage.RefreshData();
            });
        }

        private void MainFormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && residents != null && residents.Search().Count > 0)
                e.Cancel = !UiFeedback.Confirm(this, "Close this session? Resident records, requests, and payment records are stored only in memory and will be lost.");
        }
    }
}
