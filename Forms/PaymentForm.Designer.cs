namespace BarangayDocumentSystem.Forms
{
    partial class PaymentForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblSummary;
        private System.Windows.Forms.TextBox txtReceipt;
        private System.Windows.Forms.TableLayoutPanel tblFields;
        private System.Windows.Forms.Label lblReceiptCaption;
        private System.Windows.Forms.Panel pnlBody;
        private System.Windows.Forms.FlowLayoutPanel pnlActions;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblSummary = new System.Windows.Forms.Label();
            this.txtReceipt = new System.Windows.Forms.TextBox();
            this.tblFields = new System.Windows.Forms.TableLayoutPanel();
            this.lblReceiptCaption = new System.Windows.Forms.Label();
            this.pnlBody = new System.Windows.Forms.Panel();
            this.pnlActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tblFields.SuspendLayout();
            this.pnlBody.SuspendLayout();
            this.pnlActions.SuspendLayout();
            this.SuspendLayout();

            // lblSummary
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.TabIndex = 0;
            this.lblSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSummary.AutoSize = true;
            this.lblSummary.Padding = new System.Windows.Forms.Padding(12);
            this.lblSummary.MaximumSize = new System.Drawing.Size(580, 0);

            // txtReceipt
            this.txtReceipt.Name = "txtReceipt";
            this.txtReceipt.TabIndex = 1;
            this.txtReceipt.MaxLength = 50;

            // tblFields
            this.tblFields.Name = "tblFields";
            this.tblFields.TabIndex = 2;
            this.tblFields.ColumnCount = 2;
            this.tblFields.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblFields.AutoSize = true;
            this.tblFields.Padding = new System.Windows.Forms.Padding(12);
            this.tblFields.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddRows;
            this.tblFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
            this.tblFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // lblReceiptCaption
            this.lblReceiptCaption.Name = "lblReceiptCaption";
            this.lblReceiptCaption.TabIndex = 3;
            this.lblReceiptCaption.Text = "Official receipt no. *";
            this.lblReceiptCaption.AutoSize = true;
            this.lblReceiptCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblReceiptCaption.Margin = new System.Windows.Forms.Padding(0, 7, 10, 8);
            this.txtReceipt.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtReceipt.Margin = new System.Windows.Forms.Padding(0, 4, 0, 5);
            this.tblFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFields.Controls.Add(this.lblReceiptCaption, 0, 0);
            this.tblFields.Controls.Add(this.txtReceipt, 1, 0);

            // pnlBody
            this.pnlBody.Name = "pnlBody";
            this.pnlBody.TabIndex = 4;
            this.pnlBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBody.AutoScroll = true;
            this.pnlBody.Controls.Add(this.tblFields);
            this.pnlBody.Controls.Add(this.lblSummary);

            // pnlActions
            this.pnlActions.Name = "pnlActions";
            this.pnlActions.TabIndex = 5;
            this.pnlActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlActions.AutoSize = true;
            this.pnlActions.Padding = new System.Windows.Forms.Padding(12);
            this.pnlActions.WrapContents = true;

            // btnSave
            this.btnSave.Name = "btnSave";
            this.btnSave.TabIndex = 6;
            this.btnSave.Text = "Record payment";
            this.btnSave.AutoSize = true;
            this.btnSave.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSave.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnSave.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnSave.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(31, 91, 76);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);

            // btnCancel
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.AutoSize = true;
            this.btnCancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCancel.MinimumSize = new System.Drawing.Size(105, 36);
            this.btnCancel.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0, 0, 8, 6);
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(35, 45, 42);
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(190, 205, 198);
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.pnlActions.Controls.Add(this.btnSave);
            this.pnlActions.Controls.Add(this.btnCancel);
            this.AcceptButton = this.btnSave;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.pnlBody);
            this.Controls.Add(this.pnlActions);

            // PaymentForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(620, 310);
            this.MinimumSize = new System.Drawing.Size(620, 310);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.Name = "PaymentForm";
            this.Text = "Record payment";
            this.tblFields.RowCount = 1;
            this.btnSave.Click += new System.EventHandler(this.SavePayment);
            this.btnCancel.Click += new System.EventHandler(this.CloseDialog);
            this.tblFields.ResumeLayout(false);
            this.tblFields.PerformLayout();
            this.pnlBody.ResumeLayout(false);
            this.pnlBody.PerformLayout();
            this.pnlActions.ResumeLayout(false);
            this.pnlActions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
