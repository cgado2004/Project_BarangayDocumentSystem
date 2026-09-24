using System.Windows.Forms;
using System.Drawing;
using System;
namespace BarangayDocumentSystem.Forms;

partial class DocumentPreviewForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        this.txtDocument = new System.Windows.Forms.TextBox();
        this.pnlButtons = new System.Windows.Forms.Panel();
        this.btnCopy = new System.Windows.Forms.Button();
        this.btnSave = new System.Windows.Forms.Button();
        this.btnPrint = new System.Windows.Forms.Button();
        this.btnClose = new System.Windows.Forms.Button();
        this.pnlButtons.SuspendLayout();
        this.SuspendLayout();

        // Consolas is monospaced so the certificate layout holds together.
        this.txtDocument.BackColor = System.Drawing.Color.White;
        this.txtDocument.Dock = System.Windows.Forms.DockStyle.Fill;
        this.txtDocument.Font = new System.Drawing.Font("Consolas", 10F);
        this.txtDocument.Location = new System.Drawing.Point(0, 0);
        this.txtDocument.Multiline = true;
        this.txtDocument.Name = "txtDocument";
        this.txtDocument.ReadOnly = true;
        this.txtDocument.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtDocument.Size = new System.Drawing.Size(700, 560);
        this.txtDocument.TabIndex = 0;

        this.pnlButtons.Controls.Add(this.btnClose);
        this.pnlButtons.Controls.Add(this.btnPrint);
        this.pnlButtons.Controls.Add(this.btnSave);
        this.pnlButtons.Controls.Add(this.btnCopy);
        this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.pnlButtons.Location = new System.Drawing.Point(0, 560);
        this.pnlButtons.Name = "pnlButtons";
        this.pnlButtons.Padding = new System.Windows.Forms.Padding(10);
        this.pnlButtons.Size = new System.Drawing.Size(700, 60);
        this.pnlButtons.TabIndex = 1;

        this.btnCopy.Location = new System.Drawing.Point(13, 12);
        this.btnCopy.Name = "btnCopy";
        this.btnCopy.Size = new System.Drawing.Size(120, 36);
        this.btnCopy.TabIndex = 0;
        this.btnCopy.Text = "Copy";
        this.btnCopy.UseVisualStyleBackColor = true;
        this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);

        this.btnSave.Location = new System.Drawing.Point(139, 12);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(120, 36);
        this.btnSave.TabIndex = 1;
        this.btnSave.Text = "Save as .txt";
        this.btnSave.UseVisualStyleBackColor = true;
        this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

        this.btnPrint.Location = new System.Drawing.Point(265, 12);
        this.btnPrint.Name = "btnPrint";
        this.btnPrint.Size = new System.Drawing.Size(120, 36);
        this.btnPrint.TabIndex = 2;
        this.btnPrint.Text = "Print Preview";
        this.btnPrint.UseVisualStyleBackColor = true;
        this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);

        this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnClose.Location = new System.Drawing.Point(567, 12);
        this.btnClose.Name = "btnClose";
        this.btnClose.Size = new System.Drawing.Size(120, 36);
        this.btnClose.TabIndex = 3;
        this.btnClose.Text = "Close";
        this.btnClose.UseVisualStyleBackColor = true;
        this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

        this.CancelButton = this.btnClose;
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(700, 620);
        this.Controls.Add(this.txtDocument);
        this.Controls.Add(this.pnlButtons);
        this.MinimumSize = new System.Drawing.Size(600, 500);
        this.Name = "DocumentPreviewForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Document Preview";
        this.Load += new System.EventHandler(this.DocumentPreviewForm_Load);
        this.pnlButtons.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.TextBox txtDocument;
    private System.Windows.Forms.Panel pnlButtons;
    private System.Windows.Forms.Button btnCopy;
    private System.Windows.Forms.Button btnSave;
    private System.Windows.Forms.Button btnPrint;
    private System.Windows.Forms.Button btnClose;
}
