using System;
using System.Drawing;
using System.Windows.Forms;

namespace BarangayDocumentSystem.Helpers
{
    public static class UiLayout
    {
        public static readonly Color PrimaryColor = Color.FromArgb(31, 91, 76);
        public static readonly Color BackgroundColor = Color.FromArgb(242, 246, 244);

        public static Button Button(string text, EventHandler clicked, bool primary = false)
        {
            var button = new Button
            {
                Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(105, 36), Padding = new Padding(8, 3, 8, 3),
                Margin = new Padding(0, 0, 8, 6), FlatStyle = FlatStyle.Flat,
                BackColor = primary ? PrimaryColor : Color.White,
                ForeColor = primary ? Color.White : Color.FromArgb(35, 45, 42),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(190, 205, 198);
            button.Click += clicked;
            return button;
        }

        public static FlowLayoutPanel Actions()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 8, 0, 4),
                WrapContents = true
            };
        }

        public static DataGridView Grid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
                AutoGenerateColumns = false, MultiSelect = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 32 }, EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 38,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = PrimaryColor, ForeColor = Color.White,
                    SelectionBackColor = PrimaryColor, WrapMode = DataGridViewTriState.False
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    SelectionBackColor = Color.FromArgb(220, 237, 229),
                    SelectionForeColor = Color.Black,
                    Padding = new Padding(3)
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 250, 249) }
            };
        }

        public static void Column(DataGridView grid, string property, string title, int weight = 100, string format = "")
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = property, Name = property, HeaderText = title,
                FillWeight = weight, MinimumWidth = 65,
                DefaultCellStyle = new DataGridViewCellStyle { Format = format }
            });
        }

        public static TableLayoutPanel Fields()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2,
                Padding = new Padding(12), GrowStyle = TableLayoutPanelGrowStyle.AddRows
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return table;
        }

        public static void Field(TableLayoutPanel table, string caption, Control control)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var label = new Label
            {
                Text = caption, AutoSize = true, Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 7, 10, 8)
            };
            control.Dock = DockStyle.Top;
            control.Margin = new Padding(0, 4, 0, 5);
            table.Controls.Add(label, 0, row);
            table.Controls.Add(control, 1, row);
        }

        public static void PrepareDialog(Form form, string title, int width, int height)
        {
            form.Text = title;
            form.ClientSize = new Size(width, height);
            form.MinimumSize = new Size(width, 400);
            form.StartPosition = FormStartPosition.CenterParent;
            form.Font = new Font("Segoe UI", 10F);
            form.BackColor = Color.White;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.ShowInTaskbar = false;
            form.AutoScaleMode = AutoScaleMode.Font;
        }
    }
}
