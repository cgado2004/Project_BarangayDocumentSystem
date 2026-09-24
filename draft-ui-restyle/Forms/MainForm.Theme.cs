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
        private Label pageSubtitle;

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
            // name label, the seal and the bottom city/province lines are
            // added by StyleSidebar next to it.
            lblBarangay.AutoSize = false;
            lblBarangay.Size = new Size(180, 20);
            lblBarangay.Location = new Point(lblBarangay.Left, Math.Max(24, lblBarangay.Top) + 58);
            lblBarangay.TextAlign = ContentAlignment.MiddleLeft;

            // ---- page chrome: big title + the quiet subtitle under it ----
            lblPageTitle.Font = ModernTheme.F(20f, true);
            lblPageTitle.ForeColor = ModernTheme.Ink;
            lblPageTitle.BackColor = ModernTheme.Canvas;

            pageSubtitle = new Label
            {
                Text = "Live figures for the barangay office",
                AutoSize = false,
                Size = new Size(460, 20),
                Location = new Point(lblPageTitle.Left, lblPageTitle.Bottom + 1),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Font = ModernTheme.F(9.5f, false),
                ForeColor = ModernTheme.Muted,
                BackColor = ModernTheme.Canvas
            };
            lblPageTitle.Parent.Controls.Add(pageSubtitle);
            pageSubtitle.BringToFront();

            // ---- footer: the red live-figures line (no DPI text, ever) ----
            lblSession.Font = ModernTheme.F(8.5f, false);
            lblSession.ForeColor = ModernTheme.Crimson;
            lblSession.BackColor = ModernTheme.Canvas;
            RefreshFooter();
        }

        /// <summary>Called after every navigation, so the figures stay live.
        /// ShowPage also toggles the subtitle: it belongs to the dashboard,
        /// where the live figures actually are.</summary>
        internal void RefreshFooter()
        {
            if (reporting == null) return;
            var statistics = reporting.GetStatistics();
            lblSession.Text =
                statistics.TotalResidents + " residents  \u00b7  " +
                statistics.TotalRequests + " requests  \u00b7  \u20b1" +
                statistics.TotalCollected.ToString("N2") +
                " collected  \u2014  records are saved on this computer.";
        }
    }
}
