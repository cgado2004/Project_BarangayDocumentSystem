// ---------------------------------------------------------------------------
//  ResidentPickerForm.cs - choosing the resident a transaction belongs to.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.UI.Controls;

namespace BarangayDocumentSystem.UI.Forms
{
    /// <summary>
    /// The small window that finds a resident.
    ///
    /// I wrote one picker and use it from the request form and from anywhere
    /// else a resident has to be chosen, so nobody ever types a name into a
    /// request by hand. Typing a name by hand is how the same person ends up in
    /// the system three times, once as "Juan Dela Cruz", once as "Juan dela
    /// cruz" - and then the certificates have three different spellings.
    ///
    /// Inactive and archived people are deliberately not listed: a request is
    /// filed for somebody who lives in the barangay. If a record was put
    /// inactive by mistake, it is reactivated on the Residents screen first.
    /// </summary>
    public class ResidentPickerForm : Form
    {
        private readonly ResidentService _residents;

        private TextBox _keyword;
        private DataGridView _grid;
        private Label _count;
        private Button _choose;

        public Resident Selected { get; private set; }

        public ResidentPickerForm(ResidentService residents)
        {
            _residents = residents;

            BuildWindow();
            Search();
        }

        private void BuildWindow()
        {
            Text = "Choose the resident";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(760, 520);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Label heading = UiFactory.PageHeading("Who is this for?");
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4);

            Label caption = UiFactory.Caption("Search by name, purok or contact number, then choose the person.");
            caption.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 30);
            caption.Width = 700;

            _keyword = UiFactory.TextBox("Type part of the name...", 100);
            _keyword.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 58);
            _keyword.Width = 420;
            _keyword.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Search(); }
            };

            Button search = UiFactory.PrimaryButton("Search");
            search.Location = new Point(AppTheme.PageMargin + 428, AppTheme.Gap4 + 58);
            search.Width = 110;
            search.Click += delegate (object sender, EventArgs e) { Search(); };

            _count = new Label();
            _count.Font = AppTheme.Small;
            _count.ForeColor = AppTheme.Muted;
            _count.AutoSize = true;
            _count.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 100);

            _grid = UiFactory.Grid();
            _grid.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 126);
            _grid.Size = new Size(700, 300);
            _grid.CellDoubleClick += delegate (object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0) Choose();
            };

            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 64;
            footer.BackColor = AppTheme.Surface;

            _choose = UiFactory.PrimaryButton("Choose this resident");
            _choose.Width = 190;
            _choose.Location = new Point(ClientSize.Width - 190 - AppTheme.PageMargin, 15);
            _choose.Click += delegate (object sender, EventArgs e) { Choose(); };

            Button cancel = UiFactory.SecondaryButton("Cancel");
            cancel.Width = 100;
            cancel.Location = new Point(ClientSize.Width - 190 - 100 - AppTheme.PageMargin - AppTheme.Gap2, 15);
            cancel.Click += delegate (object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.Add(_choose);
            footer.Controls.Add(cancel);

            Controls.Add(heading);
            Controls.Add(caption);
            Controls.Add(_keyword);
            Controls.Add(search);
            Controls.Add(_count);
            Controls.Add(_grid);
            Controls.Add(footer);

            AcceptButton = search;
            CancelButton = cancel;
        }

        private void Search()
        {
            ResidentQuery query = new ResidentQuery();
            query.Keyword = _keyword.Text;
            query.IncludeInactive = false;      // only active residents; see the note above

            IList<Resident> residents = _residents.Search(query);

            DataTable table = new DataTable("residents");
            table.Columns.Add("resident_id", typeof(int));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("age", typeof(int));
            table.Columns.Add("purok", typeof(string));
            table.Columns.Add("classification", typeof(string));
            table.Columns.Add("fee_category", typeof(string));

            foreach (Resident resident in residents)
            {
                DataRow row = table.NewRow();
                row["resident_id"] = resident.ResidentId;
                row["name"] = resident.GetSortableName();
                row["age"] = resident.GetAge();
                row["purok"] = resident.Purok;
                row["classification"] = resident.GetClassificationText();
                row["fee_category"] = resident.GetFeeCategoryText();
                table.Rows.Add(row);
            }

            _grid.DataSource = table;
            _grid.AutoGenerateColumns = true;

            if (_grid.Columns.Contains("resident_id")) _grid.Columns["resident_id"].Visible = false;
            if (_grid.Columns.Contains("name")) _grid.Columns["name"].FillWeight = 150;
            if (_grid.Columns.Contains("age")) _grid.Columns["age"].FillWeight = 40;

            _count.Text = residents.Count + " active resident(s) found.";
        }

        private void Choose()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.Cells["resident_id"].Value == DBNull.Value)
            {
                Dialog.Warn(this, "Please choose a person from the list first.", "Nobody chosen");
                return;
            }

            int id = Convert.ToInt32(_grid.CurrentRow.Cells["resident_id"].Value);
            Selected = _residents.Get(id);

            if (Selected == null)
            {
                Dialog.Warn(this, "That record could not be opened any more. Please search again.",
                    "Record not found");
                Search();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
