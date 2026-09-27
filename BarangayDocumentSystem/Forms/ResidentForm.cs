// =====================================================================
//  PART:    Forms - add or edit a resident
//  ORIGIN:  the group's shared design - first modelled in Draft - Jonathan F. Del Rosario,
//           given this place in the tree by Fdraft - Frent Dhieniel Raborar;
//           the code and comments in this file are my v3.1 rewrite (leader_draft - Clint Wood Gado)
//  EDITS:   Clint Wood Gado - v3.1 content (real puroks, classification flags, validation), header
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Views;
using BarangayDocumentSystem.CustomControls;
using System.Windows.Forms;
using BarangayDocumentSystem.UIHelpers;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem.Forms;

/// <summary>
/// My add-or-edit resident dialog. I use the same dialog for both, so the
/// fields and the validation can never drift apart between them.
///
/// v3.1 notes on the shape of this class:
///   • it is a partial class with a parameterless constructor and an
///     InitializeComponent, which is what the Visual Studio designer needs
///     to open it on its design surface (core fix 1);
///   • the fields sit in a percent-sized UiFactory grid, so the dialog
///     reflows instead of clipping on a small screen (core fix 4);
///   • every rule it enforces comes from InputValidator, not from local
///     if-statements, so the request form enforces the very same rules.
/// </summary>
public partial class ResidentForm : DialogBase
{
    /// <summary>The details to save, set when the user confirms. Null when
    /// the dialog was cancelled.</summary>
    public ResidentDetails? Result { get; private set; }

    private readonly Resident? _existing;

    private readonly TextBox _first = new();
    private readonly TextBox _middle = new();
    private readonly TextBox _last = new();
    private readonly TextBox _suffix = new();
    private readonly DateTimePicker _dob = UiFactory.DatePicker();
    private readonly ComboBox _gender = UiFactory.ComboBox(Enum.GetNames(typeof(Gender)));
    private readonly ComboBox _civil = UiFactory.ComboBox(Enum.GetNames(typeof(CivilStatus)));
    private readonly ComboBox _purok = UiFactory.ComboBox(Puroks.All);
    private readonly DateTimePicker _residency = UiFactory.DatePicker();
    private readonly TextBox _address = new();
    private readonly TextBox _contact = new();
    private readonly TextBox _occupation = new();
    private readonly CheckBox _voter = UiFactory.CheckBox("Registered voter");
    private readonly CheckedListBox _classes = new();

    public ResidentForm()
    {
        InitializeComponent();
        BuildFields();

        _gender.SelectedIndex = 0;
        _civil.SelectedIndex = 0;
        _dob.Value = DateTime.Today.AddYears(-25);
    }

    public ResidentForm(Resident? existing) : this()
    {
        _existing = existing;
        if (existing is not null)
        {
            Text = "Edit resident";
            LoadFrom(existing);
        }
    }

    /// <summary>
    /// I lay the fields out in a four-column percent grid. Each column takes
    /// exactly a quarter of the width at every dialog size, which is the fix
    /// for the clipped right-hand fields the fixed-pixel v2 grid produced.
    /// </summary>
    private void BuildFields()
    {
        foreach (var c in new Control[] { _first, _middle, _last, _suffix,
                                          _address, _contact, _occupation })
        {
            c.Dock = DockStyle.Top;
            c.Font = Body;
            c.BackColor = Surface;
            c.ForeColor = Ink;
        }

        _purok.SelectedIndex = -1;

        _classes.Items.AddRange(new object[]
            { "Senior Citizen (RA 9994)", "PWD (RA 10754)", "Indigent (RA 11291)",
              "Student", "Solo Parent (RA 11861)" });
        _classes.CheckOnClick = true;
        _classes.Font = Body;
        _classes.BorderStyle = BorderStyle.FixedSingle;
        _classes.Height = 104;
        _classes.Dock = DockStyle.Top;

        var grid = UiFactory.Grid(4);

        int row = 0;
        void Add(string caption, Control control, int column, int span = 1)
        {
            grid.Controls.Add(UiFactory.Field(caption, control), column, row);
            if (span > 1) grid.SetColumnSpan(grid.Controls[grid.Controls.Count - 1], span);
        }

        Add("First name *", _first, 0);
        Add("Middle name", _middle, 1);
        Add("Last name *", _last, 2);
        Add("Suffix", _suffix, 3);
        row++;
        Add("Date of birth *", _dob, 0);
        Add("Gender", _gender, 1);
        Add("Civil status", _civil, 2);
        Add("Date of residency *", _residency, 3);
        row++;
        Add("Purok *", _purok, 0);
        Add("Address", _address, 1, 3);
        row++;
        Add("Contact number", _contact, 0);
        Add("Occupation", _occupation, 1);
        Add("Classifications", _classes, 2, 2);
        row++;

        _voter.Dock = DockStyle.Top;
        _voter.Height = 30;
        grid.Controls.Add(_voter, 0, row);
        grid.SetColumnSpan(_voter, 2);

        grid.Dock = DockStyle.Top;
        residentScroll.Controls.Add(grid);
    }

    /// <summary>I copy an existing resident's details into my fields.
    /// I named it LoadFrom rather than Load because Form already has a Load
    /// event, and hiding it would have been a trap for whoever reads this
    /// next.</summary>
    private void LoadFrom(Resident r)
    {
        _first.Text = r.FirstName;
        _middle.Text = r.MiddleName;
        _last.Text = r.LastName;
        _suffix.Text = r.Suffix;
        _dob.Value = r.DateOfBirth == default ? DateTime.Today.AddYears(-25) : r.DateOfBirth;
        _residency.Value = r.DateOfResidency == default ? DateTime.Today : r.DateOfResidency;
        _gender.SelectedItem = r.Gender.ToString();
        _civil.SelectedItem = r.CivilStatus.ToString();
        _purok.SelectedItem = r.Purok;
        _address.Text = r.AddressLine;
        _contact.Text = r.ContactNumber;
        _occupation.Text = r.Occupation;
        _voter.Checked = r.IsRegisteredVoter;

        _classes.SetItemChecked(0, r.HasClassification(ResidentClassification.SeniorCitizen));
        _classes.SetItemChecked(1, r.HasClassification(ResidentClassification.PWD));
        _classes.SetItemChecked(2, r.HasClassification(ResidentClassification.Indigent));
        _classes.SetItemChecked(3, r.HasClassification(ResidentClassification.Student));
        _classes.SetItemChecked(4, r.HasClassification(ResidentClassification.SoloParent));
    }

    private void Save(object? sender, EventArgs e)
    {
        // I validate here so a bad record never reaches the repository at
        // all. Every rule comes from InputValidator, so the request form and
        // this one can never disagree about what a valid phone number is.
        var checks = new[]
        {
            InputValidator.NamePart(_first.Text, "First name"),
            InputValidator.NamePart(_middle.Text, "Middle name", required: false),
            InputValidator.NamePart(_last.Text, "Last name"),
            InputValidator.Purok(_purok.SelectedItem?.ToString() ?? string.Empty),
            InputValidator.DateOfBirth(_dob.Value),
            InputValidator.ResidencyAfterBirth(_dob.Value, _residency.Value),
            InputValidator.PhoneNumber(_contact.Text)
        };

        foreach (var check in checks)
        {
            if (!check.Ok)
            {
                Dialog.FieldProblem(this, "Resident", check.Error);
                return;
            }
        }

        var classification = ResidentClassification.None;
        if (_classes.GetItemChecked(0)) classification |= ResidentClassification.SeniorCitizen;
        if (_classes.GetItemChecked(1)) classification |= ResidentClassification.PWD;
        if (_classes.GetItemChecked(2)) classification |= ResidentClassification.Indigent;
        if (_classes.GetItemChecked(3)) classification |= ResidentClassification.Student;
        if (_classes.GetItemChecked(4)) classification |= ResidentClassification.SoloParent;

        Result = new ResidentDetails(
            _first.Text.Trim(), _middle.Text.Trim(), _last.Text.Trim(), _suffix.Text.Trim(),
            _dob.Value.Date,
            (Gender)Enum.Parse(typeof(Gender), _gender.SelectedItem?.ToString() ?? nameof(Gender.Male)),
            (CivilStatus)Enum.Parse(typeof(CivilStatus), _civil.SelectedItem?.ToString() ?? nameof(CivilStatus.Single)),
            _purok.SelectedItem?.ToString() ?? string.Empty,
            _address.Text.Trim(), _contact.Text.Trim(), _occupation.Text.Trim(),
            _residency.Value.Date, _voter.Checked, classification);

        DialogResult = DialogResult.OK;
        Close();
    }

    private void Cancel(object? sender, EventArgs e) => Close();
}
