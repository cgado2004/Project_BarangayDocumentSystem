using System;
using System.Linq;
using System.Windows.Forms;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Helpers;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Controls
{
    public partial class ResidentsControl : UserControl
    {
        private readonly ResidentService residents;
        private readonly RequestService requests;
        private readonly DocumentRenderer renderer;
        public event EventHandler DataChanged;

        public ResidentsControl() { InitializeComponent(); }

        public ResidentsControl(ResidentService residents, RequestService requests, DocumentRenderer renderer) : this()
        {
            this.residents = residents;
            this.requests = requests;
            this.renderer = renderer;
            RefreshData();
        }

        private string purokFilter;

        /// <summary>Shows only one purok - the dashboard's purok chips land
        /// here. Null or empty clears the filter again.</summary>
        public void ApplyPurokFilter(string purok)
        {
            purokFilter = string.IsNullOrWhiteSpace(purok) ? null : purok.Trim();
            txtSearch.Text = "";
            RefreshData();
        }

        private void SearchChanged(object sender, EventArgs e)
        {
            UiFeedback.Run(this, RefreshData);
        }

        private void ResidentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) EditResident(sender, e);
        }

        public void RefreshData()
        {
            if (residents == null) return;
            int selectedId = SelectedResident() == null ? 0 : SelectedResident().ResidentId;
            var records = residents.Search(txtSearch.Text);
            if (purokFilter != null)
                records = records.Where(resident => resident.Purok == purokFilter).ToList();
            gridResidents.DataSource = records;
            foreach (DataGridViewRow row in gridResidents.Rows)
            {
                if (((Resident)row.DataBoundItem).ResidentId == selectedId)
                    gridResidents.CurrentCell = row.Cells[0];
            }
            lblCount.Text = records.Count + " resident(s). Select a row to edit or file a request.";
        }

        private Resident SelectedResident()
        {
            return gridResidents.CurrentRow == null ? null : gridResidents.CurrentRow.DataBoundItem as Resident;
        }

        private Resident RequireSelection()
        {
            var resident = SelectedResident();
            if (resident == null) throw new InvalidOperationException("Select a resident first.");
            return residents.Get(resident.ResidentId);
        }

        private void RegisterResident(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                using (var form = new ResidentForm(residents))
                    if (form.ShowDialog(this) == DialogResult.OK) Changed();
            });
        }

        private void EditResident(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                using (var form = new ResidentForm(residents, RequireSelection()))
                    if (form.ShowDialog(this) == DialogResult.OK) Changed();
            });
        }

        private void DeleteResident(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                var resident = RequireSelection();
                if (!UiFeedback.Confirm(this, "Delete " + resident.FullName + "? Residents with requests cannot be deleted.")) return;
                residents.Delete(resident.ResidentId);
                Changed();
            });
        }

        private void FileRequest(object sender, EventArgs e)
        {
            UiFeedback.Run(this, () =>
            {
                using (var form = new RequestForm(residents, requests, renderer, RequireSelection().ResidentId))
                    if (form.ShowDialog(this) == DialogResult.OK) Changed();
            });
        }

        private void Changed()
        {
            RefreshData();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
