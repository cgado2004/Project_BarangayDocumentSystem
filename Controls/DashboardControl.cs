using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Controls
{
    public partial class DashboardControl : UserControl
    {
        private readonly ResidentService residents;
        private readonly RequestService requests;

        public DashboardControl() { InitializeComponent(); }

        public DashboardControl(ResidentService residents, RequestService requests) : this()
        {
            this.residents = residents;
            this.requests = requests;
            RefreshData();
        }

        public void RefreshData()
        {
            if (residents == null || requests == null) return;
            var residentRecords = residents.Search();
            var requestRecords = requests.Search();
            lblResidents.Text = residentRecords.Count.ToString();
            lblRequests.Text = requestRecords.Count.ToString();
            lblPending.Text = requestRecords.Count(request => request.Status == RequestStatus.Pending).ToString();
            lblReady.Text = requestRecords.Count(request => request.Status == RequestStatus.ReadyForRelease).ToString();
            lblFree.Text = requestRecords.Count(request => request.Status == RequestStatus.Released && request.Fee == 0m).ToString();
            lblRevenue.Text = "PHP " + requestRecords.Where(request => request.IsPaid).Sum(request => request.Fee).ToString("N2");
            gridStatuses.DataSource = Enum.GetValues(typeof(RequestStatus)).Cast<RequestStatus>()
                .Select(status => new KeyValuePair<string, int>(
                    status == RequestStatus.ReadyForRelease ? "Ready for Release" : status.ToString(),
                    requestRecords.Count(request => request.Status == status))).ToList();
            gridTypes.DataSource = requestRecords.GroupBy(request => request.DocumentName)
                .Select(group => new KeyValuePair<string, int>(group.Key, group.Count())).ToList();
            gridPuroks.DataSource = residentRecords.GroupBy(resident => resident.Purok)
                .OrderBy(group => group.Key)
                .Select(group => new KeyValuePair<string, int>(group.Key, group.Count())).ToList();
        }
    }
}
