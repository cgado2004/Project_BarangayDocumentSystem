using System;
using System.Drawing;
using System.Windows.Forms;

namespace BarangayDocumentSystem.UIHelpers;

public static class DialogLayout
{

    public static void MakeScrollable(Form form)
    {
        form.AutoScroll = true;
        form.AutoScrollMargin = new Size(SystemInformation.VerticalScrollBarWidth + 8,
                                        SystemInformation.HorizontalScrollBarHeight + 8);
        form.Load += (_, _) => form.AutoScrollMinSize = form.ClientSize;
        form.Shown += (_, _) => FitToScreen(form);
    }

    public static void FitToScreen(Form form)
    {
        var area = Screen.FromControl(form.Owner ?? form).WorkingArea;
        form.Size = new Size(Math.Min(form.Width, area.Width), Math.Min(form.Height, area.Height));
        form.Location = new Point(
            Math.Max(area.Left, Math.Min(form.Left, area.Right - form.Width)),
            Math.Max(area.Top, Math.Min(form.Top, area.Bottom - form.Height)));
    }
}
