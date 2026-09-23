using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helper;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Service;
using static BarangayDocumentSystem.Helper.AppTheme;

namespace BarangayDocumentSystem;

/// <summary>
/// Recording a payment against an official receipt number, and printing the
/// receipt.
///
/// I refuse to accept the payment without the number, because money must
/// never be recorded with nothing to trace it back to. The "Record & print
/// receipt" path previews the barangay's own receipt - a half-page slip with
/// the reference, the amount and the legal basis - so the resident leaves
/// with paper that matches what the books say.
/// </summary>
public partial class PaymentForm : DialogBase
{
    /// <summary>The receipt number entered, when the user confirmed.</summary>
    public string ReceiptNumber { get; private set; } = string.Empty;

    private readonly DocumentRequest? _request;

    private readonly TextBox _receipt = new();
    private readonly Label _amount = new();
    private readonly Label _basis = new();

    public PaymentForm()
    {
        InitializeComponent();
        BuildUi();
    }

    public PaymentForm(DocumentRequest request) : this()
    {
        _request = request;

        Text = $"Record payment — {request.GetReferenceNumber()}";

        _amount.Text = DisplayFormat.Peso(request.Fee);
        _basis.Text = request.FeeBasis;
    }

    private void BuildUi()
    {
        _amount.Font = Display;
        _amount.ForeColor = Ink;
        _amount.Dock = DockStyle.Top;
        _amount.Height = 48;
        _amount.BackColor = Color.Transparent;

        _basis.Font = Small;
        _basis.ForeColor = Muted;
        _basis.Dock = DockStyle.Top;
        _basis.Height = 34;
        _basis.BackColor = Color.Transparent;

        _receipt.Font = Body;
        _receipt.BackColor = Surface;
        _receipt.ForeColor = Ink;
        _receipt.Dock = DockStyle.Top;

        var grid = UiFactory.Grid(1);
        grid.Controls.Add(UiFactory.Field("Official receipt number *", _receipt));
        grid.Controls.Add(_basis);
        grid.Controls.Add(_amount);
        grid.Dock = DockStyle.Top;

        paymentScroll.Controls.Add(grid);
    }

    private bool ReadAndValidate()
    {
        var check = InputValidator.ReceiptNumber(_receipt.Text);
        if (!check.Ok)
        {
            Dialog.FieldProblem(this, "Payment", check.Error);
            return false;
        }

        ReceiptNumber = _receipt.Text.Trim();
        return true;
    }

    private void Record(object? sender, EventArgs e)
    {
        if (!ReadAndValidate()) return;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void RecordAndPrint(object? sender, EventArgs e)
    {
        if (!ReadAndValidate()) return;
        DialogResult = DialogResult.OK;

        if (_request is not null)
        {
            try
            {
                using var document = ReceiptRenderer.CreatePrintDocument(_request, ReceiptNumber);
                using var preview = new PrintPreviewDialog
                {
                    Document = document,
                    Width = 640,
                    Height = 760,
                    StartPosition = FormStartPosition.CenterParent
                };
                preview.ShowDialog(this);
            }
            catch (SystemException ex)
            {
                // No printer driver, no print subsystem - the payment is
                // already recorded, so the receipt failing to preview must
                // not take the dialog down with it.
                Dialog.Warn(this,
                    "The payment was recorded, but the receipt could not be " +
                    $"previewed on this machine.\n\n{ex.Message}",
                    "Receipt");
            }
        }

        Close();
    }

    private void Cancel(object? sender, EventArgs e) => Close();
}

/// <summary>
/// The official receipt, drawn with GDI+ for the half-page slip the barangay
/// hands across the counter. The amount, the legal basis and the receipt
/// number are printed together, because one without the other is money I
/// cannot account for.
/// </summary>
public static class ReceiptRenderer
{
    public static PrintDocument CreatePrintDocument(DocumentRequest request, string receiptNumber)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = BarangayProfile.Current;
        var document = new PrintDocument
        {
            DocumentName = $"Receipt {receiptNumber}"
        };

        document.DefaultPageSettings.Margins = new Margins(50, 40, 40, 40);
        document.PrintPage += (_, e) =>
        {
            var g = e.Graphics;
            var b = e.MarginBounds;

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var head = new Font("Consolas", 11f, FontStyle.Bold, GraphicsUnit.Point);
            using var body = new Font("Consolas", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            using var big = new Font("Consolas", 14f, FontStyle.Bold, GraphicsUnit.Point);

            float y = b.Top;

            y = Line(g, $"{profile.BarangayName.ToUpperInvariant(),-40}", head, b.Left, y);
            y = Line(g, $"OFFICIAL RECEIPT", head, b.Left, y);
            y = Line(g, new string('-', 44), body, b.Left, y);
            y = Line(g, $"O.R. No.   : {receiptNumber}", body, b.Left, y);
            y = Line(g, $"Date       : {DisplayFormat.Stamp(DateTime.Now)}", body, b.Left, y);
            y = Line(g, new string('-', 44), body, b.Left, y);
            y = Line(g, $"RECEIVED from : {request.Resident.GetFullName()}", body, b.Left, y);
            y = Line(g, $"The sum of    : {DisplayFormat.Peso(request.Fee)}", body, b.Left, y);
            y = Line(g, $"Being payment for {request.GetDocumentName()}", body, b.Left, y);
            y = Line(g, $"Reference     : {request.GetReferenceNumber()}", body, b.Left, y);

            string detail = request.Input.Detail.Trim();
            if (detail.Length > 0)
                y = Line(g, $"Particulars   : {detail}", body, b.Left, y);

            y += 6f;
            y = Line(g, $"AMOUNT DUE    : {DisplayFormat.Peso(request.Fee)}", big, b.Left, y);
            y = Line(g, $"Basis         : {request.FeeBasis}", body, b.Left, y);
            y += 14f;
            y = Line(g, $"_________________________", body, b.Left + 180, y);
            y = Line(g, "Barangay Treasurer", body, b.Left + 180, y);

            e.HasMorePages = false;
        };

        return document;
    }

    private static float Line(Graphics g, string text, Font font, float x, float y)
    {
        g.DrawString(text, font, Brushes.Black, new PointF(x, y));
        return y + font.GetHeight(g);
    }
}
