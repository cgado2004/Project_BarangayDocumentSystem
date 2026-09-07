using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Forms;

/// <summary>
/// Files a document request for a resident.
///
/// The fee is shown LIVE as the document type changes, including the legal
/// basis for any waiver, so the clerk can tell the resident what is owed and
/// why before anything is committed.
/// </summary>
public partial class RequestForm : Form
{
    private readonly Resident _resident;
    private readonly FeeSchedule _feeSchedule;

    // Parallel to the ComboBox items — index maps to enum value.
    private readonly DocumentType[] _types =
    {
        DocumentType.BarangayClearance,
        DocumentType.CertificateOfResidency,
        DocumentType.CertificateOfIndigency,
        DocumentType.BarangayBusinessClearance,
        DocumentType.BarangayID,
        DocumentType.FirstTimeJobseekerCertificate,
        DocumentType.CertificateOfGoodMoralCharacter
    };

    public DocumentType DocumentType { get; private set; }
    public string Purpose { get; private set; } = string.Empty;

    public RequestForm(Resident resident, FeeSchedule feeSchedule)
    {
        InitializeComponent();
        _resident = resident ?? throw new ArgumentNullException(nameof(resident));
        _feeSchedule = feeSchedule ?? throw new ArgumentNullException(nameof(feeSchedule));
    }

    private void RequestForm_Load(object sender, EventArgs e)
    {
        lblResidentValue.Text =
            $"{_resident.GetFullName()}  ({_resident.Purok}, {_resident.GetAge()} yrs)";

        lblClassValue.Text = _resident.GetClassificationText();
        lblResidencyValue.Text =
            $"{_resident.GetMonthsOfResidency()} months " +
            $"(since {_resident.DateOfResidency:MMM yyyy})";

        cmbDocument.Items.AddRange(new object[]
        {
            "Barangay Clearance",
            "Certificate of Residency",
            "Certificate of Indigency",
            "Barangay Business Clearance",
            "Barangay ID",
            "First-Time Jobseeker Certificate (RA 11261)",
            "Certificate of Good Moral Character"
        });

        cmbPurpose.Items.AddRange(new object[]
        {
            "Employment Requirement",
            "Scholarship Application",
            "Bank Account Opening",
            "Medical Assistance",
            "Financial Assistance",
            "Business Permit Application",
            "School Requirement",
            "Police / NBI Clearance",
            "Legal Purposes",
            "Travel Requirement"
        });

        cmbDocument.SelectedIndex = 0;
    }

    /// <summary>
    /// Re-assesses the fee whenever the document type changes, and warns
    /// immediately if the resident cannot lawfully receive the RA 11261
    /// certificate.
    /// </summary>
    private void cmbDocument_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbDocument.SelectedIndex < 0) return;

        DocumentType type = _types[cmbDocument.SelectedIndex];
        FeeAssessment assessment = _feeSchedule.Assess(_resident, type);

        if (assessment.IsWaived)
        {
            lblFeeValue.Text = "FREE OF CHARGE";
            lblFeeValue.ForeColor = Color.FromArgb(21, 71, 52);
        }
        else
        {
            lblFeeValue.Text = $"₱{assessment.FinalFee:N2}";
            lblFeeValue.ForeColor = SystemColors.ControlText;
        }

        lblBasisValue.Text = assessment.Basis;

        // RA 11261 has statutory preconditions beyond the fee itself.
        if (type == DocumentType.FirstTimeJobseekerCertificate)
        {
            if (!_feeSchedule.CanIssueJobseekerCertificate(_resident, out string reason))
            {
                lblWarning.Text = "⚠ " + reason;
                lblWarning.Visible = true;
                btnSubmit.Enabled = false;
                return;
            }

            lblWarning.Text = "✓ Eligible under RA 11261.";
            lblWarning.ForeColor = Color.FromArgb(21, 71, 52);
            lblWarning.Visible = true;
            btnSubmit.Enabled = true;
            return;
        }

        lblWarning.Visible = false;
        btnSubmit.Enabled = true;
    }

    private void btnSubmit_Click(object sender, EventArgs e)
    {
        if (cmbDocument.SelectedIndex < 0)
        {
            MessageBox.Show("Please choose a document type.", "Invalid input",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(cmbPurpose.Text))
        {
            MessageBox.Show("Please state the purpose of the request.\n\n" +
                            "It is printed on the document.", "Invalid input",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
            cmbPurpose.Focus();
            return;
        }

        DocumentType = _types[cmbDocument.SelectedIndex];
        Purpose = cmbPurpose.Text.Trim();

        // Re-check eligibility at submit time as well — belt and braces, in
        // case the selection changed without firing the handler.
        if (DocumentType == DocumentType.FirstTimeJobseekerCertificate &&
            !_feeSchedule.CanIssueJobseekerCertificate(_resident, out string reason))
        {
            MessageBox.Show(reason, "Cannot issue this certificate",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
