using System;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Forms
{
    public partial class PaymentForm : Form
    {
        private readonly RequestService service;
        private readonly int requestId;

        public PaymentForm() { InitializeComponent(); }

        public PaymentForm(RequestService service, DocumentRequest request) : this()
        {
            this.service = service;
            requestId = request.RequestId;
            lblSummary.Text = request.ReferenceNumber + " - " + request.ResidentName +
                Environment.NewLine + request.DocumentName + Environment.NewLine +
                "Amount to record: PHP " + request.Fee.ToString("N2");
        }

        private void SavePayment(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                service.RecordPayment(requestId, txtReceipt.Text);
                DialogResult = DialogResult.OK;
                Close();
            });
        }
    }
}
