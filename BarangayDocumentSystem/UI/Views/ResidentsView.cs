// ---------------------------------------------------------------------------
//  ResidentsView.cs - the registry screen.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.UI.Controls;
using BarangayDocumentSystem.UI.Dialogs;
using BarangayDocumentSystem.UI.Forms;

namespace BarangayDocumentSystem.UI.Views
{
    /// <summary>
    /// The registry: search, register, change, and the active / inactive /
    /// archived switch.
    ///
    /// The thing to notice on this screen is what is NOT here: there is no
    /// Delete button. A resident is deactivated when they move out or pass
    /// away, or archived when the record is finished with, and either way the
    /// row stays in the database - because certificates already issued and
    /// receipts already collected point at it, and a registry with holes in it
    /// cannot answer "was this person ever a resident here?".
    ///
    /// There is also no Address column, on purpose. The review asked for the
    /// address to be removed from the system, so the purok is the location -
    /// and it is the only location the census, the reports and the printed
    /// documents ever used.
    /// </summary>
    public class ResidentsView : ViewBase
    {
        private TextBox _keyword;
        private ComboBox _purok;
        private ComboBox _state;
        private ComboBox _classification;
        private ComboBox _residency;
        private DataGridView _grid;
        private Label _count;
        private Button _edit;
        private Button _archive;

        public ResidentsView(IBarangayRepository repository, SessionManager session, IClock clock)
            : base(repository, session, clock)
        {
        }

        public override void Build()
        {
            Panel page = BuildPage("Residents",
                "Search by name, purok or contact number. Nobody is deleted: a record is deactivated or "
                + "archived, and the history of issued documents stays whole.");

            _keyword = UiFactory.TextBox("Type part of a name, a purok or a contact number...", 100);
            _keyword.Width = 300;
            _keyword.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadResidents(); }
            };

            _purok = UiFactory.DropDown(null, true);
            FillPuroks(_purok, "All puroks");
            _purok.Width = 190;

            _state = UiFactory.DropDown(new string[] { "Active residents", "Inactive", "Archived", "Everything" }, true);
            _state.Width = 170;
            _state.SelectedIndexChanged += delegate (object sender, EventArgs e) { LoadResidents(); };

            _residency = UiFactory.DropDown(new string[] { "Any residency status", "Newcomer", "Temporary", "Permanent" }, true);
            _residency.Width = 190;
            _residency.SelectedIndexChanged += delegate (object sender, EventArgs e) { LoadResidents(); };

            _classification = UiFactory.DropDown(new string[]
            {
                "Any classification", "Senior citizen", "Person with disability", "Indigent",
                "Solo parent", "4Ps beneficiary", "Student (fee category)"
            }, true);
            _classification.Width = 200;
            _classification.SelectedIndexChanged += delegate (object sender, EventArgs e) { LoadResidents(); };

            Button search = UiFactory.PrimaryButton("Search");
            search.Click += delegate (object sender, EventArgs e) { LoadResidents(); };

            Button clear = UiFactory.SecondaryButton("Clear");
            clear.Click += delegate (object sender, EventArgs e)
            {
                _keyword.Text = string.Empty;
                _purok.SelectedIndex = 0;
                _state.SelectedIndex = 0;
                _residency.SelectedIndex = 0;
                _classification.SelectedIndex = 0;
                LoadResidents();
            };

            Button register = UiFactory.PrimaryButton("Register a resident");
            register.Click += delegate (object sender, EventArgs e) { Open(null, true); };

            _edit = UiFactory.SecondaryButton("Open the record");
            _edit.Click += delegate (object sender, EventArgs e) { Open(SelectedResident(), false); };

            _archive = UiFactory.SecondaryButton("Deactivate or archive");
            _archive.Click += delegate (object sender, EventArgs e) { ChangeState(); };

            Button request = UiFactory.SecondaryButton("File a request for this person");
            request.Click += delegate (object sender, EventArgs e) { FileRequest(); };

            Button masterList = UiFactory.GhostButton("Master list (report)");
            masterList.Click += delegate (object sender, EventArgs e) { GoTo("reports"); };

            FlowLayoutPanel filters = UiFactory.Row(_keyword, _purok, _state, _residency, _classification);
            filters.Location = new Point(AppTheme.PageMargin - 4, AppTheme.PageMargin + 66);
            filters.WrapContents = true;
            filters.Width = 1160;
            filters.Height = 44;
            filters.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            filters.Controls.Add(search);
            filters.Controls.Add(clear);

            FlowLayoutPanel actions = UiFactory.Row(register, _edit, _archive, request, masterList);
            actions.Location = new Point(AppTheme.PageMargin - 4, AppTheme.PageMargin + 118);
            actions.WrapContents = true;
            actions.Width = 1160;
            actions.Height = 44;
            actions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _count = UiFactory.Body(string.Empty);
            _count.Font = AppTheme.SmallBold;
            _count.ForeColor = AppTheme.Primary;
            _count.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 170);
            _count.AutoSize = true;

            _grid = UiFactory.Grid();
            _grid.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 200);
            _grid.Size = new Size(1160, 420);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _grid.CellDoubleClick += delegate (object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0) Open(SelectedResident(), false);
            };

            page.Controls.Add(filters);
            page.Controls.Add(actions);
            page.Controls.Add(_count);
            page.Controls.Add(_grid);
        }

        public override void RefreshData()
        {
            try
            {
                LoadResidents();
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not load the residents.", error);
            }
        }

        // ==================================================================
        //  The list
        // ==================================================================

        private void LoadResidents()
        {
            ResidentQuery query = new ResidentQuery();
            query.Keyword = _keyword.Text;
            query.Purok = PurokFrom(_purok);

            switch (_state.SelectedIndex)
            {
                case 0: query.IncludeInactive = false; break;
                case 1: query.IncludeInactive = true; query.RecordState = RecordState.Inactive; break;
                case 2: query.IncludeInactive = true; query.RecordState = RecordState.Archived; break;
                default: query.IncludeInactive = true; break;
            }

            if (_residency.SelectedIndex > 0)
                query.ResidencyStatus = (ResidencyStatus)(_residency.SelectedIndex - 1);

            switch (_classification.SelectedIndex)
            {
                case 1: query.Classification = ResidentClassification.SeniorCitizen; break;
                case 2: query.Classification = ResidentClassification.PWD; break;
                case 3: query.Classification = ResidentClassification.Indigent; break;
                case 4: query.Classification = ResidentClassification.SoloParent; break;
                case 5: query.Classification = ResidentClassification.FourPsBeneficiary; break;
                default: break;
            }

            IList<Resident> residents = Residents.Search(query);

            DataTable table = new DataTable("residents");
            table.Columns.Add("resident_id", typeof(int));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("age", typeof(int));
            table.Columns.Add("purok", typeof(string));
            table.Columns.Add("residency", typeof(string));
            table.Columns.Add("classification", typeof(string));
            table.Columns.Add("fee_category", typeof(string));
            table.Columns.Add("household", typeof(int));
            table.Columns.Add("status", typeof(string));

            int students = 0;

            foreach (Resident resident in residents)
            {
                if (resident.IsStudentFeeCategory) students++;

                DataRow row = table.NewRow();
                row["resident_id"] = resident.ResidentId;
                row["name"] = resident.GetSortableName();
                row["age"] = resident.GetAge();
                row["purok"] = resident.Purok;
                row["residency"] = EnumText.Of(resident.ResidencyStatus);
                row["classification"] = resident.GetClassificationText();
                row["fee_category"] = resident.GetFeeCategoryText();
                row["household"] = resident.IsHeadOfFamily ? resident.GetHouseholdSize() : 0;
                row["status"] = EnumText.Of(resident.RecordState);
                table.Rows.Add(row);
            }

            _grid.DataSource = table;
            _grid.AutoGenerateColumns = true;

            if (_grid.Columns.Contains("resident_id")) _grid.Columns["resident_id"].Visible = false;
            if (_grid.Columns.Contains("name")) _grid.Columns["name"].FillWeight = 150;
            if (_grid.Columns.Contains("age")) _grid.Columns["age"].FillWeight = 40;
            if (_grid.Columns.Contains("classification")) _grid.Columns["classification"].FillWeight = 110;
            if (_grid.Columns.Contains("fee_category")) _grid.Columns["fee_category"].FillWeight = 120;
            if (_grid.Columns.Contains("household")) _grid.Columns["household"].FillWeight = 55;

            UiFactory.ColourStatusColumn(_grid, "status");
            UiFactory.ColourStatusColumn(_grid, "residency");

            _count.Text = residents.Count + " record(s) shown"
                        + (_state.SelectedIndex == 0 ? " - active residents only" : string.Empty)
                        + (students > 0 ? "   |   " + students + " in the student fee category" : string.Empty)
                        + ".  Double-click a row to open the record.";

            Say(residents.Count + " resident record(s) listed.");
        }

        private Resident SelectedResident()
        {
            if (_grid.CurrentRow == null) return null;

            object id = _grid.CurrentRow.Cells["resident_id"].Value;
            if (id == null || id == DBNull.Value) return null;

            return Residents.Get(Convert.ToInt32(id));
        }

        // ==================================================================
        //  Actions
        // ==================================================================

        private void Open(Resident resident, bool isNew)
        {
            // The two buttons share this method: "Register" means no record,
            // "Open" means the selected one. Either way the window is the same.
            if (!isNew && resident == null)
            {
                Dialog.Warn(this, "Please choose a resident from the list first.", "Nobody chosen");
                return;
            }

            using (ResidentForm form = new ResidentForm(Residents, Session, resident, isNew, Clock))
            {
                if (form.ShowDialog(this) == DialogResult.OK) Announce(form.Message);
            }
        }

        /// <summary>
        /// Taking somebody off the active list, or putting them back on it.
        ///
        /// The reason is asked for in a window of its own because six months
        /// later that sentence is the answer to "why is this person inactive?" -
        /// and it goes into the activity log with whoever typed it.
        /// </summary>
        private void ChangeState()
        {
            Resident resident = SelectedResident();

            if (resident == null)
            {
                Dialog.Warn(this, "Please choose a resident from the list first.", "Nobody chosen");
                return;
            }

            if (resident.IsActive)
            {
                string reason = TextPromptForm.Ask(this, "Take " + resident.GetFullName() + " off the active list",
                    "Why is this resident no longer active? Moved out, passed away, or a duplicate record - "
                    + "and the sentence stays on the record and in the activity log.",
                    "e.g. The family moved to Davao City in March.", true, true);

                if (reason == null) return;

                // Two questions rather than one with three meanings: the safe
                // answer - changing nothing - is Cancel on both of them.
                RecordState wanted = RecordState.Inactive;

                if (!Dialog.ConfirmChange(this, "Mark " + resident.GetFullName() + " as inactive?"
                    + Environment.NewLine + "Inactive keeps the person on the registry list, marked as no "
                    + "longer living here."))
                {
                    if (!Dialog.ConfirmChange(this, "Archive " + resident.GetFullName() + " instead?"
                        + Environment.NewLine + "Archived puts the record aside for the record only.")) return;

                    wanted = RecordState.Archived;
                }

                OperationResult result = Residents.ChangeState(resident, wanted, reason);

                if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

                Announce(result.Message);
                return;
            }

            if (!Dialog.ConfirmChange(this, "Put " + resident.GetFullName()
                + " back on the active list of the barangay?")) return;

            OperationResult restored = Residents.ChangeState(resident, RecordState.Active,
                "Reactivated from the residents screen.");

            if (!restored.Succeeded) { Dialog.Refused(this, restored.Message); return; }

            Announce(restored.Message);
        }

        private void FileRequest()
        {
            Resident resident = SelectedResident();

            if (resident == null)
            {
                Dialog.Warn(this, "Please choose the resident who is asking, then file the request.",
                    "Nobody chosen");
                return;
            }

            if (!resident.IsActive)
            {
                Dialog.Refused(this, resident.GetFullName() + " is not on the active list, so a new request "
                    + "cannot be filed for them. Put the record back on the active list first.");
                return;
            }

            GoTo("requests");
        }

        private void GoTo(string key)
        {
            ShellForm shell = FindForm() as ShellForm;
            if (shell != null) shell.GoTo(key);
        }
    }
}
