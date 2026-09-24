using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Forms
{
    /// <summary>
    /// The THEME half of MainForm. Partial class, new file, so the designer
    /// surface keeps compiling untouched. OnLoad runs strictly after
    /// InitializeComponent and after the constructor has filled the labels.
    /// </summary>
    public partial class MainForm
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            // the six-card dashboard does not wrap at this size
            ClientSize = new Size(1360, 860);
            MinimumSize = new Size(1120, 700);
            BackColor = ModernTheme.Canvas;
            pnlBody.BackColor = ModernTheme.Canvas;
            pnlContent.BackColor = ModernTheme.Canvas;

            // ---- sidebar ----
            ModernTheme.StyleSidebar(
                this, pnlSidebar, lblBarangay, lblBarangay,
                new[] { btnDashboard, btnResidents, btnRequests }, btnDashboard);

            // lblBarangay is repurposed as the gold BRAND overline; the bold
            // name label and the seal are added by StyleSidebar next to it.
            lblBarangay.AutoSize = false;
            lblBarangay.Size = new Size(180, 20);
            lblBarangay.Location = new Point(lblBarangay.Left, Math.Max(24, lblBarangay.Top) + 58);
            lblBarangay.TextAlign = ContentAlignment.MiddleLeft;

            // ---- page chrome ----
            lblPageTitle.Font = ModernTheme.F(20f, true);
            lblPageTitle.ForeColor = ModernTheme.Ink;
            lblPageTitle.BackColor = ModernTheme.Canvas;

            // ---- footer: the red live-figures line ----
            lblSession.Font = ModernTheme.F(8.5f, false);
            lblSession.ForeColor = ModernTheme.Crimson;
            lblSession.BackColor = ModernTheme.Canvas;
            RefreshFooter();
        }

        /// <summary>Called after every navigation, so the figures stay live.</summary>
        internal void RefreshFooter()
        {
            if (reporting == null) return;
            var statistics = reporting.GetStatistics();
            lblSession.Text =
                statistics.PendingRequests + " pending  \u00b7  " +
                statistics.ReadyRequests + " ready for release  \u00b7  \u20b1" +
                statistics.TotalCollected.ToString("N2") +
                " collected  \u2014  records are saved on this computer.";
        }
    }
}
