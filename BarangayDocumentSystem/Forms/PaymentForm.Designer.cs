using System.Windows.Forms;
using System.Drawing;
using System;
using BarangayDocumentSystem.UIHelpers;
namespace BarangayDocumentSystem.Forms;

partial class PaymentForm
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
        this.lblRef = new System.Windows.Forms.Label();
        this.lblRefValue = new System.Windows.Forms.Label();
        this.lblDocument = new System.Windows.Forms.Label();
        this.lblDocumentValue = new System.Windows.Forms.Label();
        this.lblResident = new System.Windows.Forms.Label();
        this.lblResidentValue = new System.Windows.Forms.Label();
        this.lblAmount = new System.Windows.Forms.Label();
        this.lblAmountValue = new System.Windows.Forms.Label();
        this.lblOr = new System.Windows.Forms.Label();
        this.txtOr = new System.Windows.Forms.TextBox();
        this.lblNote = new System.Windows.Forms.Label();
        this.btnConfirm = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();
        this.SuspendLayout();

        var labelFont = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular);
        var valueFont = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular);
        var boldFont = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        var inputFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular);
        var buttonFont = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
        var amountFont = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);

        // ── read-only info ──────────────────────────────────────────
        this.lblRef.AutoSize = true;
        this.lblRef.Font = labelFont;
        this.lblRef.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
        this.lblRef.Location = new System.Drawing.Point(32, 28);
        this.lblRef.Name = "lblRef";
        this.lblRef.Text = "Reference";

        this.lblRefValue.AutoSize = true;
        this.lblRefValue.Font = boldFont;
        this.lblRefValue.Location = new System.Drawing.Point(200, 28);
        this.lblRefValue.Name = "lblRefValue";
        this.lblRefValue.Text = "—";

        this.lblDocument.AutoSize = true;
        this.lblDocument.Font = labelFont;
        this.lblDocument.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
        this.lblDocument.Location = new System.Drawing.Point(32, 60);
        this.lblDocument.Name = "lblDocument";
        this.lblDocument.Text = "Document";

        this.lblDocumentValue.AutoSize = true;
        this.lblDocumentValue.Font = valueFont;
        this.lblDocumentValue.Location = new System.Drawing.Point(200, 60);
        this.lblDocumentValue.Name = "lblDocumentValue";
        this.lblDocumentValue.Text = "—";

        this.lblResident.AutoSize = true;
        this.lblResident.Font = labelFont;
        this.lblResident.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
        this.lblResident.Location = new System.Drawing.Point(32, 92);
        this.lblResident.Name = "lblResident";
        this.lblResident.Text = "Resident";

        this.lblResidentValue.AutoSize = true;
        this.lblResidentValue.Font = valueFont;
        this.lblResidentValue.Location = new System.Drawing.Point(200, 92);
        this.lblResidentValue.Name = "lblResidentValue";
        this.lblResidentValue.Text = "—";

        // ── amount ──────────────────────────────────────────────────
        this.lblAmount.AutoSize = true;
        this.lblAmount.Font = boldFont;
        this.lblAmount.Location = new System.Drawing.Point(32, 138);
        this.lblAmount.Name = "lblAmount";
        this.lblAmount.Text = "Amount due";

        this.lblAmountValue.AutoSize = true;
        this.lblAmountValue.Font = amountFont;
        this.lblAmountValue.ForeColor = System.Drawing.Color.FromArgb(21, 128, 61);
        this.lblAmountValue.Location = new System.Drawing.Point(196, 126);
        this.lblAmountValue.Name = "lblAmountValue";
        this.lblAmountValue.Text = "—";

        // ── O.R. input ──────────────────────────────────────────────
        this.lblOr.AutoSize = true;
        this.lblOr.Font = boldFont;
        this.lblOr.Location = new System.Drawing.Point(32, 200);
        this.lblOr.Name = "lblOr";
        this.lblOr.Text = "O.R. number";

        this.txtOr.Font = inputFont;
        this.txtOr.Location = new System.Drawing.Point(200, 196);
        this.txtOr.MaxLength = 30;
        this.txtOr.Name = "txtOr";
        CueBanner.Set(this.txtOr, "OR-2026-00001");
        this.txtOr.Size = new System.Drawing.Size(330, 32);
        this.txtOr.TabIndex = 1;

        this.lblNote.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Italic);
        this.lblNote.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
        this.lblNote.Location = new System.Drawing.Point(32, 240);
        this.lblNote.Name = "lblNote";
        this.lblNote.Size = new System.Drawing.Size(500, 60);
        this.lblNote.Text = "Type the O.R. number from the receipt you issued to the resident. " +
                            "This marks the request as paid, so the document can be released.";

        // ── buttons ─────────────────────────────────────────────────
        this.btnConfirm.BackColor = System.Drawing.Color.FromArgb(21, 128, 61);
        this.btnConfirm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnConfirm.Font = buttonFont;
        this.btnConfirm.ForeColor = System.Drawing.Color.White;
        this.btnConfirm.Location = new System.Drawing.Point(240, 320);
        this.btnConfirm.Name = "btnConfirm";
        this.btnConfirm.Size = new System.Drawing.Size(170, 46);
        this.btnConfirm.TabIndex = 2;
        this.btnConfirm.Text = "Mark as Paid";
        this.btnConfirm.UseVisualStyleBackColor = false;
        this.btnConfirm.FlatAppearance.BorderSize = 0;
        this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);

        this.btnCancel.BackColor = System.Drawing.Color.White;
        this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnCancel.Font = buttonFont;
        this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(31, 39, 51);
        this.btnCancel.Location = new System.Drawing.Point(420, 320);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(110, 46);
        this.btnCancel.TabIndex = 3;
        this.btnCancel.Text = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = false;
        this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 191, 168);
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

        // ── form ────────────────────────────────────────────────────
        this.AcceptButton = this.btnConfirm;
        this.CancelButton = this.btnCancel;
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.Color.FromArgb(243, 239, 231);
        this.ClientSize = new System.Drawing.Size(564, 392);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnConfirm);
        this.Controls.Add(this.lblNote);
        this.Controls.Add(this.txtOr);
        this.Controls.Add(this.lblOr);
        this.Controls.Add(this.lblAmountValue);
        this.Controls.Add(this.lblAmount);
        this.Controls.Add(this.lblResidentValue);
        this.Controls.Add(this.lblResident);
        this.Controls.Add(this.lblDocumentValue);
        this.Controls.Add(this.lblDocument);
        this.Controls.Add(this.lblRefValue);
        this.Controls.Add(this.lblRef);
        this.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Regular);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "PaymentForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Record Payment";
        this.Load += new System.EventHandler(this.PaymentForm_Load);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label lblRef;
    private System.Windows.Forms.Label lblRefValue;
    private System.Windows.Forms.Label lblDocument;
    private System.Windows.Forms.Label lblDocumentValue;
    private System.Windows.Forms.Label lblResident;
    private System.Windows.Forms.Label lblResidentValue;
    private System.Windows.Forms.Label lblAmount;
    private System.Windows.Forms.Label lblAmountValue;
    private System.Windows.Forms.Label lblOr;
    private System.Windows.Forms.TextBox txtOr;
    private System.Windows.Forms.Label lblNote;
    private System.Windows.Forms.Button btnConfirm;
    private System.Windows.Forms.Button btnCancel;
}