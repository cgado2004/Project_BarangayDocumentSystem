// ---------------------------------------------------------------------------
//  ResidentForm.cs - registering or changing one resident, and the household.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.UI.Controls;
using BarangayDocumentSystem.UI.Dialogs;

namespace BarangayDocumentSystem.UI.Forms
{
    /// <summary>
    /// The resident form - one window for registering a person and for the
    /// household they belong to, because the barangay asked for dependents to
    /// be part of registration rather than a separate errand.
    ///
    /// Four things about this window are answers to the review:
    ///
    ///  * the name sits in its own group box, on its own, so it reads the way a
    ///    name reads on paper and cannot be confused with anything else;
    ///  * there is no address box - the purok is the location, which is what
    ///    the census, the reports and the certificates have always used;
    ///  * Student is not on the classification list any more. It is a fee
    ///    category, so it is one tick box that says exactly that, and it
    ///    changes what a document costs rather than what the person is;
    ///  * a head of the family keeps the household list in this same window.
    /// </summary>
    public class ResidentForm : Form
    {
        private readonly ResidentService _residents;
        private readonly Resident _resident;
        private readonly IClock _clock;

        private TextBox _lastName;
        private TextBox _firstName;
        private TextBox _middleName;
        private TextBox _suffix;
        private ComboBox _sex;
        private ComboBox _civilStatus;
        private DateTimePicker _birthday;
        private TextBox _contact;
        private TextBox _occupation;
        private ComboBox _purok;
        private DateTimePicker _residencySince;
        private ComboBox _residencyStatus;
        private Label _suggestion;

        private CheckBox _senior;
        private CheckBox _pwd;
        private CheckBox _indigent;
        private CheckBox _soloParent;
        private CheckBox _fourPs;
        private CheckBox _student;
        private CheckBox _voter;
        private CheckBox _businessOwner;
        private CheckBox _headOfFamily;

        private DataGridView _dependents;
        private Button _addDependent;
        private Button _editDependent;
        private Button _removeDependent;
        private Label _householdNote;

        private Button _save;
        private Button _cancel;

        public string Message { get; private set; }

        public ResidentForm(ResidentService residents, SessionManager session, Resident resident,
                            bool isNew, IClock clock)
        {
            _residents = residents;
            _resident = resident;
            _clock = clock;

            BuildWindow(isNew);
            LoadResident(isNew);
        }

        // ==================================================================
        //  The window
        // ==================================================================

        private void BuildWindow(bool isNew)
        {
            Text = isNew ? "Register a resident" : "Resident - " + _resident.GetFullName();
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(960, 720);
            ClientSize = new Size(1000, 780);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Panel page = UiFactory.Page();
            page.Dock = DockStyle.Fill;

            Label heading = UiFactory.PageHeading(isNew ? "Register a resident" : "Resident record");
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin);

            Label caption = UiFactory.Caption(isNew
                ? "The barangay keeps a person's purok as their location - that is why there is no address "
                  + "line. Nobody is ever deleted; a record is deactivated or archived instead."
                : "Everything here is what the certificates and the census read. Changes are written into the "
                  + "activity log.");
            caption.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 34);
            caption.Size = new Size(940, 34);

            // ---- 1. the name, in a group box (as asked for) ----
            GroupBox nameBox = new GroupBox();
            nameBox.Text = "Name";
            nameBox.Font = AppTheme.SmallBold;
            nameBox.ForeColor = AppTheme.Primary;
            nameBox.BackColor = AppTheme.Surface;
            nameBox.FlatStyle = FlatStyle.Flat;
            nameBox.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 76);
            nameBox.Size = new Size(940, 108);
            nameBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _lastName = UiFactory.TextBox("Last name", 60);
            _firstName = UiFactory.TextBox("First name", 60);
            _middleName = UiFactory.TextBox("Middle name", 60);
            _suffix = UiFactory.TextBox("Jr., III", 10);

            Place(nameBox, "Last name", _lastName, AppTheme.Gap3, 26, 240);
            Place(nameBox, "First name", _firstName, AppTheme.Gap3 + 252, 26, 240);
            Place(nameBox, "Middle name", _middleName, AppTheme.Gap3 + 504, 26, 240);
            Place(nameBox, "Suffix", _suffix, AppTheme.Gap3 + 756, 26, 90);

            Label nameNote = UiFactory.Hint("This is the name that will be printed on every document for "
                + "this person, so please copy it exactly as it reads on their identification.");
            nameNote.Location = new Point(AppTheme.Gap3, 76);
            nameNote.Size = new Size(900, 20);
            nameBox.Controls.Add(nameNote);

            // ---- 2. who the person is ----
            GroupBox details = Card("Details", AppTheme.PageMargin + 194);
            Place(details, "Sex", _sex = UiFactory.DropDown(EnumNames<Gender>(), true), AppTheme.Gap3, 34, 170);
            Place(details, "Civil status", _civilStatus = UiFactory.DropDown(EnumNames<CivilStatus>(), true),
                AppTheme.Gap3 + 182, 34, 190);
            Place(details, "Date of birth", _birthday = UiFactory.DatePicker(new DateTime(1990, 1, 1)),
                AppTheme.Gap3 + 384, 34, 190);
            Place(details, "Contact number", _contact = UiFactory.TextBox("0917 000 0000", 20),
                AppTheme.Gap3 + 586, 34, 170);
            Place(details, "Occupation", _occupation = UiFactory.TextBox("e.g. vendor, teacher, none", 80),
                AppTheme.Gap3 + 768, 34, 150);

            details.Controls.Add(_senior = UiFactory.CheckBox("Senior citizen (RA 9994)", false));
            details.Controls.Add(_pwd = UiFactory.CheckBox("Person with disability (RA 10754)", false));
            details.Controls.Add(_indigent = UiFactory.CheckBox("Indigent household", false));
            details.Controls.Add(_soloParent = UiFactory.CheckBox("Solo parent", false));
            details.Controls.Add(_fourPs = UiFactory.CheckBox("4Ps beneficiary", false));
            details.Controls.Add(_voter = UiFactory.CheckBox("Registered voter", false));
            details.Controls.Add(_businessOwner = UiFactory.CheckBox("Owns a business in the barangay", false));

            _senior.Location = new Point(AppTheme.Gap3, 76);
            _pwd.Location = new Point(AppTheme.Gap3 + 250, 76);
            _indigent.Location = new Point(AppTheme.Gap3 + 500, 76);
            _soloParent.Location = new Point(AppTheme.Gap3, 104);
            _fourPs.Location = new Point(AppTheme.Gap3 + 250, 104);
            _voter.Location = new Point(AppTheme.Gap3 + 500, 104);
            _businessOwner.Location = new Point(AppTheme.Gap3, 132);
            _businessOwner.CheckedChanged += delegate (object sender, EventArgs e) { HouseholdChanged(); };

            Label classification = UiFactory.Hint("These are the classifications the barangay records. "
                + "Student is not one of them any more - it is a fee category, below.");
            classification.Location = new Point(AppTheme.Gap3 + 250, 134);
            classification.Size = new Size(640, 20);
            details.Controls.Add(classification);
            details.Height = 170;

            // ---- 3. residence ----
            GroupBox residence = Card("Residence in the barangay", AppTheme.PageMargin + 382);
            Place(residence, "Purok", _purok = UiFactory.DropDown(PurokList.ForDropDown(), true),
                AppTheme.Gap3, 34, 230);
            Place(residence, "Living in the barangay since", _residencySince = UiFactory.DatePicker(DateTime.Today),
                AppTheme.Gap3 + 242, 34, 230);
            Place(residence, "Residency status", _residencyStatus = UiFactory.DropDown(EnumNames<ResidencyStatus>(), true),
                AppTheme.Gap3 + 484, 34, 200);

            _residencySince.ValueChanged += delegate (object sender, EventArgs e) { SuggestStatus(); };

            _suggestion = UiFactory.Hint(string.Empty);
            _suggestion.Location = new Point(AppTheme.Gap3 + 696, 54);
            _suggestion.Size = new Size(220, 40);
            residence.Controls.Add(_suggestion);

            // ---- 4. the student fee category ----
            GroupBox fees = Card("Fees", AppTheme.PageMargin + 522);
            _student = UiFactory.CheckBox("Student fee category - discounted clearances and certifications "
                + "(" + AppConfig.StudentDiscountPercent + "% off)", false);
            _student.Location = new Point(AppTheme.Gap3, 40);
            fees.Controls.Add(_student);

            Label feeNote = UiFactory.Hint("A student is not a classification; it is a discount. The tick "
                + "here changes what a Barangay Clearance or a certification costs, and it is recorded "
                + "in the activity log when somebody ticks it.");
            feeNote.Location = new Point(AppTheme.Gap3, 68);
            feeNote.Size = new Size(900, 20);
            fees.Controls.Add(feeNote);
            fees.Height = 100;

            // ---- 5. the household ----
            SectionPanel household = new SectionPanel("Household",
                "Members of the family living with this resident. A dependent is counted in the census "
                + "and in the household size, but does not file requests of their own.");
            household.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 634);
            household.Size = new Size(940, 240);
            household.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _headOfFamily = UiFactory.CheckBox("This resident is the head of the family", false);
            _headOfFamily.Location = new Point(AppTheme.Gap4, 58);
            _headOfFamily.CheckedChanged += delegate (object sender, EventArgs e) { HouseholdChanged(); };
            household.Controls.Add(_headOfFamily);

            _dependents = UiFactory.Grid();
            _dependents.Location = new Point(AppTheme.Gap4, 88);
            _dependents.Size = new Size(600, 132);
            _dependents.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            household.Controls.Add(_dependents);

            _addDependent = UiFactory.SecondaryButton("Add a member");
            _addDependent.Location = new Point(680, 88);
            _addDependent.Click += delegate (object sender, EventArgs e) { AddDependent(); };

            _editDependent = UiFactory.SecondaryButton("Change");
            _editDependent.Location = new Point(680, 132);
            _editDependent.Click += delegate (object sender, EventArgs e) { EditDependent(); };

            _removeDependent = UiFactory.GhostButton("Remove");
            _removeDependent.Location = new Point(680, 176);
            _removeDependent.Click += delegate (object sender, EventArgs e) { RemoveDependent(); };

            household.Controls.Add(_addDependent);
            household.Controls.Add(_editDependent);
            household.Controls.Add(_removeDependent);

            _householdNote = UiFactory.Hint(string.Empty);
            _householdNote.Location = new Point(840, 92);
            _householdNote.Size = new Size(90, 120);
            household.Controls.Add(_householdNote);

            page.Controls.Add(heading);
            page.Controls.Add(caption);
            page.Controls.Add(nameBox);
            page.Controls.Add(details);
            page.Controls.Add(residence);
            page.Controls.Add(fees);
            page.Controls.Add(household);

            // ---- the buttons ----
            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 60;
            footer.BackColor = AppTheme.Surface;

            _save = UiFactory.PrimaryButton(isNew ? "Register this resident" : "Save the changes");
            _save.Location = new Point(ClientSize.Width - _save.Width - AppTheme.PageMargin, 12);
            _save.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _save.Click += delegate (object sender, EventArgs e) { Save(isNew); };

            _cancel = UiFactory.SecondaryButton("Cancel");
            _cancel.Location = new Point(_save.Left - _cancel.Width - AppTheme.Gap2, 12);
            _cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _cancel.Click += delegate (object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.Add(_save);
            footer.Controls.Add(_cancel);

            Controls.Add(page);
            Controls.Add(footer);

            AcceptButton = _save;
            CancelButton = _cancel;
        }

        private static GroupBox Card(string title, int top)
        {
            GroupBox box = new GroupBox();
            box.Text = title;
            box.Font = AppTheme.SmallBold;
            box.ForeColor = AppTheme.Primary;
            box.BackColor = AppTheme.Surface;
            box.FlatStyle = FlatStyle.Flat;
            box.Location = new Point(AppTheme.PageMargin, top);
            box.Size = new Size(940, 132);
            box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            return box;
        }

        private static void Place(Control parent, string caption, Control control, int x, int y, int width)
        {
            Label label = UiFactory.FieldLabel(caption);
            label.Location = new Point(x, y);
            label.Width = width;

            control.Location = new Point(x, y + 18);
            control.Width = width;

            parent.Controls.Add(label);
            parent.Controls.Add(control);
        }

        /// <summary>Says what just happened, in the household note and on the
        /// window itself, so a change inside this window is never silent.</summary>
        private void Say(string text)
        {
            Message = text;

            if (_householdNote != null) _householdNote.Text = text;

            Text = (text.Length > 70 ? text.Substring(0, 67) + "..." : text) + "  -  " + AppConfig.BarangayName;
        }

        private static string[] EnumNames<T>()
        {
            List<string> names = new List<string>();
            foreach (T value in (T[])Enum.GetValues(typeof(T))) names.Add(EnumText.Spaced(value.ToString()));
            return names.ToArray();
        }

        private static T FromDropDown<T>(ComboBox box)
        {
            return (T)Enum.ToObject(typeof(T), Math.Max(box.SelectedIndex, 0));
        }

        // ==================================================================
        //  Loading
        // ==================================================================

        private void LoadResident(bool isNew)
        {
            _sex.SelectedIndex = 0;
            _civilStatus.SelectedIndex = 0;
            _purok.SelectedIndex = 0;
            _residencyStatus.SelectedIndex = 1;

            if (!isNew)
            {
                _lastName.Text = _resident.LastName;
                _firstName.Text = _resident.FirstName;
                _middleName.Text = _resident.MiddleName;
                _suffix.Text = _resident.Suffix;
                _birthday.Value = _resident.DateOfBirth == default(DateTime) ? DateTime.Today : _resident.DateOfBirth;
                _contact.Text = _resident.ContactNumber;
                _occupation.Text = _resident.Occupation;

                _sex.SelectedIndex = (int)_resident.Gender;
                _civilStatus.SelectedIndex = (int)_resident.CivilStatus;

                int purok = IndexOf(_purok, _resident.Purok);
                _purok.SelectedIndex = purok >= 0 ? purok : 0;

                _residencySince.Value = _resident.DateOfResidency == default(DateTime)
                    ? DateTime.Today
                    : _resident.DateOfResidency;
                _residencyStatus.SelectedIndex = (int)_resident.ResidencyStatus;

                _senior.Checked = _resident.HasClassification(ResidentClassification.SeniorCitizen);
                _pwd.Checked = _resident.HasClassification(ResidentClassification.PWD);
                _indigent.Checked = _resident.HasClassification(ResidentClassification.Indigent);
                _soloParent.Checked = _resident.HasClassification(ResidentClassification.SoloParent);
                _fourPs.Checked = _resident.HasClassification(ResidentClassification.FourPsBeneficiary);

                _student.Checked = _resident.IsStudentFeeCategory;
                _voter.Checked = _resident.IsRegisteredVoter;
                _businessOwner.Checked = _resident.IsBusinessOwner;
                _headOfFamily.Checked = _resident.IsHeadOfFamily;
            }

            SuggestStatus();
            HouseholdChanged();
            LoadDependents();
        }

        private static int IndexOf(ComboBox box, string text)
        {
            for (int i = 0; i < box.Items.Count; i++)
                if (string.Equals(Convert.ToString(box.Items[i]), text, StringComparison.OrdinalIgnoreCase)) return i;

            return -1;
        }

        /// <summary>
        /// Suggests newcomer, temporary or permanent from how long the person
        /// has lived here, using the two numbers in the settings file.
        ///
        /// It is only a suggestion: the clerk can choose differently, because
        /// the barangay knows who has been here since before the record started
        /// being kept.
        /// </summary>
        private void SuggestStatus()
        {
            int months = 0;
            DateTime since = _residencySince.Value.Date;
            DateTime now = _clock == null ? DateTime.Today : _clock.Now();

            while (since.AddMonths(months + 1) <= now) months++;

            ResidencyStatus suggested = months < AppConfig.NewcomerMonths
                ? ResidencyStatus.Newcomer
                : (months >= AppConfig.PermanentResidencyYears * 12
                    ? ResidencyStatus.Permanent
                    : ResidencyStatus.Temporary);

            if (_resident == null || _resident.ResidencyStatus == default(ResidencyStatus))
                _residencyStatus.SelectedIndex = (int)suggested;

            _suggestion.Text = "Lived here " + months + " month(s). Suggested: "
                             + EnumText.Of(suggested) + ".";
        }

        private void HouseholdChanged()
        {
            bool head = _headOfFamily.Checked;

            _addDependent.Enabled = head;
            _editDependent.Enabled = head;
            _removeDependent.Enabled = head;

            HouseholdSize(out int members, out int dependents);

            _householdNote.Text = head
                ? members + " person(s) in this household, " + dependents + " of them dependents."
                : "Tick the box to list the members of this household.";
        }

        /// <summary>The household size: the head, plus everyone listed under
        /// them. The census, the reports and the documents all count the same
        /// way, so the numbers agree everywhere.</summary>
        private void HouseholdSize(out int members, out int dependents)
        {
            members = 1;
            dependents = 0;

            if (_resident == null || _resident.ResidentId <= 0) return;

            dependents = _residents.GetDependents(_resident.ResidentId).Count;
            members = dependents + 1;
        }

        private void LoadDependents()
        {
            DataTable table = new DataTable("dependents");
            table.Columns.Add("dependent_id", typeof(int));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("relation", typeof(string));
            table.Columns.Add("age", typeof(int));
            table.Columns.Add("studying", typeof(string));

            if (_resident != null && _resident.ResidentId > 0)
            {
                foreach (Dependent dependent in _residents.GetDependents(_resident.ResidentId))
                {
                    DataRow row = table.NewRow();
                    row["dependent_id"] = dependent.DependentId;
                    row["name"] = dependent.FullName;
                    row["relation"] = dependent.GetRelationText();
                    row["age"] = dependent.GetAge();
                    row["studying"] = dependent.IsStudying ? "still studying" : string.Empty;
                    table.Rows.Add(row);
                }
            }

            _dependents.DataSource = table;
            _dependents.AutoGenerateColumns = true;

            if (_dependents.Columns.Contains("dependent_id")) _dependents.Columns["dependent_id"].Visible = false;
            if (_dependents.Columns.Contains("name")) _dependents.Columns["name"].FillWeight = 150;

            HouseholdChanged();
        }

        // ==================================================================
        //  The household actions
        // ==================================================================

        private Dependent SelectedDependent()
        {
            if (_dependents.CurrentRow == null) return null;

            object id = _dependents.CurrentRow.Cells["dependent_id"].Value;
            if (id == null || id == DBNull.Value) return null;

            int wanted = Convert.ToInt32(id);
            foreach (Dependent dependent in _residents.GetDependents(_resident.ResidentId))
                if (dependent.DependentId == wanted) return dependent;

            return null;
        }

        private void AddDependent()
        {
            if (_resident.ResidentId <= 0)
            {
                Dialog.Warn(this, "Please save the resident first, then add the members of the household.",
                    "Save the resident first");
                return;
            }

            Dependent dependent = AskForDependent(null);
            if (dependent == null) return;

            OperationResult<Dependent> result = _residents.AddDependent(_resident, dependent);

            if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

            LoadDependents();
            Say(result.Message);
        }

        private void EditDependent()
        {
            Dependent existing = SelectedDependent();
            if (existing == null)
            {
                Dialog.Warn(this, "Please choose a member of the household from the list first.",
                    "Nobody chosen");
                return;
            }

            Dependent changed = AskForDependent(existing);
            if (changed == null) return;

            OperationResult result = _residents.UpdateDependent(_resident, changed);

            if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

            LoadDependents();
            Say(result.Message);
        }

        private void RemoveDependent()
        {
            Dependent existing = SelectedDependent();
            if (existing == null)
            {
                Dialog.Warn(this, "Please choose a member of the household from the list first.",
                    "Nobody chosen");
                return;
            }

            if (!Dialog.ConfirmChange(this, "Take " + existing.FullName + " off this household list?"
                + Environment.NewLine + "The person is removed from the household, not from the census.")) return;

            OperationResult result = _residents.RemoveDependent(_resident, existing);

            if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

            LoadDependents();
            Say(result.Message);
        }

        /// <summary>
        /// A small window for one member of the household. I build it here
        /// rather than in its own file because it is not a screen of its own -
        /// it only exists while somebody is typing a name, and it is never more
        /// than four boxes.
        /// </summary>
        private Dependent AskForDependent(Dependent existing)
        {
            using (Form prompt = new Form())
            {
                prompt.Text = existing == null ? "Add a member of the household" : "Change a member";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.MinimizeBox = false;
                prompt.MaximizeBox = false;
                prompt.ClientSize = new Size(520, 340);
                prompt.BackColor = AppTheme.Canvas;
                prompt.Font = AppTheme.Body;

                Label title = UiFactory.PageHeading(existing == null
                    ? "Add a member of the household"
                    : existing.FullName);
                title.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4);

                Label note = UiFactory.Caption("A member of the household is counted in the census and in "
                    + "the household size. They do not file requests of their own - when they do, they are "
                    + "registered as a resident.");
                note.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 36);
                note.Size = new Size(470, 44);

                TextBox name = UiFactory.TextBox("Full name", 120);
                name.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 92);
                name.Width = 470;
                name.Text = existing == null ? string.Empty : existing.FullName;

                ComboBox relation = UiFactory.DropDown(EnumNames<DependentRelation>(), true);
                relation.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 150);
                relation.Width = 200;
                relation.SelectedIndex = existing == null ? 0 : (int)existing.Relation;

                DateTimePicker birthday = UiFactory.DatePicker(existing == null || existing.DateOfBirth == default(DateTime)
                    ? DateTime.Today : existing.DateOfBirth);
                birthday.Location = new Point(AppTheme.PageMargin + 220, AppTheme.Gap4 + 150);
                birthday.Width = 250;

                CheckBox studying = UiFactory.CheckBox("Still studying", existing != null && existing.IsStudying);
                studying.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 196);

                TextBox remarks = UiFactory.TextBox("anything worth noting", 200);
                remarks.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 234);
                remarks.Width = 470;
                remarks.Text = existing == null ? string.Empty : existing.Remarks;

                Label relationLabel = UiFactory.FieldLabel("Relationship");
                relationLabel.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 132);
                relationLabel.Width = 200;

                Label nameLabel = UiFactory.FieldLabel("Full name");
                nameLabel.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 74);
                nameLabel.Width = 470;

                Button ok = UiFactory.PrimaryButton("Save");
                ok.Location = new Point(prompt.ClientSize.Width - ok.Width - AppTheme.PageMargin, 268);
                ok.DialogResult = DialogResult.OK;

                Button cancel = UiFactory.SecondaryButton("Cancel");
                cancel.Location = new Point(ok.Left - cancel.Width - AppTheme.Gap2, 268);
                cancel.DialogResult = DialogResult.Cancel;

                prompt.Controls.Add(title);
                prompt.Controls.Add(note);
                prompt.Controls.Add(nameLabel);
                prompt.Controls.Add(name);
                prompt.Controls.Add(relationLabel);
                prompt.Controls.Add(relation);
                prompt.Controls.Add(birthday);
                prompt.Controls.Add(studying);
                prompt.Controls.Add(remarks);
                prompt.Controls.Add(ok);
                prompt.Controls.Add(cancel);

                prompt.AcceptButton = ok;
                prompt.CancelButton = cancel;

                if (prompt.ShowDialog(this) != DialogResult.OK) return null;

                Dependent dependent = existing ?? new Dependent();
                dependent.HeadResidentId = _resident.ResidentId;
                dependent.FullName = name.Text.Trim();
                dependent.Relation = FromDropDown<DependentRelation>(relation);
                dependent.DateOfBirth = birthday.Value.Date;
                dependent.IsStudying = studying.Checked;
                dependent.Remarks = remarks.Text.Trim();
                dependent.CreatedBy = _resident.CreatedBy;

                IList<string> problems = InputValidator.ValidateDependent(dependent);
                if (problems.Count > 0)
                {
                    Dialog.Warn(this, Dialog.Problems(problems), "Please check the details");
                    return null;
                }

                return dependent;
            }
        }

        // ==================================================================
        //  Saving
        // ==================================================================

        private Resident Gather()
        {
            Resident resident = _resident;

            resident.LastName = _lastName.Text.Trim();
            resident.FirstName = _firstName.Text.Trim();
            resident.MiddleName = _middleName.Text.Trim();
            resident.Suffix = _suffix.Text.Trim();
            resident.Gender = FromDropDown<Gender>(_sex);
            resident.CivilStatus = FromDropDown<CivilStatus>(_civilStatus);
            resident.DateOfBirth = _birthday.Value.Date;
            resident.ContactNumber = _contact.Text.Trim();
            resident.Occupation = _occupation.Text.Trim();
            resident.Purok = Convert.ToString(_purok.SelectedItem);
            resident.DateOfResidency = _residencySince.Value.Date;
            resident.ResidencyStatus = FromDropDown<ResidencyStatus>(_residencyStatus);

            ResidentClassification flags = ResidentClassification.None;
            if (_senior.Checked) flags |= ResidentClassification.SeniorCitizen;
            if (_pwd.Checked) flags |= ResidentClassification.PWD;
            if (_indigent.Checked) flags |= ResidentClassification.Indigent;
            if (_soloParent.Checked) flags |= ResidentClassification.SoloParent;
            if (_fourPs.Checked) flags |= ResidentClassification.FourPsBeneficiary;
            resident.Classification = flags;

            // Student is a fee category, not a classification - and that is the
            // whole point of the change the barangay asked for.
            resident.IsStudentFeeCategory = _student.Checked;

            resident.IsRegisteredVoter = _voter.Checked;
            resident.IsBusinessOwner = _businessOwner.Checked;
            resident.IsHeadOfFamily = _headOfFamily.Checked;
            resident.CreatedBy = _resident.CreatedBy;

            return resident;
        }

        private void Save(bool isNew)
        {
            Resident resident = Gather();

            IList<string> problems = InputValidator.ValidateResident(resident);
            if (problems.Count > 0)
            {
                Dialog.Warn(this, Dialog.Problems(problems) + Environment.NewLine + Environment.NewLine
                    + "Nothing has been saved yet.", "Please check the form");
                return;
            }

            string question = (isNew ? "Register " : "Save the changes to ") + resident.GetFullName()
                + "?" + Environment.NewLine + Environment.NewLine
                + "Purok " + resident.Purok + ", " + resident.GetAge() + " years old, "
                + EnumText.Of(resident.ResidencyStatus) + "." + Environment.NewLine
                + "Classification: " + resident.GetClassificationText() + "." + Environment.NewLine
                + "Fee category: " + resident.GetFeeCategoryText() + ".";

            if (!Dialog.ConfirmChange(this, question)) return;

            Cursor = Cursors.WaitCursor;
            _save.Enabled = false;

            try
            {
                OperationResult result = Save(isNew, resident, false);
                if (!result.Succeeded && result.Message != null
                    && result.Message.IndexOf("same name", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Two people really can share a name and a birthday, so the
                    // clerk is asked rather than refused. The service still
                    // makes the decision - this only passes the answer on.
                    bool different = Dialog.Confirm(this, result.Message + Environment.NewLine + Environment.NewLine
                        + "Choose the first button only if this is a different person.",
                        "Somebody with the same name is already on file", null);

                    if (different) result = Save(isNew, resident, true);
                }

                if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

                Message = result.Message;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (RepositoryException error)
            {
                Dialog.Error(this, error.Message, "The database is not answering");
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not save the resident.", error);
            }
            finally
            {
                Cursor = Cursors.Default;
                _save.Enabled = true;
            }
        }

        private OperationResult Save(bool isNew, Resident resident, bool acceptAsNewResident)
        {
            if (isNew)
            {
                OperationResult<Resident> created = _residents.Register(resident, acceptAsNewResident);
                return created.Succeeded ? OperationResult.Ok(created.Message) : OperationResult.Fail(created.Message);
            }

            return _residents.Update(resident, acceptAsNewResident);
        }
    }
}
