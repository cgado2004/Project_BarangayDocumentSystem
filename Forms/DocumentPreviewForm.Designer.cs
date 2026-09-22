using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Forms
{
    partial class DocumentPreviewForm
    {
        private TextBox txtDocument;
        private Button btnPrint, btnPrintPreview;
        private Label lblState;

        private void InitializeComponent()
        {
            UiLayout.PrepareDialog(this, "Document preview", 900, 720);
            txtDocument = new TextBox
            {
                Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White, Font = new Font("Segoe UI", 11F), BorderStyle = BorderStyle.FixedSingle,
                TabStop = false
            };
            lblState = new Label { Dock = DockStyle.Top, Height = 40, Padding = new Padding(12, 8, 0, 0) };
            var actions = UiLayout.Actions();
            actions.Dock = DockStyle.Bottom;
            actions.Padding = new Padding(12);
            btnPrintPreview = UiLayout.Button("Print preview", PrintPreview);
            actions.Controls.Add(btnPrintPreview);
            btnPrint = UiLayout.Button("Print...", PrintDocument, true);
            actions.Controls.Add(btnPrint);
            var close = UiLayout.Button("Close", (sender, args) => Close());
            close.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(close);
            CancelButton = close;
            Controls.Add(txtDocument);
            Controls.Add(lblState);
            Controls.Add(actions);
        }
    }
}
