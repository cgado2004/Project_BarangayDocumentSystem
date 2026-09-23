namespace BarangayDocumentSystem.Forms
{
    partial class DocumentPreviewForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtDocument;
        private System.Windows.Forms.Label lblState;
        private System.Windows.Forms.FlowLayoutPanel pnlActions;
        private System.Windows.Forms.Button btnPrintPreview;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtDocument = new System.Windows.Forms.TextBox();
            this.lblState = new System.Windows.Forms.Label();
            this.pnlActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnPrintPreview = new System.Windows.Forms.Button();
            this.btnPrint = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.pnlActions.SuspendLayout();
            this.SuspendLayout();

            // txtDocument
            this.txtDocument.Name = "txtDocument";
            this.txtDocument.TabIndex = 0;
            this.txtDocument.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDocument.Multiline = true;
            this.txtDocument.ReadOnly = true;
            this.txtDocument.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDocument.BackColor = System.Drawing.Color.White;
            this.txtDocument.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtDocument.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDocument.TabStop = false;

            // lblState
            this.lblState.Name = "lblState";
            this.lblState.TabIndex = 1;
            this.lblState.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblState.Height = 40;
            this.lblState.Padding = new System.Windows.Forms.Padding(12, 8, 0, 0);

            // pnlActions
            this.pnlActions.Name = "pnlActions";
            this.pnlActions.TabIndex = 2;
            this.pnlActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlActions.AutoSize = true;
            this.pnlActions.Padding = new System.Windows.Forms.Padding(12);
            this.pnlActions.WrapContents = true;

            // btnPrintPreview
            this.btnPrintPreview.Name = "btnPrintPreview";
            this.btnPrintPreview.TabIndex = 3;
            this.btnPrintPreview.Text = "Print preview";
            this.btnPrintPreview.AutoSize = true;
            this.btnPrintPreview.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPrintPreview.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnPrintPreview.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnPrintPreview.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnPrintPreview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrintPreview.BackColor = System.Drawing.Color.White;
            this.btnPrintPreview.ForeColor = System.Drawing.Color.FromArgb(35, 45, 42);
            this.btnPrintPreview.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrintPreview.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnPrintPreview);

            // btnPrint
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.TabIndex = 4;
            this.btnPrint.Text = "Print...";
            this.btnPrint.AutoSize = true;
            this.btnPrint.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPrint.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnPrint.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnPrint.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrint.BackColor = System.Drawing.Color.FromArgb(31, 91, 76);
            this.btnPrint.ForeColor = System.Drawing.Color.White;
            this.btnPrint.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrint.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnPrint);

            // btnClose
            this.btnClose.Name = "btnClose";
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "Close";
            this.btnClose.AutoSize = true;
            this.btnClose.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnClose.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnClose.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnClose.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.BackColor = System.Drawing.Color.White;
            this.btnClose.ForeColor = System.Drawing.Color.FromArgb(35, 45, 42);
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.pnlActions.Controls.Add(this.btnClose);
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelButton = this.btnClose;
            this.Controls.Add(this.txtDocument);
            this.Controls.Add(this.lblState);
            this.Controls.Add(this.pnlActions);

            // DocumentPreviewForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 720);
            this.MinimumSize = new System.Drawing.Size(900, 400);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.Name = "DocumentPreviewForm";
            this.Text = "Document preview";
            this.btnPrintPreview.Click += new System.EventHandler(this.PrintPreview);
            this.btnPrint.Click += new System.EventHandler(this.PrintDocument);
            this.btnClose.Click += new System.EventHandler(this.CloseDialog);
            this.pnlActions.ResumeLayout(false);
            this.pnlActions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
