using System;
using System.Linq;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Forms
{
    public partial class RequestForm : Form
    {
        private readonly RequestService service;

        public RequestForm() { InitializeComponent(); }

        public RequestForm(ResidentService residents, RequestService requests, DocumentRenderer renderer, int? selectedResidentId = null)
            : this()
        {
            service = requests;
            cmbResident.DisplayMember = "FullName";
            cmbResident.ValueMember = "ResidentId";
            cmbResident.DataSource = residents.Search();
            cmbResident.FormattingEnabled = true;
            cmbResident.Format += FormatResident;
            if (selectedResidentId.HasValue) cmbResident.SelectedValue = selectedResidentId.Value;
            cmbDocument.DisplayMember = "Title";
            cmbDocument.DataSource = renderer.Templates.ToList();
            cmbResident.SelectedIndexChanged += SelectionChanged;
            cmbDocument.SelectedIndexChanged += SelectionChanged;
            UpdateFee();
        }

        private void SelectionChanged(object sender, EventArgs e) { UpdateFee(); }

        private void FormatResident(object sender, ListControlConvertEventArgs e)
        {
            var resident = e.ListItem as Resident;
            if (resident != null)
                e.Value = "#" + resident.ResidentId + " - " + resident.FullName + " (" + resident.Purok + ")";
        }

        private void UpdateFee()
        {
            var resident = cmbResident.SelectedItem as Resident;
            var template = cmbDocument.SelectedItem as IDocumentTemplate;
            bool business = template != null && template.DocumentType == DocumentType.BarangayBusinessClearance;
            businessFields.Visible = business;
            btnSubmit.Enabled = false;
            if (resident == null || template == null || service == null)
            {
                lblFee.Text = "Register a resident first, then select a document.";
                return;
            }
            try
            {
                var assessment = service.Assess(resident.ResidentId, template.DocumentType);
                lblFee.Text = "Fee: PHP " + assessment.Amount.ToString("N2") + Environment.NewLine + assessment.Basis;
                btnSubmit.Enabled = true;
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            {
                lblFee.Text = error.Message;
            }
        }

        private void SubmitRequest(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                var resident = cmbResident.SelectedItem as Resident;
                var template = cmbDocument.SelectedItem as IDocumentTemplate;
                if (resident == null || template == null) throw new ArgumentException("Select a resident and document type.");
                service.Create(new RequestDetails
                {
                    ResidentId = resident.ResidentId, DocumentType = template.DocumentType,
                    Purpose = txtPurpose.Text, BusinessName = txtBusinessName.Text,
                    BusinessAddress = txtBusinessAddress.Text, BusinessNature = txtBusinessNature.Text
                });
                DialogResult = DialogResult.OK;
                Close();
            });
        }
    }
}
