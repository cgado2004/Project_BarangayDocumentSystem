using BarangayDocumentSystem.Helper;

namespace BarangayDocumentSystem.Helper;

/// <summary>
/// Builds themed controls.
///
/// ── DRY ─────────────────────────────────────────────────────────────────
/// A styled button previously took six lines in a Designer file — BackColor,
/// FlatStyle, Font, ForeColor, Size, UseVisualStyleBackColor — repeated for
/// every button across six forms. Roughly 40 near-identical blocks.
///
/// One call now. And because every control is built here, restyling the whole
/// application means editing one method.
/// </summary>
public static class UiFactory
{
    public static Button PrimaryButton(string text, int width = 150)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = AppTheme.ControlHeight + 6,
            BackColor = AppTheme.Primary,
            ForeColor = AppTheme.TextOnPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.BodyBoldFont,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = AppTheme.PrimaryLight;
        button.FlatAppearance.MouseDownBackColor = AppTheme.PrimaryDark;
        return button;
    }

    public static Button SecondaryButton(string text, int width = 150)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = AppTheme.ControlHeight + 6,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.BodyFont,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderColor = AppTheme.Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = AppTheme.Background;
        return button;
    }

    public static Button DangerButton(string text, int width = 150)
    {
        var button = SecondaryButton(text, width);
        button.ForeColor = AppTheme.Danger;
        button.FlatAppearance.BorderColor = AppTheme.Danger;
        return button;
    }

    public static Label Heading(string text) => new()
    {
        Text = text,
        Font = AppTheme.HeadingFont,
        ForeColor = AppTheme.TextPrimary,
        AutoSize = true
    };

    public static Label Caption(string text) => new()
    {
        Text = text,
        Font = AppTheme.SmallFont,
        ForeColor = AppTheme.TextSecondary,
        AutoSize = true
    };

    /// <summary>A card panel — white surface, thin border, used for grouping.</summary>
    public static Panel Card() => new()
    {
        BackColor = AppTheme.Surface,
        Padding = new Padding(AppTheme.SpaceMd),
        BorderStyle = BorderStyle.FixedSingle
    };

    /// <summary>
    /// Applies the house style to a DataGridView.
    ///
    /// ── DRY ─────────────────────────────────────────────────────────────
    /// The four grids in v1 each repeated twelve property assignments in their
    /// Designer files. One call replaces all of it, and the four grids can no
    /// longer drift out of visual sync.
    /// </summary>
    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = AppTheme.Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = AppTheme.Border;

        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextOnPrimary;
        grid.ColumnHeadersDefaultCellStyle.Font = AppTheme.BodyBoldFont;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(AppTheme.SpaceSm, 6, 0, 6);
        grid.ColumnHeadersHeight = 40;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        grid.DefaultCellStyle.Font = AppTheme.BodyFont;
        grid.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = AppTheme.PrimaryLight;
        grid.DefaultCellStyle.SelectionForeColor = AppTheme.TextOnPrimary;
        grid.DefaultCellStyle.Padding = new Padding(AppTheme.SpaceSm, 4, 0, 4);

        grid.AlternatingRowsDefaultCellStyle.BackColor = AppTheme.Background;
        grid.RowTemplate.Height = 34;
    }
}
