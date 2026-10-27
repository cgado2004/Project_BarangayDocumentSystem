using System;
using System.Drawing;
using System.Windows.Forms;

namespace BarangayDocumentSystem.UIHelpers;

public static class DialogLayout
{
    public static void MakeScrollable(Form form)
    {
        form.Load += (_, _) => FixDpiAutosize(form);
        form.Shown += (_, _) =>
        {
            FitToScreen(form);
            ConfigureScrolling(form);
        };
    }

    private static void FixDpiAutosize(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child.AutoSize)
            {
                child.AutoSize = false;
                child.AutoSize = true;
            }
            FixDpiAutosize(child);
        }
    }

    private static void ConfigureScrolling(Form form)
    {
        int neededH = 0;
        foreach (Control c in form.Controls)
        {
            if (c.Dock != DockStyle.None) continue;
            neededH = Math.Max(neededH, c.Bottom + c.Margin.Bottom);
        }
        neededH += 12;

        bool needsVertical = neededH > form.ClientSize.Height;

        if (!needsVertical)
        {
            form.AutoScroll = false;
            form.AutoScrollMinSize = Size.Empty;
            return;
        }

        form.AutoScroll = true;
        form.AutoScrollMargin = Size.Empty;
        form.AutoScrollMinSize = new Size(0, neededH);
    }

    public static void FitToScreen(Form form)
    {
        var raw = Screen.FromControl(form.Owner ?? form).WorkingArea;
        double scale = Math.Max(1.0, form.DeviceDpi / 96.0);
        var area = new Rectangle(
            (int)(raw.Left * scale),
            (int)(raw.Top * scale),
            (int)(raw.Width * scale),
            (int)(raw.Height * scale));

        form.Height = Math.Min(form.Height, area.Height);
        form.Location = new Point(
            Math.Max(area.Left, Math.Min(form.Left, area.Right - form.Width)),
            Math.Max(area.Top, Math.Min(form.Top, area.Bottom - form.Height)));
    }
}
