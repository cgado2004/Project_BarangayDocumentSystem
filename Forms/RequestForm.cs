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

        private void CloseDialog(object sender, EventArgs e) { Close(); }

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
                    Scope = cmbScope.SelectedIndex == 1 ? ClearanceScope.Abroad : ClearanceScope.Local,
                    Purpose = txtPurpose.Text, BusinessName = txtBusinessName.Text,
                    BusinessAddress = txtBusinessAddress.Text, BusinessNature = txtBusinessNature.Text
                });
                DialogResult = DialogResult.OK;
                Close();
            });
        }
    
        /// <summary>
        /// The clearance scope picker: one extra row in the fields table,
        /// built here at run time so the designer file stays exactly as
        /// Jonathan committed it. Enabled only for the Barangay Clearance —
        /// the one document the Citizen's Charter prices two ways
        /// (PHP 100 local employment, PHP 200 work abroad).
        /// </summary>
        private ComboBox cmbScope;

        private void BuildScopeRow()
        {
            var caption = new Label
            {
                Text = "Clearance scope",
                AutoSize = true,
                Margin = new Padding(0, 7, 10, 8)
            };
            cmbScope = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 4, 0, 5),
                Enabled = false
            };
            cmbScope.Items.AddRange(new object[] { "Local employment (PHP 100)", "Work abroad (PHP 200)" });
            cmbScope.SelectedIndex = 0;

            // shift the existing rows down one and take row 2 for the scope
            // (rows before this: 0 = resident, 1 = document, 2 = purpose,
            //  3-5 = business fields; the scope slots in under the document)
            tblFields.RowCount += 1;
            tblFields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var shifted = new[]
            {
                new[] { lblPurposeCaption, (Control)txtPurpose },
                new[] { lblBusinessNameCaption, (Control)txtBusinessName },
                new[] { lblBusinessAddressCaption, (Control)txtBusinessAddress },
                new[] { lblBusinessNatureCaption, (Control)txtBusinessNature }
            };
            int row = 3;
            foreach (var pair in shifted)
            {
                tblFields.SetCellPosition(pair[0], new TableLayoutPanelCellPosition(0, row));
                tblFields.SetCellPosition(pair[1], new TableLayoutPanelCellPosition(1, row));
                row++;
            }
            tblFields.Controls.Add(caption, 0, 2);
            tblFields.Controls.Add(cmbScope, 1, 2);

            cmbDocument.SelectedValueChanged += (sender, args) =>
            {
                var template = cmbDocument.SelectedItem as IDocumentTemplate;
                cmbScope.Enabled = template != null &&
                    template.DocumentType == DocumentType.BarangayClearance;
            };
        }
    }
}
