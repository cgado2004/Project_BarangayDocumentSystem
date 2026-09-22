using System;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Forms
{
    public partial class RejectionForm : Form
    {
        private readonly RequestService service;
        private readonly int requestId;

        public RejectionForm() { InitializeComponent(); }

        public RejectionForm(RequestService service, DocumentRequest request) : this()
        {
            this.service = service;
            requestId = request.RequestId;
            lblSummary.Text = "Reject " + request.ReferenceNumber + " for " + request.ResidentName + "?" +
                (request.IsPaid ? Environment.NewLine + "The payment will remain in the collection history. This app does not issue refunds." : "");
        }

        private void RejectRequest(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                service.Reject(requestId, txtReason.Text);
                DialogResult = DialogResult.OK;
                Close();
            });
        }
    }
}
