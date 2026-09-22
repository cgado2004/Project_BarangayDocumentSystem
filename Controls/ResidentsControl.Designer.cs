using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Controls
{
    partial class ResidentsControl
    {
        private DataGridView gridResidents;
        private TextBox txtSearch;
        private Label lblCount;

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            Padding = new Padding(16);
            BackColor = UiLayout.BackgroundColor;
            gridResidents = UiLayout.Grid();
            gridResidents.Name = "gridResidents";
            UiLayout.Column(gridResidents, "FullName", "Resident", 160);
            UiLayout.Column(gridResidents, "DateOfBirth", "Birth date", 95, "yyyy-MM-dd");
            UiLayout.Column(gridResidents, "Gender", "Gender", 65);
            UiLayout.Column(gridResidents, "Purok", "Purok", 80);
            UiLayout.Column(gridResidents, "ContactNumber", "Contact", 110);
            gridResidents.CellDoubleClick += (sender, args) => { if (args.RowIndex >= 0) EditResident(sender, args); };
            txtSearch = new TextBox { Dock = DockStyle.Fill, MaxLength = 150, Name = "txtSearchResidents" };
            txtSearch.TextChanged += (sender, args) => UiFeedback.Run(this, RefreshData);
            var search = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 0, 0, 10) };
            search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
            search.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            search.Controls.Add(new Label { Text = "Search name, purok, or contact", AutoSize = true, Margin = new Padding(0, 5, 0, 0) }, 0, 0);
            search.Controls.Add(txtSearch, 1, 0);
            var actions = UiLayout.Actions();
            actions.Controls.Add(UiLayout.Button("Register resident", RegisterResident, true));
            actions.Controls.Add(UiLayout.Button("Edit", EditResident));
            actions.Controls.Add(UiLayout.Button("Delete", DeleteResident));
            actions.Controls.Add(UiLayout.Button("File request", FileRequest));
            lblCount = new Label { Dock = DockStyle.Bottom, Height = 36, TextAlign = ContentAlignment.MiddleLeft };
            Controls.Add(gridResidents);
            Controls.Add(lblCount);
            Controls.Add(actions);
            Controls.Add(search);
        }
    }
}
