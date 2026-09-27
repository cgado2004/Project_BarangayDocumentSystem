using System.Windows.Forms;
using System.Drawing;
using System;
namespace BarangayDocumentSystem.Forms;

partial class RequestForm
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
        this.grpResident = new System.Windows.Forms.GroupBox();
        this.lblResident = new System.Windows.Forms.Label();
        this.lblResidentValue = new System.Windows.Forms.Label();
        this.lblClass = new System.Windows.Forms.Label();
        this.lblClassValue = new System.Windows.Forms.Label();
        this.lblResidency = new System.Windows.Forms.Label();
        this.lblResidencyValue = new System.Windows.Forms.Label();

        this.lblDocument = new System.Windows.Forms.Label();
        this.cmbDocument = new System.Windows.Forms.ComboBox();
        this.lblPurpose = new System.Windows.Forms.Label();
        this.cmbPurpose = new System.Windows.Forms.ComboBox();

        this.grpFee = new System.Windows.Forms.GroupBox();
        this.lblFee = new System.Windows.Forms.Label();
        this.lblFeeValue = new System.Windows.Forms.Label();
        this.lblBasis = new System.Windows.Forms.Label();
        this.lblBasisValue = new System.Windows.Forms.Label();

        this.lblWarning = new System.Windows.Forms.Label();
        this.btnSubmit = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();

        this.grpResident.SuspendLayout();
        this.grpFee.SuspendLayout();
        this.SuspendLayout();

        var labelFont = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular);
        var inputFont = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular);
        var buttonFont = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
        var boldFont = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        var feeBigFont = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);

        // ── grpResident ─────────────────────────────────────────────
        this.grpResident.Controls.Add(this.lblResidencyValue);
        this.grpResident.Controls.Add(this.lblResidency);
        this.grpResident.Controls.Add(this.lblClassValue);
        this.grpResident.Controls.Add(this.lblClass);
        this.grpResident.Controls.Add(this.lblResidentValue);
        this.grpResident.Controls.Add(this.lblResident);
        this.grpResident.Font = labelFont;
        this.grpResident.Location = new System.Drawing.Point(36, 20);
        this.grpResident.Name = "grpResident";
        this.grpResident.Size = new System.Drawing.Size(748, 128);
        this.grpResident.TabIndex = 0;
        this.grpResident.TabStop = false;
        this.grpResident.Text = "Requesting resident";

        this.lblResident.AutoSize = true;
        this.lblResident.Font = labelFont;
        this.lblResident.Location = new System.Drawing.Point(22, 34);
        this.lblResident.Name = "lblResident";
        this.lblResident.Text = "Name";

        this.lblResidentValue.AutoSize = true;
        this.lblResidentValue.Font = boldFont;
        this.lblResidentValue.Location = new System.Drawing.Point(170, 34);
        this.lblResidentValue.Name = "lblResidentValue";
        this.lblResidentValue.Text = "—";

        this.lblClass.AutoSize = true;
        this.lblClass.Font = labelFont;
        this.lblClass.Location = new System.Drawing.Point(22, 62);
        this.lblClass.Name = "lblClass";
        this.lblClass.Text = "Classification";

        this.lblClassValue.AutoSize = true;
        this.lblClassValue.Font = inputFont;
        this.lblClassValue.Location = new System.Drawing.Point(170, 62);
        this.lblClassValue.Name = "lblClassValue";
        this.lblClassValue.Text = "—";

        this.lblResidency.AutoSize = true;
        this.lblResidency.Font = labelFont;
        this.lblResidency.Location = new System.Drawing.Point(22, 90);
        this.lblResidency.Name = "lblResidency";
        this.lblResidency.Text = "Residency";

        this.lblResidencyValue.AutoSize = true;
        this.lblResidencyValue.Font = inputFont;
        this.lblResidencyValue.Location = new System.Drawing.Point(170, 90);
        this.lblResidencyValue.Name = "lblResidencyValue";
        this.lblResidencyValue.Text = "—";

        // ── document ────────────────────────────────────────────────
        this.lblDocument.AutoSize = true;
        this.lblDocument.Font = labelFont;
        this.lblDocument.Location = new System.Drawing.Point(36, 172);
        this.lblDocument.Name = "lblDocument";
        this.lblDocument.Text = "Document type";

        this.cmbDocument.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbDocument.Font = inputFont;
        this.cmbDocument.Location = new System.Drawing.Point(210, 168);
        this.cmbDocument.Name = "cmbDocument";
        this.cmbDocument.Size = new System.Drawing.Size(574, 30);
        this.cmbDocument.TabIndex = 2;
        this.cmbDocument.SelectedIndexChanged += new System.EventHandler(this.cmbDocument_SelectedIndexChanged);

        this.lblPurpose.AutoSize = true;
        this.lblPurpose.Font = labelFont;
        this.lblPurpose.Location = new System.Drawing.Point(36, 216);
        this.lblPurpose.Name = "lblPurpose";
        this.lblPurpose.Text = "Purpose";

        this.cmbPurpose.Font = inputFont;
        this.cmbPurpose.Location = new System.Drawing.Point(210, 212);
        this.cmbPurpose.MaxLength = 120;
        this.cmbPurpose.Name = "cmbPurpose";
        this.cmbPurpose.Size = new System.Drawing.Size(574, 30);
        this.cmbPurpose.TabIndex = 4;

        // ── grpFee ──────────────────────────────────────────────────
        this.grpFee.Controls.Add(this.lblBasisValue);
        this.grpFee.Controls.Add(this.lblBasis);
        this.grpFee.Controls.Add(this.lblFeeValue);
        this.grpFee.Controls.Add(this.lblFee);
        this.grpFee.Font = labelFont;
        this.grpFee.Location = new System.Drawing.Point(36, 260);
        this.grpFee.Name = "grpFee";
        this.grpFee.Size = new System.Drawing.Size(748, 124);
        this.grpFee.TabIndex = 5;
        this.grpFee.TabStop = false;
        this.grpFee.Text = "Fee assessment";

        this.lblFee.AutoSize = true;
        this.lblFee.Font = labelFont;
        this.lblFee.Location = new System.Drawing.Point(22, 40);
        this.lblFee.Name = "lblFee";
        this.lblFee.Text = "Fee";

        this.lblFeeValue.AutoSize = true;
        this.lblFeeValue.Font = feeBigFont;
        this.lblFeeValue.Location = new System.Drawing.Point(164, 30);
        this.lblFeeValue.Name = "lblFeeValue";
        this.lblFeeValue.Text = "—";

        this.lblBasis.AutoSize = true;
        this.lblBasis.Font = labelFont;
        this.lblBasis.Location = new System.Drawing.Point(22, 84);
        this.lblBasis.Name = "lblBasis";
        this.lblBasis.Text = "Basis";

        this.lblBasisValue.Font = inputFont;
        this.lblBasisValue.Location = new System.Drawing.Point(168, 82);
        this.lblBasisValue.Name = "lblBasisValue";
        this.lblBasisValue.Size = new System.Drawing.Size(560, 30);
        this.lblBasisValue.Text = "—";

        // ── warning ─────────────────────────────────────────────────
        this.lblWarning.Font = boldFont;
        this.lblWarning.ForeColor = System.Drawing.Color.FromArgb(185, 28, 28);
        this.lblWarning.Location = new System.Drawing.Point(36, 398);
        this.lblWarning.Name = "lblWarning";
        this.lblWarning.Size = new System.Drawing.Size(748, 56);
        this.lblWarning.TabIndex = 6;
        this.lblWarning.Visible = false;

        // ── buttons ─────────────────────────────────────────────────
        this.btnSubmit.BackColor = System.Drawing.Color.FromArgb(11, 37, 69);
        this.btnSubmit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnSubmit.Font = buttonFont;
        this.btnSubmit.ForeColor = System.Drawing.Color.White;
        this.btnSubmit.Location = new System.Drawing.Point(530, 476);
        this.btnSubmit.Name = "btnSubmit";
        this.btnSubmit.Size = new System.Drawing.Size(130, 44);
        this.btnSubmit.TabIndex = 7;
        this.btnSubmit.Text = "File Request";
        this.btnSubmit.UseVisualStyleBackColor = false;
        this.btnSubmit.FlatAppearance.BorderSize = 0;
        this.btnSubmit.Click += new System.EventHandler(this.btnSubmit_Click);

        this.btnCancel.BackColor = System.Drawing.Color.White;
        this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnCancel.Font = buttonFont;
        this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(31, 39, 51);
        this.btnCancel.Location = new System.Drawing.Point(670, 476);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(114, 44);
        this.btnCancel.TabIndex = 8;
        this.btnCancel.Text = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = false;
        this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 191, 168);
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

        // ── form ────────────────────────────────────────────────────
        this.AcceptButton = this.btnSubmit;
        this.CancelButton = this.btnCancel;
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.Color.FromArgb(243, 239, 231);
        this.ClientSize = new System.Drawing.Size(820, 544);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnSubmit);
        this.Controls.Add(this.lblWarning);
        this.Controls.Add(this.grpFee);
        this.Controls.Add(this.cmbPurpose);
        this.Controls.Add(this.lblPurpose);
        this.Controls.Add(this.cmbDocument);
        this.Controls.Add(this.lblDocument);
        this.Controls.Add(this.grpResident);
        this.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Regular);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "RequestForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "New Document Request";
        this.Load += new System.EventHandler(this.RequestForm_Load);
        this.grpResident.ResumeLayout(false);
        this.grpResident.PerformLayout();
        this.grpFee.ResumeLayout(false);
        this.grpFee.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.GroupBox grpResident;
    private System.Windows.Forms.Label lblResident;
    private System.Windows.Forms.Label lblResidentValue;
    private System.Windows.Forms.Label lblClass;
    private System.Windows.Forms.Label lblClassValue;
    private System.Windows.Forms.Label lblResidency;
    private System.Windows.Forms.Label lblResidencyValue;
    private System.Windows.Forms.Label lblDocument;
    private System.Windows.Forms.ComboBox cmbDocument;
    private System.Windows.Forms.Label lblPurpose;
    private System.Windows.Forms.ComboBox cmbPurpose;
    private System.Windows.Forms.GroupBox grpFee;
    private System.Windows.Forms.Label lblFee;
    private System.Windows.Forms.Label lblFeeValue;
    private System.Windows.Forms.Label lblBasis;
    private System.Windows.Forms.Label lblBasisValue;
    private System.Windows.Forms.Label lblWarning;
    private System.Windows.Forms.Button btnSubmit;
    private System.Windows.Forms.Button btnCancel;
}