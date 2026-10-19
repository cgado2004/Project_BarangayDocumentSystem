using System.Windows.Forms;
using System.Drawing;
using System;
using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Forms;

// Previews the fee before filing.
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

    /// <summary>True when the clerk ticked Student to confirm the student discount.</summary>
    public bool IsStudent { get; private set; }

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
        FeeAssessment assessment = _feeSchedule.Assess(_resident, type, chkStudent.Checked);

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

    // The student discount changes the fee, so re-run the same assessment.
    private void chkStudent_CheckedChanged(object sender, EventArgs e) =>
        cmbDocument_SelectedIndexChanged(sender, e);

    private void btnSubmit_Click(object sender, EventArgs e)
    {
        if (cmbDocument.SelectedIndex < 0)
        {
            Dialog.Warn("Please choose a document type.");
            return;
        }

        if (string.IsNullOrWhiteSpace(cmbPurpose.Text))
        {
            Dialog.Warn("Please state the purpose of the request.\n\n"
                      + "It is printed on the document.");
            cmbPurpose.Focus();
            return;
        }

        DocumentType = _types[cmbDocument.SelectedIndex];
        Purpose = cmbPurpose.Text.Trim();
        IsStudent = chkStudent.Checked;

        // Re-check eligibility at submit time as well — belt and braces, in
        // case the selection changed without firing the handler.
        if (DocumentType == DocumentType.FirstTimeJobseekerCertificate &&
            !_feeSchedule.CanIssueJobseekerCertificate(_resident, out string reason))
        {
            Dialog.Warn(reason, "Cannot issue this certificate");
            return;
        }

        FeeAssessment fee = _feeSchedule.Assess(_resident, DocumentType, chkStudent.Checked);
        string feeText = fee.IsWaived ? "FREE" : $"₱{fee.FinalFee:N2}";
        if (!Dialog.Confirm(
                $"File this request?\n\n" +
                $"Resident:  {_resident.GetFullName()}\n" +
                $"Document:  {cmbDocument.Text}\n" +
                $"Purpose:   {Purpose}\n" +
                $"Fee:       {feeText}" + (chkStudent.Checked ? "  (student discount)" : ""),
                "Confirm request"))
            return;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
