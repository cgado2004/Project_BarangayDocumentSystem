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
        private Label agedWarning;

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

            // ---- footer: live figures, the ageing warning, the official ----
            lblSession.AutoSize = true;
            lblSession.Font = ModernTheme.F(8.5f, false);
            lblSession.ForeColor = ModernTheme.SlateInk;
            lblSession.BackColor = ModernTheme.Canvas;
            agedWarning = new Label
            {
                AutoSize = true,
                BackColor = ModernTheme.Canvas,
                Font = ModernTheme.F(8.5f, true),
                ForeColor = ModernTheme.Crimson
            };
            lblSession.Parent.Controls.Add(agedWarning);
            lblSession.LocationChanged += delegate { PlaceAgedWarning(); };
            lblSession.SizeChanged += delegate { PlaceAgedWarning(); };

            // purok chips navigate to the residents page, pre-filtered
            dashboardPage.PurokChipClicked += delegate(string purok)
            {
                residentsPage.ApplyPurokFilter(purok);
                ShowPage(residentsPage, btnResidents, "Residents");
            };

            RefreshFooter();
        }

        /// <summary>Called after every navigation, so the figures stay live.
        /// ShowPage also toggles the subtitle: it belongs to the dashboard,
        /// where the live figures actually are.</summary>
        internal void RefreshFooter()
        {
            if (reporting == null) return;
            var statistics = reporting.GetStatistics();
            string official = settings != null && settings.Profile != null
                ? settings.Profile.PunongBarangay : "";
            lblSession.Text =
                statistics.TotalResidents + " residents  \u00b7  " +
                statistics.TotalRequests + " requests  \u00b7  \u20b1" +
                statistics.TotalCollected.ToString("N2") + " collected" +
                (official.Length == 0 ? "" : "  \u00b7  " + official);

            if (agedWarning != null)
            {
                agedWarning.Text = "\u26a0  " + statistics.AgedPendingRequests +
                    " past the " + Services.ReportingService.WorkingDayStandard +
                    "-working-day standard";
                agedWarning.Visible = statistics.AgedPendingRequests > 0;
                PlaceAgedWarning();
            }
        }

        /// <summary>Keeps the red warning just right of the live figures,
        /// wherever the session label lands.</summary>
        private void PlaceAgedWarning()
        {
            if (agedWarning != null)
                agedWarning.Location = new Point(lblSession.Right + 18, lblSession.Top);
        }
    }
}
