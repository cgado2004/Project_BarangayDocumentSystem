namespace BarangayDocumentSystem;

/// <summary>
/// The designer half of the payment dialog. The chrome lives here; the
/// amount, the basis and the receipt field are built in BuildUi() in the
/// other half.
/// </summary>
partial class PaymentForm
{
    private System.ComponentModel.IContainer components = null;

    private BarangayDocumentSystem.Helper.SmoothPanel paymentScroll;
    private System.Windows.Forms.FlowLayoutPanel buttonBar;
    private BarangayDocumentSystem.Helper.PillButton btnRecord;
    private BarangayDocumentSystem.Helper.PillButton btnRecordPrint;
    private BarangayDocumentSystem.Helper.PillButton btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.paymentScroll = new BarangayDocumentSystem.Helper.SmoothPanel();
        this.buttonBar = new System.Windows.Forms.FlowLayoutPanel();
        this.btnRecord = new BarangayDocumentSystem.Helper.PillButton();
        this.btnRecordPrint = new BarangayDocumentSystem.Helper.PillButton();
        this.btnCancel = new BarangayDocumentSystem.Helper.PillButton();

        //
        // paymentScroll - the scrolling body of the dialog
        //
        this.paymentScroll.Name = "paymentScroll";
        this.paymentScroll.Dock = System.Windows.Forms.DockStyle.Fill;
        this.paymentScroll.AutoScroll = true;
        this.paymentScroll.BackColor = System.Drawing.Color.Transparent;

        //
        // btnRecord
        //
        this.btnRecord.Name = "btnRecord";
        this.btnRecord.Text = "Record";
        this.btnRecord.Width = 120;
        this.btnRecord.Click += new System.EventHandler(this.Record);

        //
        // btnRecordPrint - the same thing, plus the receipt preview
        //
        this.btnRecordPrint.Name = "btnRecordPrint";
        this.btnRecordPrint.Text = "Record & print receipt";
        this.btnRecordPrint.Width = 190;
        this.btnRecordPrint.Click += new System.EventHandler(this.RecordAndPrint);

        //
        // btnCancel
        //
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Text = "Cancel";
        this.btnCancel.Width = 110;
        this.btnCancel.Look = BarangayDocumentSystem.Helper.PillButton.Style.Outline;
        this.btnCancel.Click += new System.EventHandler(this.Cancel);

        //
        // buttonBar
        //
        this.buttonBar.Name = "buttonBar";
        this.buttonBar.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.buttonBar.Height = 60;
        this.buttonBar.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        this.buttonBar.WrapContents = false;
        this.buttonBar.BackColor = System.Drawing.Color.Transparent;
        this.buttonBar.Padding = new System.Windows.Forms.Padding(0, 12, 0, 0);
        this.buttonBar.Controls.Add(this.btnRecordPrint);
        this.buttonBar.Controls.Add(this.btnRecord);
        this.buttonBar.Controls.Add(this.btnCancel);

        //
        // PaymentForm
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.Text = "Record payment";
        this.ClientSize = new System.Drawing.Size(520, 360);
        this.MinimumSize = new System.Drawing.Size(470, 320);
        this.CancelButton = this.btnCancel;

        this.Controls.Add(this.paymentScroll);
        this.Controls.Add(this.buttonBar);

        this.SuspendLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
