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

        this.lblRef.AutoSize = true;
        this.lblRef.Location = new System.Drawing.Point(24, 24);
        this.lblRef.Name = "lblRef";
        this.lblRef.Size = new System.Drawing.Size(83, 20);
        this.lblRef.TabIndex = 0;
        this.lblRef.Text = "Reference:";

        this.lblRefValue.AutoSize = true;
        this.lblRefValue.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
        this.lblRefValue.Location = new System.Drawing.Point(170, 24);
        this.lblRefValue.Name = "lblRefValue";
        this.lblRefValue.Size = new System.Drawing.Size(20, 20);
        this.lblRefValue.TabIndex = 1;
        this.lblRefValue.Text = "-";

        this.lblDocument.AutoSize = true;
        this.lblDocument.Location = new System.Drawing.Point(24, 56);
        this.lblDocument.Name = "lblDocument";
        this.lblDocument.Size = new System.Drawing.Size(83, 20);
        this.lblDocument.TabIndex = 2;
        this.lblDocument.Text = "Document:";

        this.lblDocumentValue.AutoSize = true;
        this.lblDocumentValue.Location = new System.Drawing.Point(170, 56);
        this.lblDocumentValue.Name = "lblDocumentValue";
        this.lblDocumentValue.Size = new System.Drawing.Size(20, 20);
        this.lblDocumentValue.TabIndex = 3;
        this.lblDocumentValue.Text = "-";

        this.lblResident.AutoSize = true;
        this.lblResident.Location = new System.Drawing.Point(24, 88);
        this.lblResident.Name = "lblResident";
        this.lblResident.Size = new System.Drawing.Size(69, 20);
        this.lblResident.TabIndex = 4;
        this.lblResident.Text = "Resident:";

        this.lblResidentValue.AutoSize = true;
        this.lblResidentValue.Location = new System.Drawing.Point(170, 88);
        this.lblResidentValue.Name = "lblResidentValue";
        this.lblResidentValue.Size = new System.Drawing.Size(20, 20);
        this.lblResidentValue.TabIndex = 5;
        this.lblResidentValue.Text = "-";

        this.lblAmount.AutoSize = true;
        this.lblAmount.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold);
        this.lblAmount.Location = new System.Drawing.Point(24, 126);
        this.lblAmount.Name = "lblAmount";
        this.lblAmount.Size = new System.Drawing.Size(126, 25);
        this.lblAmount.TabIndex = 6;
        this.lblAmount.Text = "Amount due:";

        this.lblAmountValue.AutoSize = true;
        this.lblAmountValue.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
        this.lblAmountValue.ForeColor = System.Drawing.Color.FromArgb(21, 71, 52);
        this.lblAmountValue.Location = new System.Drawing.Point(170, 122);
        this.lblAmountValue.Name = "lblAmountValue";
        this.lblAmountValue.Size = new System.Drawing.Size(30, 31);
        this.lblAmountValue.TabIndex = 7;
        this.lblAmountValue.Text = "-";

        this.lblOr.AutoSize = true;
        this.lblOr.Location = new System.Drawing.Point(24, 172);
        this.lblOr.Name = "lblOr";
        this.lblOr.Size = new System.Drawing.Size(130, 20);
        this.lblOr.TabIndex = 8;
        this.lblOr.Text = "O.R. number:";

        this.txtOr.Location = new System.Drawing.Point(170, 169);
        this.txtOr.MaxLength = 30;
        this.txtOr.Name = "txtOr";
        CueBanner.Set(this.txtOr, "OR-2026-00001");
        this.txtOr.Size = new System.Drawing.Size(260, 27);
        this.txtOr.TabIndex = 9;

        this.lblNote.ForeColor = System.Drawing.Color.FromArgb(90, 107, 130);
        this.lblNote.Location = new System.Drawing.Point(24, 206);
        this.lblNote.Name = "lblNote";
        this.lblNote.Size = new System.Drawing.Size(410, 42);
        this.lblNote.TabIndex = 10;
        this.lblNote.Text = "RA 11032 requires an official receipt for every collection. The amount on the OR must match the posted fee.";

        this.btnConfirm.BackColor = System.Drawing.Color.FromArgb(21, 71, 52);
        this.btnConfirm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnConfirm.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
        this.btnConfirm.ForeColor = System.Drawing.Color.White;
        this.btnConfirm.Location = new System.Drawing.Point(190, 258);
        this.btnConfirm.Name = "btnConfirm";
        this.btnConfirm.Size = new System.Drawing.Size(130, 40);
        this.btnConfirm.TabIndex = 11;
        this.btnConfirm.Text = "Confirm";
        this.btnConfirm.UseVisualStyleBackColor = false;
        this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);

        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(330, 258);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(104, 40);
        this.btnCancel.TabIndex = 12;
        this.btnCancel.Text = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = true;
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

        this.AcceptButton = this.btnConfirm;
        this.CancelButton = this.btnCancel;
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(464, 318);
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
