#nullable enable
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Forms;

public partial class ResidentForm : Form
{
    private readonly Resident? _editing;
    private readonly List<Dependent> _dependents = new();
    private DataGridView _dependentsGrid = null!;

    public ResidentDetails Details { get; private set; } = null!;


    public bool ConfirmOnSave { get; set; } = true;

    public ResidentForm() : this(null) { }

    public ResidentForm(Resident? existing)
    {
        InitializeComponent();
        DialogLayout.MakeScrollable(this);
        _editing = existing;
        BuildDependentsSection();
    }

    private void ResidentForm_Load(object sender, EventArgs e)
    {
        cmbPurok.Items.AddRange(new object[]
        {
            "Purok 1", "Purok 2", "Purok 3", "Purok 4", "Purok 5", "Purok 6", "Purok 7"
        });

        cmbCivilStatus.Items.AddRange(Enum.GetNames(typeof(CivilStatus)).Cast<object>().ToArray());


        txtFirstName.KeyPress += (s, ev) => InputValidator.AllowNameCharsOnly(ev);
        txtMiddleName.KeyPress += (s, ev) => InputValidator.AllowNameCharsOnly(ev);
        txtLastName.KeyPress += (s, ev) => InputValidator.AllowNameCharsOnly(ev);
        txtSuffix.KeyPress += (s, ev) => InputValidator.AllowNameCharsOnly(ev);

        if (_editing is null)
        {
            Text = "Add Resident";
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
        radOther.Checked = _editing.Gender == Gender.Other;

        cmbCivilStatus.SelectedItem = _editing.CivilStatus.ToString();
        cmbPurok.Text = _editing.Purok;
        txtAddress.Text = _editing.HouseNumber;
        txtContact.Text = _editing.ContactNumber;
        txtOccupation.Text = _editing.Occupation;
        dtpResidency.Value = _editing.DateOfResidency;
        chkVoter.Checked = _editing.IsRegisteredVoter;

        chkSenior.Checked = _editing.HasClassification(ResidentClassification.SeniorCitizen);
        chkPwd.Checked = _editing.HasClassification(ResidentClassification.PWD);
        chkIndigent.Checked = _editing.HasClassification(ResidentClassification.Indigent);
        chkSoloParent.Checked = _editing.HasClassification(ResidentClassification.SoloParent);

        _dependents.AddRange(_editing.Dependents);
        RefreshDependents();
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
            Gender: radFemale.Checked ? Gender.Female : radOther.Checked ? Gender.Other : Gender.Male,
            CivilStatus: (CivilStatus)Enum.Parse(typeof(CivilStatus), cmbCivilStatus.Text),
            Purok: cmbPurok.Text.Trim(),
            HouseNumber: txtAddress.Text.Trim(),
            ContactNumber: txtContact.Text.Trim(),
            Occupation: txtOccupation.Text.Trim(),
            DateOfResidency: dtpResidency.Value.Date,
            IsRegisteredVoter: chkVoter.Checked,
            Classification: BuildClassification(),
            Dependents: _dependents);

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

        const ResidentClassification editable = ResidentClassification.SeniorCitizen
            | ResidentClassification.PWD | ResidentClassification.Indigent | ResidentClassification.SoloParent;
        var result = (_editing?.Classification ?? ResidentClassification.None) & ~editable;

        if (chkSenior.Checked) result |= ResidentClassification.SeniorCitizen;
        if (chkPwd.Checked) result |= ResidentClassification.PWD;
        if (chkIndigent.Checked) result |= ResidentClassification.Indigent;
        if (chkSoloParent.Checked) result |= ResidentClassification.SoloParent;

        return result;
    }

    private void txtContact_KeyPress(object sender, KeyPressEventArgs e) =>
        InputValidator.AllowDigitsOnly(e);

    private void BuildDependentsSection()
    {
        _dependentsGrid = new DataGridView { Name = "dependentsGrid" };
        UiFactory.StyleGrid(_dependentsGrid);
        _dependentsGrid.Location = new Point(16, 26);
        _dependentsGrid.Size = new Size(842, 92);

        var add = UiFactory.PrimaryButton("Add dependent", 150);
        add.AutoSize = false;
        add.Location = new Point(16, 124);
        add.Size = new Size(150, 36);
        add.Click += (_, _) => AddDependent();

        var remove = UiFactory.SecondaryButton("Remove", 100);
        remove.AutoSize = false;
        remove.Location = new Point(176, 124);
        remove.Size = new Size(100, 36);
        remove.Click += (_, _) => RemoveDependent();

        var grp = new GroupBox
        {
            Text = "Dependents",
            Location = new Point(36, 524),
            Size = new Size(874, 168),
            Font = new Font("Segoe UI", 11F)
        };
        grp.Controls.Add(_dependentsGrid);
        grp.Controls.Add(add);
        grp.Controls.Add(remove);
        Controls.Add(grp);


        btnSave.Location = new Point(690, 704);
        btnCancel.Location = new Point(810, 704);
        ClientSize = new Size(946, 772);
    }

    private void AddDependent()
    {
        using var form = new DependentForm();
        if (form.ShowDialog(this) != DialogResult.OK || form.Result is null) return;
        _dependents.Add(form.Result);
        RefreshDependents();
    }

    private void RemoveDependent()
    {
        if (_dependentsGrid.CurrentRow?.Index is not int index || index < 0
            || index >= _dependents.Count)
        {
            Dialog.SelectFirst("dependent");
            return;
        }
        _dependents.RemoveAt(index);
        RefreshDependents();
    }

    private void RefreshDependents()
    {
        _dependentsGrid.DataSource = _dependents
            .Select(d => new
            {
                FullName = d.FullName,
                Relationship = d.Relationship,
                Age = d.GetAge()?.ToString() ?? "—"
            })
            .ToList();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
