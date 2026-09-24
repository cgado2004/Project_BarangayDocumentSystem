using System;
using System.Windows.Forms;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Helpers;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Controls
{
    public partial class RequestsControl : UserControl
    {
        private readonly ResidentService residents;
        private readonly RequestService requests;
        private readonly DocumentRenderer renderer;
        public event EventHandler DataChanged;

        public RequestsControl()
        {
            InitializeComponent();
            cmbStatus.SelectedIndex = 0;
            UpdateSelection();
        }

        public RequestsControl(ResidentService residents, RequestService requests, DocumentRenderer renderer) : this()
        {
            this.residents = residents;
            this.requests = requests;
            this.renderer = renderer;
            RefreshData();
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            UiFeedback.Run(this, RefreshData);
        }

        private void RequestSelectionChanged(object sender, EventArgs e) { UpdateSelection(); }

        private void ProcessRequest(object sender, EventArgs e) { ChangeStatus(requests.StartProcessing); }

        private void ReadyRequest(object sender, EventArgs e) { ChangeStatus(requests.MarkReady); }

        public void RefreshData()
        {
            if (requests == null) return;
            int selectedId = SelectedRequest() == null ? 0 : SelectedRequest().RequestId;
            RequestStatus? status = cmbStatus.SelectedIndex <= 0 ? (RequestStatus?)null :
                (RequestStatus)(cmbStatus.SelectedIndex - 1);
            gridRequests.DataSource = requests.Search(txtSearch.Text, status);
            foreach (DataGridViewRow row in gridRequests.Rows)
            {
                if (((DocumentRequest)row.DataBoundItem).RequestId == selectedId)
                    gridRequests.CurrentCell = row.Cells[0];
            }
            UpdateSelection();
        }

        private DocumentRequest SelectedRequest()
        {
            return gridRequests.CurrentRow == null ? null : gridRequests.CurrentRow.DataBoundItem as DocumentRequest;
        }

        private DocumentRequest RequireSelection()
        {
            var request = SelectedRequest();
            if (request == null) throw new InvalidOperationException("Select a document request first.");
            return requests.Get(request.RequestId);
        }

        private void UpdateSelection()
        {
            var request = SelectedRequest();
            bool active = request != null && request.Status != RequestStatus.Rejected && request.Status != RequestStatus.Released;
            btnProcess.Enabled = request != null && request.Status == RequestStatus.Pending;
            btnReady.Enabled = request != null && request.Status == RequestStatus.Processing;
            btnRelease.Enabled = request != null && request.Status == RequestStatus.ReadyForRelease &&
                (request.Fee == 0m || request.IsPaid);
            btnPay.Enabled = active && request.Fee > 0m && !request.IsPaid;
            btnReject.Enabled = active;
            btnPreview.Enabled = request != null;
            if (request == null)
            {
                txtDetails.Text = "No request selected. Use New request to get started.";
                return;
            }
            txtDetails.Text = request.ReferenceNumber + " | " + request.ResidentName +
                " | Requested: " + request.DateRequested.ToString("yyyy-MM-dd HH:mm") +
                Environment.NewLine + "Purpose: " + request.Purpose +
                Environment.NewLine + "Fee: PHP " + request.Fee.ToString("N2") + " | " + request.FeeBasis +
                Environment.NewLine + "Payment: " + request.PaymentText +
                (request.IsPaid ? " | Receipt: " + request.OfficialReceiptNumber + " | Paid: " + request.DatePaid.Value.ToString("yyyy-MM-dd HH:mm") : "") +
                (request.Status == RequestStatus.Rejected ? Environment.NewLine + "Rejection reason: " + request.RejectionReason : "");
        }

        private void NewRequest(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                if (residents.Search().Count == 0) throw new InvalidOperationException("Register a resident before filing a request.");
                using (var form = new RequestForm(residents, requests, renderer))
                    if (form.ShowDialog(this) == DialogResult.OK) Changed();
            });
        }

        private void ChangeStatus(Action<int> action)
        {
            UiFeedback.Run(this, () => { action(RequireSelection().RequestId); Changed(); });
        }

        private void ReleaseRequest(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                var request = RequireSelection();
                if (!UiFeedback.Confirm(this, "Release " + request.ReferenceNumber +
                    "? The document text will be finalized and the request cannot be rejected afterward.")) return;
                requests.Release(request.RequestId);
                Changed();
            });
        }

        private void RecordPayment(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                using (var form = new PaymentForm(requests, RequireSelection()))
                    if (form.ShowDialog(this) == DialogResult.OK) Changed();
            });
        }

        private void RejectRequest(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                using (var form = new RejectionForm(requests, RequireSelection()))
                    if (form.ShowDialog(this) == DialogResult.OK) Changed();
            });
        }

        private void PreviewDocument(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                var request = RequireSelection();
                using (var form = new DocumentPreviewForm(request, requests.Preview(request.RequestId)))
                    form.ShowDialog(this);
            });
        }

        private void Changed()
        {
            RefreshData();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
