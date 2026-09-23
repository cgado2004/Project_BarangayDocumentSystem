namespace BarangayDocumentSystem;

/// <summary>
/// The designer half of the print preview. The chrome lives here; the
/// preview control and the zoom bar are built in BuildUi() in the other
/// half.
/// </summary>
partial class DocumentPreviewForm
{
    private System.ComponentModel.IContainer components = null;

    private BarangayDocumentSystem.Helper.SmoothPanel previewHost;

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
        this.previewHost = new BarangayDocumentSystem.Helper.SmoothPanel();

        //
        // previewHost - fills the form above the zoom bar
        //
        this.previewHost.Name = "previewHost";
        this.previewHost.Dock = System.Windows.Forms.DockStyle.Fill;
        this.previewHost.BackColor = System.Drawing.Color.Transparent;

        //
        // DocumentPreviewForm
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.Text = "Preview";
        this.ClientSize = new System.Drawing.Size(760, 720);
        this.MinimumSize = new System.Drawing.Size(560, 480);

        this.SuspendLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
