using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Forms
{
    partial class RejectionForm
    {
        private Label lblSummary;
        private TextBox txtReason;

        private void InitializeComponent()
        {
            UiLayout.PrepareDialog(this, "Reject request", 660, 300);
            MinimumSize = new Size(660, 300);
            lblSummary = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12), MaximumSize = new Size(620, 0) };
            txtReason = new TextBox { MaxLength = 300 };
            var fields = UiLayout.Fields();
            UiLayout.Field(fields, "Reason *", txtReason);
            var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            body.Controls.Add(fields);
            body.Controls.Add(lblSummary);
            var actions = UiLayout.Actions();
            actions.Dock = DockStyle.Bottom;
            actions.Padding = new Padding(12);
            var reject = UiLayout.Button("Reject request", RejectRequest, true);
            var cancel = UiLayout.Button("Cancel", (sender, args) => Close());
            cancel.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(reject);
            actions.Controls.Add(cancel);
            AcceptButton = reject;
            CancelButton = cancel;
            Controls.Add(body);
            Controls.Add(actions);
        }
    }
}
