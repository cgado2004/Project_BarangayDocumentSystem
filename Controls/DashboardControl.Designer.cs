using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Controls
{
    partial class DashboardControl
    {
        private Label lblResidents, lblRequests, lblPending, lblReady, lblFree, lblRevenue;
        private DataGridView gridStatuses, gridTypes, gridPuroks;

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            Padding = new Padding(16);
            BackColor = UiLayout.BackgroundColor;
            var cards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 228, ColumnCount = 3, RowCount = 2 };
            for (int index = 0; index < 3; index++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            lblResidents = AddCard(cards, "REGISTERED RESIDENTS", 0, 0);
            lblRequests = AddCard(cards, "DOCUMENT REQUESTS", 1, 0);
            lblRevenue = AddCard(cards, "COLLECTIONS THIS SESSION", 2, 0);
            lblPending = AddCard(cards, "PENDING REQUESTS", 0, 1);
            lblReady = AddCard(cards, "READY FOR RELEASE", 1, 1);
            lblFree = AddCard(cards, "FREE DOCUMENTS RELEASED", 2, 1);
            var breakdowns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 12, 0, 0) };
            breakdowns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            breakdowns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            breakdowns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
            breakdowns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            gridStatuses = AddBreakdown(breakdowns, "Requests by status", 0);
            gridTypes = AddBreakdown(breakdowns, "Requests by document", 1);
            gridPuroks = AddBreakdown(breakdowns, "Residents by purok", 2);
            Controls.Add(breakdowns);
            Controls.Add(cards);
        }

        private static Label AddCard(TableLayoutPanel cards, string title, int column, int row)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 10, 10), Padding = new Padding(16) };
            var heading = new Label { Text = title, Dock = DockStyle.Top, Height = 27, ForeColor = Color.FromArgb(80, 95, 86) };
            var value = new Label { Text = "0", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 25F), ForeColor = UiLayout.PrimaryColor };
            panel.Controls.Add(value);
            panel.Controls.Add(heading);
            cards.Controls.Add(panel, column, row);
            return value;
        }

        private static DataGridView AddBreakdown(TableLayoutPanel table, string heading, int column)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8), Margin = new Padding(0, 0, 10, 0) };
            var label = new Label { Text = heading, Dock = DockStyle.Top, Height = 34, TextAlign = ContentAlignment.MiddleLeft };
            var grid = UiLayout.Grid();
            UiLayout.Column(grid, "Key", "Category", 200);
            UiLayout.Column(grid, "Value", "Count", 55);
            panel.Controls.Add(grid);
            panel.Controls.Add(label);
            table.Controls.Add(panel, column, 0);
            return grid;
        }
    }
}
