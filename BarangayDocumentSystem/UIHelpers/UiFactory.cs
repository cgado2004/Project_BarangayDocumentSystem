using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarangayDocumentSystem.UIHelpers;

public static class UiFactory
{
    public static Button PrimaryButton(string text, int width = 150)
    {
        var b = BaseButton(text, width);
        b.BackColor = AppTheme.Primary;
        b.ForeColor = AppTheme.TextOnPrimary;
        b.Font = AppTheme.BodyBoldFont;
        b.FlatAppearance.MouseOverBackColor = AppTheme.PrimaryHover;
        b.FlatAppearance.MouseDownBackColor = AppTheme.PrimaryDark;
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    public static Button SecondaryButton(string text, int width = 150)
    {
        var b = BaseButton(text, width);
        b.BackColor = AppTheme.Surface;
        b.ForeColor = AppTheme.TextPrimary;
        b.Font = AppTheme.BodyFont;
        b.FlatAppearance.BorderColor = AppTheme.BorderStrong;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.MouseOverBackColor = AppTheme.Hover;
        b.FlatAppearance.MouseDownBackColor = AppTheme.Border;
        return b;
    }

    public static Button DangerButton(string text, int width = 150)
    {
        var b = BaseButton(text, width);
        b.BackColor = AppTheme.Danger;
        b.ForeColor = Color.White;
        b.Font = AppTheme.BodyBoldFont;
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = AppTheme.DangerHover;
        b.FlatAppearance.MouseDownBackColor = AppTheme.DangerDark;
        return b;
    }

    private static Button BaseButton(string text, int width) => new Button
    {
        Text = text,
        Width = width,
        Height = AppTheme.ControlHeight,
        AutoSize = true,
        MinimumSize = new Size(width, AppTheme.ControlHeight),
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        UseVisualStyleBackColor = false,
        Margin = new Padding(0, 0, 8, 8)
    };

    public static Panel CardHost(Control inner)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Border,
            Padding = new Padding(1),
            Margin = new Padding(0)
        };
        inner.Dock = DockStyle.Fill;
        host.Controls.Add(inner);
        return host;
    }

    public static TableLayoutPanel Toolbar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = AppTheme.Surface,
            Padding = new Padding(20, 10, 20, 10),
            Margin = new Padding(0),
            ColumnCount = 1,
            RowCount = 1
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bar.Paint += (s, e) =>
        {
            using var pen = new Pen(AppTheme.Border);
            var r = bar.ClientRectangle;
            e.Graphics.DrawLine(pen, 0, r.Height - 1, r.Width, r.Height - 1);
        };
        return bar;
    }

    public static DateTimePicker DayFilter(string name) => new()
    {
        Name = name,
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "MMM dd, yyyy",
        Value = System.DateTime.Today,
        Enabled = false,
        Width = 170,
        Font = AppTheme.BodyFont,
        AccessibleDescription = "Check to filter this date. Uncheck to show all dates."
    };

    public static Control FilterField(string caption, Control input) => FilterField(new Label
    {
        Text = caption, AutoSize = true, Font = AppTheme.BodyFont,
        ForeColor = AppTheme.TextSecondary
    }, input);

    public static Control FilterField(Control caption, Control input)
    {
        var field = new FlowLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false, Margin = new Padding(0, 0, 12, 0)
        };
        caption.Margin = new Padding(0, 7, 8, 7);
        caption.Font = AppTheme.BodyFont;
        caption.ForeColor = AppTheme.TextSecondary;
        field.Controls.Add(caption);
        input.Margin = new Padding(0, 7, 0, 7);
        input.AccessibleName = caption.Text;
        field.Controls.Add(input);
        return field;
    }

    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = AppTheme.Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = AppTheme.BorderStrong;

        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ColumnAdded += (_, e) =>
        {
            e.Column.MinimumWidth = System.Math.Max(100,
                TextRenderer.MeasureText(e.Column.HeaderText, AppTheme.SmallBoldFont).Width + 50);
        };

        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.SurfaceAlt;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.SurfaceAlt;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppTheme.TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.Font = AppTheme.SmallBoldFont;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(14, 0, 14, 0);
        grid.ColumnHeadersHeight = 42;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        grid.DefaultCellStyle.BackColor = AppTheme.Surface;
        grid.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = AppTheme.PrimarySoft;
        grid.DefaultCellStyle.SelectionForeColor = AppTheme.TextPrimary;
        grid.DefaultCellStyle.Font = AppTheme.BodyFont;
        grid.DefaultCellStyle.Padding = new Padding(14, 0, 14, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = AppTheme.SurfaceAlt;
        grid.RowTemplate.Height = 42;
    }

    public static GraphicsPath RoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        if (d <= 0 || rect.Width < d || rect.Height < d)
        {
            path.AddRectangle(rect);
            return path;
        }
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
