using System.Windows.Forms;
using System;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;
using System.Linq;

namespace BarangayDocumentSystem.Forms;

/// <summary>
/// Register or edit a resident.
///
/// ── DRY, two ways ───────────────────────────────────────────────────────
/// 1. VALIDATION. v1 repeated a six-line "check, warn, focus, select, return
///    false" block for every field. Each is now one call to InputValidator,
///    and the whole method reads as a chain of conditions.
///
/// 2. OUTPUT. v1 exposed fourteen separate properties, which the caller then
///    copied one by one. This form now exposes ONE ResidentDetails record, so
///    adding a field does not touch any call site.
/// </summary>
public partial class ResidentForm : Form
{
    private readonly Resident? _editing;

    /// <summary>Everything the caller needs, in one object.</summary>
    public ResidentDetails Details { get; private set; } = null!;

    public ResidentForm() : this(null) { }

    public ResidentForm(Resident? existing)
    {
        InitializeComponent();
        _editing = existing;
    }

    private void ResidentForm_Load(object sender, EventArgs e)
    {
        cmbPurok.Items.AddRange(new object[]
        {
            "Purok 1", "Purok 2", "Purok 3", "Purok 4", "Purok 5", "Purok 6", "Purok 7"
        });

        cmbCivilStatus.Items.AddRange(Enum.GetNames(typeof(CivilStatus)).Cast<object>().ToArray());

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

        txtFirstName.Text  = _editing.FirstName;
        txtMiddleName.Text = _editing.MiddleName;
        txtLastName.Text   = _editing.LastName;
        txtSuffix.Text     = _editing.Suffix;
        dtpBirth.Value     = _editing.DateOfBirth == default
            ? new DateTime(2000, 1, 1) : _editing.DateOfBirth;

        radMale.Checked   = _editing.Gender == Gender.Male;
        radFemale.Checked = _editing.Gender == Gender.Female;

        cmbCivilStatus.SelectedItem = _editing.CivilStatus.ToString();
        cmbPurok.Text      = _editing.Purok;
        txtAddress.Text    = _editing.AddressLine;
        txtContact.Text    = _editing.ContactNumber;
        txtOccupation.Text = _editing.Occupation;
        dtpResidency.Value = _editing.DateOfResidency;
        chkVoter.Checked   = _editing.IsRegisteredVoter;

        chkSenior.Checked     = _editing.HasClassification(ResidentClassification.SeniorCitizen);
        chkPwd.Checked        = _editing.HasClassification(ResidentClassification.PWD);
        chkIndigent.Checked   = _editing.HasClassification(ResidentClassification.Indigent);
        chkStudent.Checked    = _editing.HasClassification(ResidentClassification.Student);
        chkSoloParent.Checked = _editing.HasClassification(ResidentClassification.SoloParent);
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        if (!IsValid()) return;

        Details = new ResidentDetails(
            FirstName:         txtFirstName.Text.Trim(),
            MiddleName:        txtMiddleName.Text.Trim(),
            LastName:          txtLastName.Text.Trim(),
            Suffix:            txtSuffix.Text.Trim(),
            DateOfBirth:       dtpBirth.Value.Date,
            Gender:            radFemale.Checked ? Gender.Female : Gender.Male,
            CivilStatus:       (CivilStatus)Enum.Parse(typeof(CivilStatus), cmbCivilStatus.Text),
            Purok:             cmbPurok.Text.Trim(),
            AddressLine:       txtAddress.Text.Trim(),
            ContactNumber:     txtContact.Text.Trim(),
            Occupation:        txtOccupation.Text.Trim(),
            DateOfResidency:   dtpResidency.Value.Date,
            IsRegisteredVoter: chkVoter.Checked,
            Classification:    BuildClassification());

        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>
    /// v1: roughly 60 lines of repeated check-warn-focus blocks.
    /// Now: a readable chain, because InputValidator owns the repetition.
    /// </summary>
    private bool IsValid() =>
        InputValidator.Required(txtFirstName, "First name")
        && InputValidator.Required(txtLastName, "Last name")
        && InputValidator.PlausibleBirthDate(dtpBirth)
        && InputValidator.NotFuture(dtpResidency, "Date of residency")
        && InputValidator.NotBefore(dtpResidency, dtpBirth,
                                    "Date of residency", "the date of birth")
        && InputValidator.RequiredSelection(cmbPurok, "purok")
        && InputValidator.ContactNumber(txtContact)
        && SeniorAgeIsConsistent();

    /// <summary>
    /// Domain-specific check that does not generalise, so it stays here rather
    /// than being forced into InputValidator. Warns instead of blocking —
    /// early senior status exists in some edge cases.
    /// </summary>
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

    /// <summary>Combines the ticked boxes into the [Flags] enum.</summary>
    private ResidentClassification BuildClassification()
    {
        var result = ResidentClassification.None;

        if (chkSenior.Checked)     result |= ResidentClassification.SeniorCitizen;
        if (chkPwd.Checked)        result |= ResidentClassification.PWD;
        if (chkIndigent.Checked)   result |= ResidentClassification.Indigent;
        if (chkStudent.Checked)    result |= ResidentClassification.Student;
        if (chkSoloParent.Checked) result |= ResidentClassification.SoloParent;

        return result;
    }

    /// <summary>Layer one of validation — blocks bad characters as typed.</summary>
    private void txtContact_KeyPress(object sender, KeyPressEventArgs e) =>
        InputValidator.AllowDigitsOnly(e, "+- ");

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
