// ---------------------------------------------------------------------------
//  DependentForm.cs - the household list of a head of the family.
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
    /// The dependents of one household.
    ///
    /// The barangay asked for dependents to be part of registration, and this
    /// is where they live. A dependent is deliberately NOT a resident: a child
    /// or a parent living in the same house is counted in the census and shows
    /// up in the household size, but they do not get their own registry record
    /// and their own reference number, because they do not transact with the
    /// barangay on their own. When they grow up and file their own request,
    /// they are registered as a resident - and the household list is where the
    /// clerk can see who they were living with.
    ///
    /// The name is shown in a group box here too, the same as on the resident
    /// form, so the two screens read the same way.
    /// </summary>
    public class DependentForm : Form
    {
        private readonly ResidentService _residents;
        private readonly Resident _head;

        private DataGridView _grid;
        private NameGroupBox _headBox;
        private Label _summary;

        private TextBox _name;
        private ComboBox _relation;
        private DateTimePicker _birthday;
        private CheckBox _studying;
        private TextBox _remarks;

        private Button _add;
        private Button _update;
        private Button _remove;
        private Button _close;

        public string Message { get; private set; }

        public DependentForm(ResidentService residents, Resident head, IClock clock)
        {
            _residents = residents;
            _head = head;

            BuildWindow();
            LoadDependents();
        }

        private void BuildWindow()
        {
            Text = "Household of " + _head.GetFullName();
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(900, 660);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Panel page = new Panel();
            page.Dock = DockStyle.Fill;
            page.AutoScroll = true;
            page.Padding = new Padding(AppTheme.PageMargin, AppTheme.Gap4, AppTheme.PageMargin, AppTheme.Gap4);
            page.BackColor = AppTheme.Canvas;

            Label heading = UiFactory.PageHeading("Household members");
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4);

            Label caption = UiFactory.Caption(
                "Dependents are counted in the census and in the household size, but they are not "
                + "separate residents - they do not file requests of their own.");
            caption.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 30);
            caption.Width = 830;

            _headBox = new NameGroupBox("Head of the family");
            _headBox.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 60);
            _headBox.Size = new Size(830, 88);
            _headBox.NameText = _head.GetFullName().ToUpperInvariant();

            _summary = new Label();
            _summary.Font = AppTheme.Small;
            _summary.ForeColor = AppTheme.Muted;
            _summary.AutoSize = true;
            _summary.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 162);

            _grid = UiFactory.Grid();
            _grid.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 190);
            _grid.Size = new Size(830, 220);
            _grid.SelectionChanged += delegate (object sender, EventArgs e) { FillFromGrid(); };
            _grid.CellDoubleClick += delegate (object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0) UpdateDependent();
            };

            FlatGroupBox entry = new FlatGroupBox("Add or change a member");
            entry.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 424);
            entry.Size = new Size(830, 158);

            _name = UiFactory.TextBox("Full name", 120);
            _relation = UiFactory.DropDown(DependentRelationTexts(), true);
            _birthday = UiFactory.DatePicker(DateTime.Today);
            _studying = UiFactory.CheckBox("Still studying", false);
            _remarks = UiFactory.TextBox("e.g. lives with the family, work in another city", 200);

            AddField(entry, "Full name", _name, 12, 26, 300);
            AddField(entry, "Relationship", _relation, 324, 26, 170);
            AddField(entry, "Date of birth", _birthday, 506, 26, 170);
            _studying.Location = new Point(12, 78);
            AddField(entry, "Remarks", _remarks, 160, 74, 520);

            entry.Controls.Add(_studying);

            // ---- the buttons ----
            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 64;
            footer.BackColor = AppTheme.Surface;
            footer.Padding = new Padding(AppTheme.PageMargin, 0, AppTheme.PageMargin, 0);

            _add = UiFactory.PrimaryButton("Add this member");
            _update = UiFactory.SecondaryButton("Save the change");
            _remove = UiFactory.DangerButton("Remove from the household");
            _close = UiFactory.GhostButton("Close");

            _add.Click += delegate (object sender, EventArgs e) { AddDependent(); };
            _update.Click += delegate (object sender, EventArgs e) { UpdateDependent(); };
            _remove.Click += delegate (object sender, EventArgs e) { RemoveDependent(); };
            _close.Click += delegate (object sender, EventArgs e) { DialogResult = DialogResult.OK; Close(); };

            UiFactory.AlignButtons(_add, _update, _remove, _close);
            _remove.Width = 220;

            FlowLayoutPanel buttons = UiFactory.ButtonRow(_add, _update, _remove, _close);
            buttons.Location = new Point(AppTheme.PageMargin, 15);
            footer.Controls.Add(buttons);

            page.Controls.Add(heading);
            page.Controls.Add(caption);
            page.Controls.Add(_headBox);
            page.Controls.Add(_summary);
            page.Controls.Add(_grid);
            page.Controls.Add(entry);

            Controls.Add(page);
            Controls.Add(footer);

            CancelButton = _close;
        }

        private static void AddField(Control parent, string caption, Control control, int x, int y, int width)
        {
            Label label = UiFactory.FieldLabel(caption);
            label.Location = new Point(x, y);
            label.Width = width;

            control.Location = new Point(x, y + 20);
            control.Width = width;

            parent.Controls.Add(label);
            parent.Controls.Add(control);
        }

        private static string[] DependentRelationTexts()
        {
            string[] names = Enum.GetNames(typeof(DependentRelation));
            string[] texts = new string[names.Length];

            for (int i = 0; i < names.Length; i++) texts[i] = EnumText.Spaced(names[i]);

            return texts;
        }

        // ==================================================================
        //  Loading
        // ==================================================================

        private void LoadDependents()
        {
            IList<Dependent> dependents = _residents.GetDependents(_head.ResidentId);

            DataTable table = new DataTable("dependents");
            table.Columns.Add("dependent_id", typeof(int));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("relation", typeof(string));
            table.Columns.Add("age", typeof(int));
            table.Columns.Add("birthday", typeof(string));
            table.Columns.Add("studying", typeof(string));
            table.Columns.Add("remarks", typeof(string));

            foreach (Dependent dependent in dependents)
            {
                DataRow row = table.NewRow();
                row["dependent_id"] = dependent.DependentId;
                row["name"] = dependent.FullName;
                row["relation"] = dependent.GetRelationText();
                row["age"] = dependent.GetAge();
                row["birthday"] = dependent.DateOfBirth.ToString("dd MMM yyyy");
                row["studying"] = dependent.IsStudying ? "Yes" : "No";
                row["remarks"] = dependent.Remarks;
                table.Rows.Add(row);
            }

            _grid.DataSource = table;
            _grid.AutoGenerateColumns = true;

            if (_grid.Columns.Contains("dependent_id")) _grid.Columns["dependent_id"].Visible = false;
            if (_grid.Columns.Contains("name")) _grid.Columns["name"].FillWeight = 150;
            if (_grid.Columns.Contains("age")) _grid.Columns["age"].FillWeight = 40;
            if (_grid.Columns.Contains("studying")) _grid.Columns["studying"].FillWeight = 50;

            int members = dependents.Count + 1;   // the head counts too

            _summary.Text = dependents.Count + " dependent(s) on the list. Household size, including "
                          + _head.GetFullName() + ": " + members + ".";
            _headBox.NoteText = "Household size " + members + "  |  " + _head.Purok;
        }

        private void FillFromGrid()
        {
            if (_grid.CurrentRow == null) return;
            if (_grid.CurrentRow.Cells["dependent_id"].Value == DBNull.Value) return;

            int id = Convert.ToInt32(_grid.CurrentRow.Cells["dependent_id"].Value);
            Dependent dependent = Find(id);
            if (dependent == null) return;

            _name.Text = dependent.FullName;
            _relation.SelectedIndex = (int)dependent.Relation;
            _birthday.Value = dependent.DateOfBirth > DateTime.MinValue ? dependent.DateOfBirth : DateTime.Today;
            _studying.Checked = dependent.IsStudying;
            _remarks.Text = dependent.Remarks;
        }

        private Dependent Find(int dependentId)
        {
            foreach (Dependent dependent in _residents.GetDependents(_head.ResidentId))
                if (dependent.DependentId == dependentId) return dependent;

            return null;
        }

        private Dependent Selected()
        {
            if (_grid.CurrentRow == null) return null;
            if (_grid.CurrentRow.Cells["dependent_id"].Value == DBNull.Value) return null;

            return Find(Convert.ToInt32(_grid.CurrentRow.Cells["dependent_id"].Value));
        }

        // ==================================================================
        //  Actions
        // ==================================================================

        private Dependent Gather(Dependent dependent)
        {
            Dependent result = dependent ?? new Dependent();

            result.HeadResidentId = _head.ResidentId;
            result.FullName = (_name.Text ?? string.Empty).Trim();
            result.Relation = (DependentRelation)Math.Max(_relation.SelectedIndex, 0);
            result.DateOfBirth = _birthday.Value.Date;
            result.IsStudying = _studying.Checked;
            result.Remarks = (_remarks.Text ?? string.Empty).Trim();

            return result;
        }

        private void AddDependent()
        {
            Dependent dependent = Gather(null);

            IList<string> problems = InputValidator.ValidateDependent(dependent);
            if (problems.Count > 0)
            {
                Dialog.Warn(this, InputValidator.Describe(problems), "Please check the details");
                return;
            }

            if (!Dialog.ConfirmChange(this, "Add " + dependent.FullName + " ("
                + dependent.GetRelationText() + ", age " + dependent.GetAge()
                + ") to " + _head.GetFullName() + "'s household?")) return;

            OperationResult<Dependent> result = _residents.AddDependent(_head, dependent);

            if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

            Message = result.Message;
            ClearEntry();
            LoadDependents();
            Say(result.Message);
        }

        private void UpdateDependent()
        {
            Dependent dependent = Selected();
            if (dependent == null)
            {
                Dialog.Warn(this, "Please choose a household member from the list first.", "Nobody chosen");
                return;
            }

            Dependent changed = Gather(dependent);

            IList<string> problems = InputValidator.ValidateDependent(changed);
            if (problems.Count > 0)
            {
                Dialog.Warn(this, InputValidator.Describe(problems), "Please check the details");
                return;
            }

            if (!Dialog.ConfirmChange(this, "Save the changes to " + changed.FullName + "?")) return;

            OperationResult result = _residents.UpdateDependent(_head, changed);

            if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

            Message = result.Message;
            LoadDependents();
            Say(result.Message);
        }

        private void RemoveDependent()
        {
            Dependent dependent = Selected();
            if (dependent == null)
            {
                Dialog.Warn(this, "Please choose a household member from the list first.", "Nobody chosen");
                return;
            }

            if (!Dialog.ConfirmChange(this, "Take " + dependent.FullName + " off "
                + _head.GetFullName() + "'s household list?" + Environment.NewLine
                + "The person is not deleted from the records - only from this household.")) return;

            OperationResult result = _residents.RemoveDependent(_head, dependent);

            if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

            Message = result.Message;
            ClearEntry();
            LoadDependents();
            Say(result.Message);
        }

        private void ClearEntry()
        {
            _name.Text = string.Empty;
            _relation.SelectedIndex = 0;
            _birthday.Value = DateTime.Today;
            _studying.Checked = false;
            _remarks.Text = string.Empty;
        }

        private void Say(string message)
        {
            Text = message.Length > 80 ? "Household of " + _head.GetFullName() : Text;
        }
    }
}
