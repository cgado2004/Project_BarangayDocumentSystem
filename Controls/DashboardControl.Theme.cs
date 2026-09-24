using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Controls
{
    /// <summary>
    /// The THEME half of DashboardControl, drawn to the approved dashboard
    /// preview: hero banner, ONE row of six accent stat cards, purok chips
    /// that filter, document-type bars, and no third grid. Partial class -
    /// the designer file and the refresh wiring stay exactly as committed.
    /// </summary>
    public partial class DashboardControl
    {
        private FlowLayoutPanel chipFlow;
        private Panel barHost;
        private HeroBanner heroBanner;
        private Label pendingNote;
        private BarangayProfile profile;

        /// <summary>A purok chip was clicked - the shell switches to the
        /// residents page and filters the registry to that purok.</summary>
        internal event Action<string> PurokChipClicked;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyTheme();
        }

        /// <summary>Identity from AppSettings: the hero paints the barangay
        /// profile instead of hardcoded strings. Safe before and after load.</summary>
        internal void SetProfile(BarangayProfile value)
        {
            profile = value;
            if (heroBanner == null || profile == null) return;
            heroBanner.BarangayLine = profile.BarangayName;
            heroBanner.CityLine = profile.CityName + ", " + profile.ProvinceName;
            heroBanner.OfficialLine = profile.PunongBarangay;
            heroBanner.Invalidate();
        }

        private void ApplyTheme()
        {
            BackColor = ModernTheme.Canvas;

            // ---- hero banner (docked on top of the page) ----
            heroBanner = new HeroBanner();
            SetProfile(profile);
            Controls.Add(heroBanner);
            heroBanner.BringToFront();
            heroBanner.Height = 150;

            // ---- the six stat cards in ONE row, like the reference ----
            // The designer's table holds 3 columns x 2 rows; the theme widens
            // it to six at run time so the designer file stays untouched.
            tblCards.ColumnCount = 6;
            tblCards.RowCount = 1;
            while (tblCards.ColumnStyles.Count < 6)
                tblCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6f));
            for (int column = 0; column < 6; column++)
                tblCards.ColumnStyles[column] = new ColumnStyle(SizeType.Percent, 100f / 6f);
            var cards = new[] { pnlResidents, pnlPending, pnlReady, pnlRequests, pnlRevenue, pnlFree };
            for (int column = 0; column < cards.Length; column++)
                tblCards.SetCellPosition(cards[column], new TableLayoutPanelCellPosition(column, 0));

            pendingNote = Style(pnlResidents, lblResidentsHeading, lblResidents,
                "RESIDENTS", "on the registry", ModernTheme.PrimaryNavy);
            Style(pnlPending, lblPendingHeading, lblPending,
                "PENDING", "to be processed", ModernTheme.Amber);
            Style(pnlReady, lblReadyHeading, lblReady,
                "READY", "to be collected", ModernTheme.Sky);
            Style(pnlRequests, lblRequestsHeading, lblRequests,
                "RELEASED", "issued to date", ModernTheme.Emerald);
            Style(pnlRevenue, lblRevenueHeading, lblRevenue,
                "COLLECTED", "against receipts", ModernTheme.SlateInk);
            Style(pnlFree, lblFreeHeading, lblFree,
                "ISSUED FREE", "statutory exemptions", ModernTheme.Crimson);

            // ---- breakdowns: chips and bars only; the statuses table the
            //      original wiring populates is kept but out of view ----
            gridStatuses.Visible = false;
            pnlStatuses.Visible = false;
            // two cards share the row the way the reference draws it
            // (purok chips left, document bars right); the hidden statuses
            // card collapses into a zero-width third column.
            tblBreakdowns.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, 58f);
            tblBreakdowns.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, 42f);
            tblBreakdowns.ColumnStyles[2] = new ColumnStyle(SizeType.Percent, 0f);
            tblBreakdowns.SetCellPosition(pnlPuroks, new TableLayoutPanelCellPosition(0, 0));
            tblBreakdowns.SetCellPosition(pnlTypes, new TableLayoutPanelCellPosition(1, 0));

            lblTypesHeading.Font = ModernTheme.F(10f, true);
            lblTypesHeading.ForeColor = ModernTheme.Ink;
            pnlTypes.BackColor = Color.White;
            ModernTheme.StyleGrid(gridTypes);
            gridTypes.Visible = false;   // kept populated by the original wiring
            barHost = new Panel { BackColor = Color.White, Dock = DockStyle.Fill, AutoScroll = true };
            pnlTypes.Controls.Add(barHost);
            barHost.BringToFront();

            lblPuroksHeading.Text = "Residents by purok  \u2014  click one to filter";
            lblPuroksHeading.Font = ModernTheme.F(10f, true);
            lblPuroksHeading.ForeColor = ModernTheme.Ink;
            pnlPuroks.BackColor = Color.White;
            ModernTheme.StyleGrid(gridPuroks);
            gridPuroks.Visible = false;  // kept populated by the original wiring
            chipFlow = new FlowLayoutPanel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(4, 8, 4, 4)
            };
            pnlPuroks.Controls.Add(chipFlow);
            chipFlow.BringToFront();

            // first paint of the overlays from the current figures
            OnStatistics(reporting == null ? null : reporting.GetStatistics());

            // and every refresh afterwards (raised by RefreshData)
            StatisticsUpdated += OnStatistics;
        }

        /// <summary>Retitles a stat card, paints its accent and adds the
        /// small caption under the value. Returns that caption so callers
        /// can keep it live (the PENDING card counts the aged requests).</summary>
        private Label Style(Panel card, Label heading, Label value,
                            string title, string caption, Color accent)
        {
            heading.Text = title;
            ModernTheme.StyleStatCard(card, heading, value, accent);

            var note = new Label
            {
                Text = caption,
                AutoSize = false,
                Size = new Size(card.Width - 20, 16),
                Location = new Point(14, card.Height - 24),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Font = ModernTheme.F(8f, false),
                ForeColor = ModernTheme.Muted,
                BackColor = Color.White
            };
            card.Controls.Add(note);
            note.BringToFront();
            return note;
        }

        private void OnStatistics(BarangayStatistics statistics)
        {
            if (statistics == null) return;

            // the reference prices the collected card in pesos
            lblRevenue.Text = "\u20b1" + statistics.TotalCollected.ToString("N2");

            // the RELEASED figure: the same list the statuses grid binds to
            lblRequests.Text = CountOf(statistics, "Released").ToString();

            // the PENDING caption carries the Charter's ageing figure
            if (pendingNote != null)
                pendingNote.Text = statistics.AgedPendingRequests > 0
                    ? "to be processed \u00b7 " + statistics.AgedPendingRequests + " aged"
                    : "to be processed";

            // ---- purok chips: click one to filter the residents page ----
            if (chipFlow != null)
            {
                chipFlow.SuspendLayout();
                chipFlow.Controls.Clear();
                foreach (var pair in statistics.ResidentsByPurok)
                {
                    string purok = pair.Key;
                    string label = purok + "  \u00b7  " + pair.Value;
                    var chip = new PillChip
                    {
                        Text = label,
                        Width = TextRenderer.MeasureText(label, chipFlow.Font).Width + 40,
                        Margin = new Padding(4, 4, 8, 6),
                        Cursor = Cursors.Hand
                    };
                    chip.Click += (sender, args) =>
                    {
                        if (PurokChipClicked != null) PurokChipClicked(purok);
                    };
                    chipFlow.Controls.Add(chip);
                }
                chipFlow.ResumeLayout(true);
            }

            // ---- document bars ----
            if (barHost != null)
            {
                barHost.SuspendLayout();
                barHost.Controls.Clear();
                int max = 1;
                foreach (var pair in statistics.RequestsByDocument)
                    if (pair.Value > max) max = pair.Value;

                foreach (var pair in statistics.RequestsByDocument)
                {
                    barHost.Controls.Add(new NavyBar
                    {
                        Caption = pair.Key,
                        Value = pair.Value,
                        Maximum = max,
                        Dock = DockStyle.Top,
                        Height = 40
                    });
                }
                barHost.ResumeLayout(true);
            }
        }

        /// <summary>One named status count out of the statistics the
        /// ReportingService already produces - or zero if it is absent.</summary>
        private static int CountOf(BarangayStatistics statistics, string status)
        {
            foreach (var pair in statistics.RequestsByStatus)
                if (pair.Key == status) return pair.Value;
            return 0;
        }
    }
}
