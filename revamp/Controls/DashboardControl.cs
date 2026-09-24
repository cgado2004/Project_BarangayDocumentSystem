using System.Windows.Forms;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Controls
{
    public partial class DashboardControl : UserControl
    {
        private readonly ReportingService reporting;

        public DashboardControl() { InitializeComponent(); }

        public DashboardControl(ReportingService reporting) : this()
        {
            this.reporting = reporting;
            RefreshData();
        }

        public void RefreshData()
        {
            if (reporting == null) return;
            var statistics = reporting.GetStatistics();
            lblResidents.Text = statistics.TotalResidents.ToString();
            lblRequests.Text = statistics.TotalRequests.ToString();
            lblPending.Text = statistics.PendingRequests.ToString();
            lblReady.Text = statistics.ReadyRequests.ToString();
            lblFree.Text = statistics.FreeDocumentsReleased.ToString();
            lblRevenue.Text = "PHP " + statistics.TotalCollected.ToString("N2");
            gridStatuses.DataSource = statistics.RequestsByStatus;
            gridTypes.DataSource = statistics.RequestsByDocument;
            gridPuroks.DataSource = statistics.ResidentsByPurok;
            if (StatisticsUpdated != null) StatisticsUpdated(statistics);
        }

        /// <summary>The theme's live overlays (peso card, RELEASED card,
        /// chips and bars) listen here instead of re-computing anything.</summary>
        internal event Action<Models.BarangayStatistics> StatisticsUpdated;
    }
}
