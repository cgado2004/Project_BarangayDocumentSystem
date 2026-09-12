using BarangayDocumentSystem.UI.Theme;

namespace BarangayDocumentSystem.UI.Common;

/// <summary>
/// Left navigation rail — the visible half of the TabControl replacement.
///
/// ── DRY ─────────────────────────────────────────────────────────────────
/// Every nav item is created by the same private method, so all items share
/// identical padding, hover behaviour and selected styling by construction.
/// Adding an item is one AddItem call, not a hand-styled control.
///
/// ── SINGLE RESPONSIBILITY ───────────────────────────────────────────────
/// This control only knows how to display a list of destinations and announce
/// which one was clicked. It does not know what a Resident is, and it cannot
/// show a view. MainShell listens and decides what to display.
///
/// That is why the sidebar could be dropped into an unrelated application
/// unchanged.
/// </summary>
public class NavigationSidebar : Panel
{
    private readonly List<Button> _items = new();
    private Button? _selected;

    /// <summary>Raised with the key of the clicked destination.</summary>
    public event EventHandler<string>? NavigationChanged;

    public NavigationSidebar()
    {
        Dock = DockStyle.Left;
        Width = AppTheme.SidebarWidth;
        BackColor = AppTheme.PrimaryDark;
        Padding = new Padding(0, AppTheme.SpaceSm, 0, 0);
    }

    /// <summary>
    /// Adds a destination. <paramref name="key"/> is what NavigationChanged
    /// reports; <paramref name="glyph"/> is a Unicode symbol used instead of
    /// an image file so the app needs no external assets.
    /// </summary>
    public void AddItem(string key, string text, string glyph)
    {
        var button = new Button
        {
            Text = $"   {glyph}    {text}",
            Tag = key,                        // Tag carries the logic value
            Dock = DockStyle.Top,
            Height = 48,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppTheme.BodyFont,
            ForeColor = Color.FromArgb(198, 216, 206),
            BackColor = AppTheme.PrimaryDark,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Padding = new Padding(AppTheme.SpaceSm, 0, 0, 0)
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = AppTheme.Primary;
        button.Click += Item_Click;

        // Docked controls stack in reverse insertion order, so inserting at 0
        // keeps the visual order matching the call order.
        _items.Add(button);
        Controls.Add(button);
        button.BringToFront();

        if (_selected is null)
            Select(button);
    }

    private void Item_Click(object? sender, EventArgs e)
    {
        if (sender is not Button button) return;

        Select(button);
        NavigationChanged?.Invoke(this, (string)button.Tag!);
    }

    /// <summary>Highlights one item and clears the rest.</summary>
    private void Select(Button button)
    {
        foreach (var item in _items)
        {
            item.BackColor = AppTheme.PrimaryDark;
            item.ForeColor = Color.FromArgb(198, 216, 206);
            item.Font = AppTheme.BodyFont;
        }

        button.BackColor = AppTheme.Primary;
        button.ForeColor = AppTheme.TextOnPrimary;
        button.Font = AppTheme.BodyBoldFont;
        _selected = button;
    }

    /// <summary>Selects an item programmatically and raises the event.</summary>
    public void Navigate(string key)
    {
        var target = _items.FirstOrDefault(b => (string)b.Tag! == key);
        if (target is null) return;

        Select(target);
        NavigationChanged?.Invoke(this, key);
    }
}
