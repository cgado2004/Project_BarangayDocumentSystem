using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Helper;

namespace BarangayDocumentSystem;

/// <summary>
/// Records payment of a document fee and captures the official receipt
/// number. RA 11032 requires an OR for every collection, which is why the
/// number is mandatory rather than optional.
/// </summary>
public partial class PaymentForm : Form
{
    private readonly DocumentRequest _request;

    public string OfficialReceiptNo { get; private set; } = string.Empty;

    public PaymentForm(DocumentRequest request)
    {
        InitializeComponent();
        _request = request ?? throw new ArgumentNullException(nameof(request));
    }

    private void PaymentForm_Load(object sender, EventArgs e)
    {
        lblRefValue.Text      = _request.GetReferenceNumber();
        lblDocumentValue.Text = _request.GetDocumentName();
        lblResidentValue.Text = _request.Resident.GetFullName();
        lblAmountValue.Text   = $"P{_request.Fee:N2}";
        txtOr.Focus();
    }

    private void btnConfirm_Click(object sender, EventArgs e)
    {
        string or = txtOr.Text.Trim();

        if (string.IsNullOrWhiteSpace(or))
        {
            Dialog.Warn("An official receipt number is required.\n\n" +
                "RA 11032 requires an OR to be issued for every collection.");
            txtOr.Focus();
            return;
        }

        if (or.Length < 3)
        {
            Dialog.Warn("That receipt number looks too short. Please check it.");
            txtOr.Focus();
            txtOr.SelectAll();
            return;
        }

        OfficialReceiptNo = or;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
