using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Controls
{
    partial class RequestsControl
    {
        private DataGridView gridRequests;
        private TextBox txtSearch, txtDetails;
        private ComboBox cmbStatus;
        private Button btnProcess, btnReady, btnRelease, btnPay, btnReject, btnPreview;

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            Padding = new Padding(16);
            BackColor = UiLayout.BackgroundColor;
            gridRequests = UiLayout.Grid();
            gridRequests.Name = "gridRequests";
            UiLayout.Column(gridRequests, "ReferenceNumber", "Reference", 95);
            UiLayout.Column(gridRequests, "ResidentName", "Resident", 130);
            UiLayout.Column(gridRequests, "DocumentName", "Document", 180);
            UiLayout.Column(gridRequests, "StatusText", "Status", 115);
            UiLayout.Column(gridRequests, "Fee", "Fee (PHP)", 65, "N2");
            UiLayout.Column(gridRequests, "PaymentText", "Payment", 70);
            txtSearch = new TextBox { MaxLength = 150, Dock = DockStyle.Fill };
            cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            cmbStatus.Items.AddRange(new object[] { "All statuses", "Pending", "Processing", "Ready for Release", "Released", "Rejected" });
            cmbStatus.SelectedIndex = 0;
            var search = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, Padding = new Padding(0, 0, 0, 8) };
            search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
            search.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
            search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            search.Controls.Add(new Label { Text = "Search", AutoSize = true, Margin = new Padding(0, 5, 0, 0) }, 0, 0);
            search.Controls.Add(txtSearch, 1, 0);
            search.Controls.Add(new Label { Text = "Status", AutoSize = true, Margin = new Padding(8, 5, 0, 0) }, 2, 0);
            search.Controls.Add(cmbStatus, 3, 0);
            var actions = UiLayout.Actions();
            actions.Controls.Add(UiLayout.Button("New request", NewRequest, true));
            btnProcess = UiLayout.Button("Start processing", (sender, args) => ChangeStatus(requests.StartProcessing));
            btnReady = UiLayout.Button("Mark ready", (sender, args) => ChangeStatus(requests.MarkReady));
            btnRelease = UiLayout.Button("Release", ReleaseRequest, true);
            btnPay = UiLayout.Button("Record payment", RecordPayment);
            btnReject = UiLayout.Button("Reject", RejectRequest);
            btnPreview = UiLayout.Button("View / print", PreviewDocument);
            actions.Controls.AddRange(new Control[] { btnProcess, btnReady, btnPay, btnRelease, btnReject, btnPreview });
            txtDetails = new TextBox
            {
                Dock = DockStyle.Bottom, Height = 116, Multiline = true, ReadOnly = true,
                ScrollBars = ScrollBars.Vertical, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle
            };
            gridRequests.SelectionChanged += (sender, args) => UpdateSelection();
            txtSearch.TextChanged += (sender, args) => UiFeedback.Run(this, RefreshData);
            cmbStatus.SelectedIndexChanged += (sender, args) => UiFeedback.Run(this, RefreshData);
            Controls.Add(gridRequests);
            Controls.Add(txtDetails);
            Controls.Add(actions);
            Controls.Add(search);
            UpdateSelection();
        }
    }
}
