using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helper;
using static BarangayDocumentSystem.Helper.AppTheme;

namespace BarangayDocumentSystem;

/// <summary>
/// What every screen in my application inherits from.
///
/// I wrote this so scrolling, flicker and padding are solved once instead of
/// three times. It IS a SmoothPanel, so the whole surface is composited and
/// double-buffered (the v3.1 smooth-scrolling fix), and AutoScroll together
/// with AutoScrollMinSize means that when the window is made smaller than
/// the content, scrollbars appear rather than controls being silently cut
/// off. That is what makes the app usable on a small laptop as well as on a
/// desk monitor.
/// </summary>
public class ViewBase : SmoothPanel
{
    /// <summary>The shell calls this whenever it shows my view, so the screen
    /// can reload itself from the repository. I made it virtual so each screen
    /// decides for itself what refreshing means.</summary>
    public virtual void OnShown() { }

    public ViewBase()
    {
        Dock = DockStyle.Fill;
        BackColor = Canvas;
        AutoScroll = true;
        AutoScrollMinSize = new Size(860, 520);
        Padding = new Padding(PagePad);
    }

    /// <summary>The big page title with a grey line of context underneath. I
    /// build it here so all of my screens look the same.</summary>
    protected static Panel PageHeader(string title, string subtitle)
    {
        var host = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = Color.Transparent
        };

        var h = new Label
        {
            Text = title,
            Font = Display,
            ForeColor = Ink,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 44
        };

        var s = new Label
        {
            Text = subtitle,
            Font = Body,
            ForeColor = Muted,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 24
        };

        host.Controls.Add(s);
        host.Controls.Add(h);
        return host;
    }
}
