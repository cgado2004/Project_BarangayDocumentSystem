using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Forms;

/// <summary>
/// Register or edit a resident. Used for both, because the fields are
/// identical — passing a resident to the constructor switches it to edit mode.
/// </summary>
public partial class ResidentForm : Form
{
    private readonly Resident? _editing;

    public string FirstName { get; private set; } = string.Empty;
    public string MiddleName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Suffix { get; private set; } = string.Empty;
    public DateTime DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public CivilStatus CivilStatus { get; private set; }
    public string Purok { get; private set; } = string.Empty;
    public string AddressLine { get; private set; } = string.Empty;
    public string ContactNumber { get; private set; } = string.Empty;
    public string Occupation { get; private set; } = string.Empty;
    public DateTime DateOfResidency { get; private set; }
    public bool IsVoter { get; private set; }
    public ResidentClassification Classification { get; private set; }

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
            "Purok 1", "Purok 2", "Purok 3", "Purok 4",
            "Purok 5", "Purok 6", "Purok 7"
        });

        cmbCivilStatus.Items.AddRange(Enum.GetNames<CivilStatus>().Cast<object>().ToArray());

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

        // --- edit mode: populate from the existing record ---
        Text = $"Edit Resident — {_editing.GetFullName()}";

        txtFirstName.Text  = _editing.FirstName;
        txtMiddleName.Text = _editing.MiddleName;
        txtLastName.Text   = _editing.LastName;
        txtSuffix.Text     = _editing.Suffix;
        dtpBirth.Value     = _editing.DateOfBirth == default
            ? new DateTime(2000, 1, 1) : _editing.DateOfBirth;

        radMale.Checked   = _editing.Gender == Models.Gender.Male;
        radFemale.Checked = _editing.Gender == Models.Gender.Female;

        cmbCivilStatus.SelectedItem = _editing.CivilStatus.ToString();
        cmbPurok.Text        = _editing.Purok;
        txtAddress.Text      = _editing.AddressLine;
        txtContact.Text      = _editing.ContactNumber;
        txtOccupation.Text   = _editing.Occupation;
        dtpResidency.Value   = _editing.DateOfResidency;
        chkVoter.Checked     = _editing.IsRegisteredVoter;

        chkSenior.Checked     = _editing.HasClassification(ResidentClassification.SeniorCitizen);
        chkPwd.Checked        = _editing.HasClassification(ResidentClassification.PWD);
        chkIndigent.Checked   = _editing.HasClassification(ResidentClassification.Indigent);
        chkStudent.Checked    = _editing.HasClassification(ResidentClassification.Student);
        chkSoloParent.Checked = _editing.HasClassification(ResidentClassification.SoloParent);
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        if (!ValidateInput())
            return;

        FirstName       = txtFirstName.Text.Trim();
        MiddleName      = txtMiddleName.Text.Trim();
        LastName        = txtLastName.Text.Trim();
        Suffix          = txtSuffix.Text.Trim();
        DateOfBirth     = dtpBirth.Value.Date;
        Gender          = radFemale.Checked ? Models.Gender.Female : Models.Gender.Male;
        CivilStatus     = Enum.Parse<CivilStatus>(cmbCivilStatus.Text);
        Purok           = cmbPurok.Text.Trim();
        AddressLine     = txtAddress.Text.Trim();
        ContactNumber   = txtContact.Text.Trim();
        Occupation      = txtOccupation.Text.Trim();
        DateOfResidency = dtpResidency.Value.Date;
        IsVoter         = chkVoter.Checked;

        // Build the flags enum with bitwise OR — a resident may hold several.
        Classification = ResidentClassification.None;
        if (chkSenior.Checked)     Classification |= ResidentClassification.SeniorCitizen;
        if (chkPwd.Checked)        Classification |= ResidentClassification.PWD;
        if (chkIndigent.Checked)   Classification |= ResidentClassification.Indigent;
        if (chkStudent.Checked)    Classification |= ResidentClassification.Student;
        if (chkSoloParent.Checked) Classification |= ResidentClassification.SoloParent;

        DialogResult = DialogResult.OK;
        Close();
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(txtFirstName.Text))
        {
            ShowError("First name is required.", txtFirstName);
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtLastName.Text))
        {
            ShowError("Last name is required.", txtLastName);
            return false;
        }

        if (dtpBirth.Value.Date > DateTime.Today)
        {
            ShowError("Date of birth cannot be in the future.", dtpBirth);
            return false;
        }

        if (dtpBirth.Value.Date < DateTime.Today.AddYears(-130))
        {
            ShowError("Please check the date of birth — that age is not plausible.", dtpBirth);
            return false;
        }

        if (dtpResidency.Value.Date > DateTime.Today)
        {
            ShowError("Date of residency cannot be in the future.", dtpResidency);
            return false;
        }

        if (dtpResidency.Value.Date < dtpBirth.Value.Date)
        {
            ShowError("Date of residency cannot be before the date of birth.", dtpResidency);
            return false;
        }

        if (string.IsNullOrWhiteSpace(cmbPurok.Text))
        {
            ShowError("Purok is required.", cmbPurok);
            return false;
        }

        string contact = txtContact.Text.Trim();
        if (string.IsNullOrWhiteSpace(contact))
        {
            ShowError("Contact number is required.", txtContact);
            return false;
        }

        if (!contact.All(c => char.IsDigit(c) || c is '+' or '-' or ' '))
        {
            ShowError("Contact number may only contain digits, spaces, + and -.", txtContact);
            return false;
        }

        // A senior citizen classification should match the actual age.
        int age = DateTime.Today.Year - dtpBirth.Value.Year;
        if (dtpBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;

        if (chkSenior.Checked && age < 60)
        {
            var proceed = MessageBox.Show(
                $"This resident is {age} years old, which is under 60.\n\n" +
                "Senior citizen status normally begins at 60. Tick it anyway?",
                "Check senior citizen status",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (proceed != DialogResult.Yes)
            {
                chkSenior.Focus();
                return false;
            }
        }

        return true;
    }

    private void ShowError(string message, Control focus)
    {
        MessageBox.Show(message, "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        focus.Focus();
        if (focus is TextBox tb) tb.SelectAll();
    }

    /// <summary>Rejects characters that cannot appear in a phone number.</summary>
    private void txtContact_KeyPress(object sender, KeyPressEventArgs e)
    {
        bool allowed = char.IsControl(e.KeyChar)
                       || char.IsDigit(e.KeyChar)
                       || e.KeyChar is '+' or '-' or ' ';

        if (!allowed)
            e.Handled = true;
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
