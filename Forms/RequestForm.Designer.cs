using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Forms
{
    partial class RequestForm
    {
        private ComboBox cmbResident, cmbDocument;
        private TextBox txtPurpose, txtBusinessName, txtBusinessAddress, txtBusinessNature;
        private Label lblFee;
        private Button btnSubmit;
        private TableLayoutPanel businessFields;

        private void InitializeComponent()
        {
            UiLayout.PrepareDialog(this, "New document request", 760, 550);
            cmbResident = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DropDownWidth = 480 };
            cmbDocument = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DropDownWidth = 480 };
            txtPurpose = new TextBox { MaxLength = 300 };
            txtBusinessName = new TextBox { MaxLength = 120 };
            txtBusinessAddress = new TextBox { MaxLength = 250 };
            txtBusinessNature = new TextBox { MaxLength = 150 };
            lblFee = new Label { AutoSize = true, MaximumSize = new Size(650, 0), Padding = new Padding(12), ForeColor = UiLayout.PrimaryColor };
            var fields = UiLayout.Fields();
            UiLayout.Field(fields, "Resident *", cmbResident);
            UiLayout.Field(fields, "Document *", cmbDocument);
            UiLayout.Field(fields, "Purpose *", txtPurpose);
            businessFields = UiLayout.Fields();
            UiLayout.Field(businessFields, "Business name *", txtBusinessName);
            UiLayout.Field(businessFields, "Business address *", txtBusinessAddress);
            UiLayout.Field(businessFields, "Nature of business *", txtBusinessNature);
            var content = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1 };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.Controls.Add(fields);
            content.Controls.Add(businessFields);
            content.Controls.Add(lblFee);
            var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            body.Controls.Add(content);
            var actions = UiLayout.Actions();
            actions.Dock = DockStyle.Bottom;
            actions.Padding = new Padding(12);
            btnSubmit = UiLayout.Button("File request", SubmitRequest, true);
            var cancel = UiLayout.Button("Cancel", (sender, args) => Close());
            cancel.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(btnSubmit);
            actions.Controls.Add(cancel);
            AcceptButton = btnSubmit;
            CancelButton = cancel;
            Controls.Add(body);
            Controls.Add(actions);
        }
    }
}
