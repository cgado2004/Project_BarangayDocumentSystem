namespace BarangayDocumentSystem;

/// <summary>
/// The designer half of the rejection dialog. The chrome lives here; the
/// theme fonts and colours are applied in BuildUi() in the other half.
/// </summary>
partial class RejectionForm
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label lblSummary;
    private System.Windows.Forms.Label lblReasonCaption;
    private System.Windows.Forms.TextBox txtReason;
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
        this.lblSummary = new System.Windows.Forms.Label();
        this.lblReasonCaption = new System.Windows.Forms.Label();
        this.txtReason = new System.Windows.Forms.TextBox();
        this.buttonBar = new System.Windows.Forms.FlowLayoutPanel();
        this.btnConfirm = new BarangayDocumentSystem.Helper.PillButton();
        this.btnCancel = new BarangayDocumentSystem.Helper.PillButton();
        this.SuspendLayout();

        //
        // lblSummary - what is about to be rejected, and the money warning
        //
        this.lblSummary.Name = "lblSummary";
        this.lblSummary.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblSummary.AutoSize = true;
        this.lblSummary.Padding = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.lblSummary.MaximumSize = new System.Drawing.Size(620, 0);

        //
        // lblReasonCaption
        //
        this.lblReasonCaption.Name = "lblReasonCaption";
        this.lblReasonCaption.Text = "Reason for rejection *  — the resident is entitled to be told.";
        this.lblReasonCaption.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblReasonCaption.AutoSize = true;
        this.lblReasonCaption.Padding = new System.Windows.Forms.Padding(0, 4, 0, 6);

        //
        // txtReason
        //
        this.txtReason.Name = "txtReason";
        this.txtReason.Dock = System.Windows.Forms.DockStyle.Top;
        this.txtReason.Multiline = true;
        this.txtReason.Height = 110;
        this.txtReason.MaxLength = 300;

        //
        // btnConfirm
        //
        this.btnConfirm.Name = "btnConfirm";
        this.btnConfirm.Text = "Reject request";
        this.btnConfirm.Width = 150;
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
        // RejectionForm
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.Text = "Reject request";
        this.ClientSize = new System.Drawing.Size(520, 340);
        this.MinimumSize = new System.Drawing.Size(470, 300);
        this.CancelButton = this.btnCancel;

        this.Controls.Add(this.txtReason);
        this.Controls.Add(this.lblReasonCaption);
        this.Controls.Add(this.lblSummary);
        this.Controls.Add(this.buttonBar);

        this.SuspendLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
