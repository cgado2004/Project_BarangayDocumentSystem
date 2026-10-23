using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Forms;

public partial class ResidentForm : Form
{
    private readonly Resident? _editing;
    private readonly List<Dependent> _dependents = new();

    public ResidentDetails Details { get; private set; } = null!;

    /// <summary>Ask "are you sure?" before accepting Save. The restore flow turns this off because it confirms on its own.</summary>
    public bool ConfirmOnSave { get; set; } = true;

    public ResidentForm() : this(null) { }

    public ResidentForm(Resident? existing)
    {
        InitializeComponent();
        UiFactory.MakeButtonsFlexible(this);
        _editing = existing;
    }

    private void ResidentForm_Load(object sender, EventArgs e)
    {
        SetUpDependentsGrid();
        if (_editing is not null) _dependents.AddRange(_editing.Dependents);
        RefreshDependents();

        cmbPurok.Items.AddRange(new object[]
        {
            "Purok 1", "Purok 2", "Purok 3", "Purok 4", "Purok 5", "Purok 6", "Purok 7"
        });

        cmbCivilStatus.Items.AddRange(Enum.GetNames(typeof(CivilStatus)).Cast<object>().ToArray());

        // Block invalid characters as the user types.
        txtFirstName.KeyPress += (s, ev) => InputValidator.AllowNameCharsOnly(ev);
        txtMiddleName.KeyPress += (s, ev) => InputValidator.AllowNameCharsOnly(ev);
        txtLastName.KeyPress += (s, ev) => InputValidator.AllowNameCharsOnly(ev);
        txtSuffix.KeyPress += (s, ev) => InputValidator.AllowNameCharsOnly(ev);

        if (_editing is null)
        {
            Text = "Register Resident";
            cmbPurok.SelectedIndex = 0;
            cmbCivilStatus.SelectedIndex = 0;
            radMale.Checked = true;
            dtpBirth.Value = new DateTime(2000, 1, 1);
            dtpResidency.Value = DateTime.Today;
            return;
        }

        Text = $"Edit Resident — {_editing.GetFullName()}";

        txtFirstName.Text = _editing.FirstName;
        txtMiddleName.Text = _editing.MiddleName;
        txtLastName.Text = _editing.LastName;
        txtSuffix.Text = _editing.Suffix;
        dtpBirth.Value = _editing.DateOfBirth == default
            ? new DateTime(2000, 1, 1) : _editing.DateOfBirth;

        radMale.Checked = _editing.Gender == Gender.Male;
        radFemale.Checked = _editing.Gender == Gender.Female;

        cmbCivilStatus.SelectedItem = _editing.CivilStatus.ToString();
        cmbPurok.Text = _editing.Purok;
        txtHouseNumber.Text = _editing.HouseNumber;
        txtContact.Text = _editing.ContactNumber;
        txtOccupation.Text = _editing.Occupation;
        dtpResidency.Value = _editing.DateOfResidency;
        chkVoter.Checked = _editing.IsRegisteredVoter;

        chkSenior.Checked = _editing.HasClassification(ResidentClassification.SeniorCitizen);
        chkPwd.Checked = _editing.HasClassification(ResidentClassification.PWD);
        chkIndigent.Checked = _editing.HasClassification(ResidentClassification.Indigent);
        chkSoloParent.Checked = _editing.HasClassification(ResidentClassification.SoloParent);
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        if (!IsValid()) return;
        if (!NamesAreValid()) return;

        Details = new ResidentDetails(
            FirstName: txtFirstName.Text.Trim(),
            MiddleName: txtMiddleName.Text.Trim(),
            LastName: txtLastName.Text.Trim(),
            Suffix: txtSuffix.Text.Trim(),
            DateOfBirth: dtpBirth.Value.Date,
            Gender: radFemale.Checked ? Gender.Female : Gender.Male,
            CivilStatus: (CivilStatus)Enum.Parse(typeof(CivilStatus), cmbCivilStatus.Text),
            Purok: cmbPurok.Text.Trim(),
            HouseNumber: txtHouseNumber.Text.Trim(),
            ContactNumber: txtContact.Text.Trim(),
            Occupation: txtOccupation.Text.Trim(),
            DateOfResidency: dtpResidency.Value.Date,
            IsRegisteredVoter: chkVoter.Checked,
            Classification: BuildClassification(),
            Dependents: _dependents.ToList());

        if (ConfirmOnSave)
        {
            string name = $"{Details.FirstName} {Details.LastName}".Trim();
            bool adding = _editing is null;
            if (!Dialog.Confirm(
                    adding ? $"Add {name} ({Details.Purok}) to the residents list?"
                           : $"Save the changes to {name}'s record?",
                    adding ? "Confirm new resident" : "Confirm changes"))
                return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    // Regex check — blocks digits and special characters beyond the allowlist.
    private bool NamesAreValid()
    {
        if (!InputValidator.IsValidName(txtFirstName.Text))
        {
            Dialog.Warn("First name cannot contain special characters or numbers.");
            txtFirstName.Focus();
            return false;
        }

        if (!string.IsNullOrWhiteSpace(txtMiddleName.Text) &&
            !InputValidator.IsValidName(txtMiddleName.Text))
        {
            Dialog.Warn("Middle name cannot contain special characters or numbers.");
            txtMiddleName.Focus();
            return false;
        }

        if (!InputValidator.IsValidName(txtLastName.Text))
        {
            Dialog.Warn("Last name cannot contain special characters or numbers.");
            txtLastName.Focus();
            return false;
        }

        return true;
    }

    private bool IsValid() =>
        InputValidator.Required(txtFirstName, "First name")
        && InputValidator.Required(txtLastName, "Last name")
        && InputValidator.ValidSuffix(txtSuffix)
        && InputValidator.PlausibleBirthDate(dtpBirth)
        && InputValidator.NotFuture(dtpResidency, "Date of residency")
        && InputValidator.NotBefore(dtpResidency, dtpBirth,
                                    "Date of residency", "the date of birth")
        && InputValidator.RequiredSelection(cmbPurok, "purok")
        && InputValidator.ContactNumber11(txtContact)
        && SeniorAgeIsConsistent();

    private bool SeniorAgeIsConsistent()
    {
        if (!chkSenior.Checked) return true;

        int age = DateTime.Today.Year - dtpBirth.Value.Year;
        if (dtpBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;

        if (age >= 60) return true;

        if (Dialog.Confirm(
                $"This resident is {age} years old, which is under 60.\n\n" +
                "Senior citizen status normally begins at 60. Tick it anyway?",
                "Check senior citizen status"))
            return true;

        chkSenior.Focus();
        return false;
    }

    private ResidentClassification BuildClassification()
    {
        var result = ResidentClassification.None;

        if (chkSenior.Checked) result |= ResidentClassification.SeniorCitizen;
        if (chkPwd.Checked) result |= ResidentClassification.PWD;
        if (chkIndigent.Checked) result |= ResidentClassification.Indigent;
        if (chkSoloParent.Checked) result |= ResidentClassification.SoloParent;

        return result;
    }

    private void txtContact_KeyPress(object sender, KeyPressEventArgs e) =>
        InputValidator.AllowDigitsOnly(e);

    // ---- Dependents tab ----------------------------------------------------

    private void SetUpDependentsGrid()
    {
        UiFactory.StyleGrid(dgvDependents);
        dgvDependents.Columns.Clear();
        dgvDependents.Columns.Add("colName", "FULL NAME");
        dgvDependents.Columns.Add("colRelationship", "RELATIONSHIP");
        dgvDependents.Columns.Add("colBirth", "DATE OF BIRTH");
        dgvDependents.Columns.Add("colAge", "AGE");
        dgvDependents.Columns["colName"]!.FillWeight = 40;
        dgvDependents.Columns["colRelationship"]!.FillWeight = 25;
        dgvDependents.Columns["colBirth"]!.FillWeight = 22;
        dgvDependents.Columns["colAge"]!.FillWeight = 13;
    }

    private void RefreshDependents(int selectIndex = -1)
    {
        dgvDependents.Rows.Clear();
        foreach (var d in _dependents)
        {
            dgvDependents.Rows.Add(
                d.FullName,
                d.Relationship,
                d.DateOfBirth is { } born ? born.ToString("MMM d, yyyy") : "\u2014",
                d.GetAge()?.ToString() ?? "\u2014");
        }

        if (selectIndex >= 0 && selectIndex < dgvDependents.Rows.Count)
        {
            dgvDependents.ClearSelection();
            dgvDependents.Rows[selectIndex].Selected = true;
            dgvDependents.CurrentCell = dgvDependents.Rows[selectIndex].Cells[0];
        }

        tabDependents.Text = _dependents.Count == 0 ? "Dependents" : $"Dependents ({_dependents.Count})";
    }

    private int SelectedDependentIndex() =>
        dgvDependents.CurrentRow is { } row && row.Index < _dependents.Count ? row.Index : -1;

    private void btnAddDependent_Click(object sender, EventArgs e)
    {
        using var dialog = new DependentForm();
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is null) return;

        _dependents.Add(dialog.Result);
        RefreshDependents(_dependents.Count - 1);
    }

    private void btnEditDependent_Click(object sender, EventArgs e) => EditSelectedDependent();

    private void dgvDependents_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0) EditSelectedDependent();
    }

    private void EditSelectedDependent()
    {
        int index = SelectedDependentIndex();
        if (index < 0) { Dialog.SelectFirst("dependent"); return; }

        using var dialog = new DependentForm(_dependents[index]);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is null) return;

        _dependents[index] = dialog.Result;
        RefreshDependents(index);
    }

    private void btnRemoveDependent_Click(object sender, EventArgs e)
    {
        int index = SelectedDependentIndex();
        if (index < 0) { Dialog.SelectFirst("dependent"); return; }

        if (!Dialog.Confirm($"Remove {_dependents[index].FullName} from this resident's dependents?",
                            "Remove dependent"))
            return;

        _dependents.RemoveAt(index);
        RefreshDependents(Math.Min(index, _dependents.Count - 1));
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}