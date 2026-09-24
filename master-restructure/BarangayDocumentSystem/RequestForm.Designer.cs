namespace BarangayDocumentSystem.UI.Forms;

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

        // ============================ grpResident =========================
        this.grpResident.Controls.Add(this.lblResidencyValue);
        this.grpResident.Controls.Add(this.lblResidency);
        this.grpResident.Controls.Add(this.lblClassValue);
        this.grpResident.Controls.Add(this.lblClass);
        this.grpResident.Controls.Add(this.lblResidentValue);
        this.grpResident.Controls.Add(this.lblResident);
        this.grpResident.Location = new System.Drawing.Point(20, 18);
        this.grpResident.Name = "grpResident";
        this.grpResident.Size = new System.Drawing.Size(600, 118);
        this.grpResident.TabIndex = 0;
        this.grpResident.TabStop = false;
        this.grpResident.Text = "Requesting resident";

        this.lblResident.AutoSize = true;
        this.lblResident.Location = new System.Drawing.Point(18, 28);
        this.lblResident.Name = "lblResident";
        this.lblResident.Size = new System.Drawing.Size(51, 20);
        this.lblResident.TabIndex = 0;
        this.lblResident.Text = "Name:";

        this.lblResidentValue.AutoSize = true;
        this.lblResidentValue.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
        this.lblResidentValue.Location = new System.Drawing.Point(150, 28);
        this.lblResidentValue.Name = "lblResidentValue";
        this.lblResidentValue.Size = new System.Drawing.Size(20, 20);
        this.lblResidentValue.TabIndex = 1;
        this.lblResidentValue.Text = "—";

        this.lblClass.AutoSize = true;
        this.lblClass.Location = new System.Drawing.Point(18, 56);
        this.lblClass.Name = "lblClass";
        this.lblClass.Size = new System.Drawing.Size(100, 20);
        this.lblClass.TabIndex = 2;
        this.lblClass.Text = "Classification:";

        this.lblClassValue.AutoSize = true;
        this.lblClassValue.Location = new System.Drawing.Point(150, 56);
        this.lblClassValue.Name = "lblClassValue";
        this.lblClassValue.Size = new System.Drawing.Size(20, 20);
        this.lblClassValue.TabIndex = 3;
        this.lblClassValue.Text = "—";

        this.lblResidency.AutoSize = true;
        this.lblResidency.Location = new System.Drawing.Point(18, 84);
        this.lblResidency.Name = "lblResidency";
        this.lblResidency.Size = new System.Drawing.Size(80, 20);
        this.lblResidency.TabIndex = 4;
        this.lblResidency.Text = "Residency:";

        this.lblResidencyValue.AutoSize = true;
        this.lblResidencyValue.Location = new System.Drawing.Point(150, 84);
        this.lblResidencyValue.Name = "lblResidencyValue";
        this.lblResidencyValue.Size = new System.Drawing.Size(20, 20);
        this.lblResidencyValue.TabIndex = 5;
        this.lblResidencyValue.Text = "—";

        // ============================== document ==========================
        this.lblDocument.AutoSize = true;
        this.lblDocument.Location = new System.Drawing.Point(20, 156);
        this.lblDocument.Name = "lblDocument";
        this.lblDocument.Size = new System.Drawing.Size(122, 20);
        this.lblDocument.TabIndex = 1;
        this.lblDocument.Text = "Document type:";

        this.cmbDocument.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbDocument.Location = new System.Drawing.Point(170, 153);
        this.cmbDocument.Name = "cmbDocument";
        this.cmbDocument.Size = new System.Drawing.Size(450, 28);
        this.cmbDocument.TabIndex = 2;
        this.cmbDocument.SelectedIndexChanged += new System.EventHandler(this.cmbDocument_SelectedIndexChanged);

        this.lblPurpose.AutoSize = true;
        this.lblPurpose.Location = new System.Drawing.Point(20, 196);
        this.lblPurpose.Name = "lblPurpose";
        this.lblPurpose.Size = new System.Drawing.Size(63, 20);
        this.lblPurpose.TabIndex = 3;
        this.lblPurpose.Text = "Purpose:";

        // Editable ComboBox — clerks can pick a common purpose or type one.
        this.cmbPurpose.Location = new System.Drawing.Point(170, 193);
        this.cmbPurpose.MaxLength = 120;
        this.cmbPurpose.Name = "cmbPurpose";
        this.cmbPurpose.Size = new System.Drawing.Size(450, 28);
        this.cmbPurpose.TabIndex = 4;

        // ================================ grpFee ==========================
        this.grpFee.Controls.Add(this.lblBasisValue);
        this.grpFee.Controls.Add(this.lblBasis);
        this.grpFee.Controls.Add(this.lblFeeValue);
        this.grpFee.Controls.Add(this.lblFee);
        this.grpFee.Location = new System.Drawing.Point(20, 236);
        this.grpFee.Name = "grpFee";
        this.grpFee.Size = new System.Drawing.Size(600, 100);
        this.grpFee.TabIndex = 5;
        this.grpFee.TabStop = false;
        this.grpFee.Text = "Fee assessment";

        this.lblFee.AutoSize = true;
        this.lblFee.Location = new System.Drawing.Point(18, 30);
        this.lblFee.Name = "lblFee";
        this.lblFee.Size = new System.Drawing.Size(35, 20);
        this.lblFee.TabIndex = 0;
        this.lblFee.Text = "Fee:";

        this.lblFeeValue.AutoSize = true;
        this.lblFeeValue.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
        this.lblFeeValue.Location = new System.Drawing.Point(146, 24);
        this.lblFeeValue.Name = "lblFeeValue";
        this.lblFeeValue.Size = new System.Drawing.Size(30, 31);
        this.lblFeeValue.TabIndex = 1;
        this.lblFeeValue.Text = "—";

        this.lblBasis.AutoSize = true;
        this.lblBasis.Location = new System.Drawing.Point(18, 66);
        this.lblBasis.Name = "lblBasis";
        this.lblBasis.Size = new System.Drawing.Size(45, 20);
        this.lblBasis.TabIndex = 2;
        this.lblBasis.Text = "Basis:";

        this.lblBasisValue.Location = new System.Drawing.Point(146, 66);
        this.lblBasisValue.Name = "lblBasisValue";
        this.lblBasisValue.Size = new System.Drawing.Size(440, 24);
        this.lblBasisValue.TabIndex = 3;
        this.lblBasisValue.Text = "—";

        // =============================== warning ==========================
        this.lblWarning.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
        this.lblWarning.ForeColor = System.Drawing.Color.FromArgb(170, 60, 40);
        this.lblWarning.Location = new System.Drawing.Point(20, 344);
        this.lblWarning.Name = "lblWarning";
        this.lblWarning.Size = new System.Drawing.Size(600, 44);
        this.lblWarning.TabIndex = 6;
        this.lblWarning.Visible = false;

        this.btnSubmit.BackColor = System.Drawing.Color.FromArgb(21, 71, 52);
        this.btnSubmit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnSubmit.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
        this.btnSubmit.ForeColor = System.Drawing.Color.White;
        this.btnSubmit.Location = new System.Drawing.Point(370, 396);
        this.btnSubmit.Name = "btnSubmit";
        this.btnSubmit.Size = new System.Drawing.Size(130, 40);
        this.btnSubmit.TabIndex = 7;
        this.btnSubmit.Text = "File Request";
        this.btnSubmit.UseVisualStyleBackColor = false;
        this.btnSubmit.Click += new System.EventHandler(this.btnSubmit_Click);

        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(510, 396);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(110, 40);
        this.btnCancel.TabIndex = 8;
        this.btnCancel.Text = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = true;
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

        this.AcceptButton = this.btnSubmit;
        this.CancelButton = this.btnCancel;
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(644, 456);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnSubmit);
        this.Controls.Add(this.lblWarning);
        this.Controls.Add(this.grpFee);
        this.Controls.Add(this.cmbPurpose);
        this.Controls.Add(this.lblPurpose);
        this.Controls.Add(this.cmbDocument);
        this.Controls.Add(this.lblDocument);
        this.Controls.Add(this.grpResident);
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
