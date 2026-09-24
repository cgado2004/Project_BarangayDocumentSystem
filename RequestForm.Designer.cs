namespace BarangayDocumentSystem;

/// <summary>
/// The designer half of the new-request dialog. The chrome lives here; the
/// fields, the variable-fee area and the live assessment are built in
/// BuildUi() in the other half.
/// </summary>
partial class RequestForm
{
    private System.ComponentModel.IContainer components = null;

    private BarangayDocumentSystem.Helper.SmoothPanel requestScroll;
    private System.Windows.Forms.FlowLayoutPanel buttonBar;
    private BarangayDocumentSystem.Helper.PillButton btnConfirm;
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
        this.requestScroll = new BarangayDocumentSystem.Helper.SmoothPanel();
        this.buttonBar = new System.Windows.Forms.FlowLayoutPanel();
        this.btnConfirm = new BarangayDocumentSystem.Helper.PillButton();
        this.btnCancel = new BarangayDocumentSystem.Helper.PillButton();

        //
        // requestScroll - the scrolling body of the dialog
        //
        this.requestScroll.Name = "requestScroll";
        this.requestScroll.Dock = System.Windows.Forms.DockStyle.Fill;
        this.requestScroll.AutoScroll = true;
        this.requestScroll.BackColor = System.Drawing.Color.Transparent;

        //
        // btnConfirm
        //
        this.btnConfirm.Name = "btnConfirm";
        this.btnConfirm.Text = "File request";
        this.btnConfirm.Width = 130;
        this.btnConfirm.Click += new System.EventHandler(this.Confirm);

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
        this.buttonBar.Controls.Add(this.btnConfirm);
        this.buttonBar.Controls.Add(this.btnCancel);

        //
        // RequestForm
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.Text = "New request";
        this.ClientSize = new System.Drawing.Size(660, 640);
        this.MinimumSize = new System.Drawing.Size(600, 540);
        this.CancelButton = this.btnCancel;

        this.Controls.Add(this.requestScroll);
        this.Controls.Add(this.buttonBar);

        this.SuspendLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
