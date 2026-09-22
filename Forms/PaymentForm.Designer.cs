using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Forms
{
    partial class PaymentForm
    {
        private Label lblSummary;
        private TextBox txtReceipt;

        private void InitializeComponent()
        {
            UiLayout.PrepareDialog(this, "Record payment", 620, 310);
            MinimumSize = new Size(620, 310);
            lblSummary = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12), MaximumSize = new Size(580, 0) };
            txtReceipt = new TextBox { MaxLength = 50 };
            var fields = UiLayout.Fields();
            UiLayout.Field(fields, "Official receipt no. *", txtReceipt);
            var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            body.Controls.Add(fields);
            body.Controls.Add(lblSummary);
            var actions = UiLayout.Actions();
            actions.Dock = DockStyle.Bottom;
            actions.Padding = new Padding(12);
            var save = UiLayout.Button("Record payment", SavePayment, true);
            var cancel = UiLayout.Button("Cancel", (sender, args) => Close());
            cancel.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(save);
            actions.Controls.Add(cancel);
            AcceptButton = save;
            CancelButton = cancel;
            Controls.Add(body);
            Controls.Add(actions);
        }
    }
}
