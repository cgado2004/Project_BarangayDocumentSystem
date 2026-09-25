using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Views;
using BarangayDocumentSystem.CustomControls;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.UIHelpers;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem.Views;

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

}
