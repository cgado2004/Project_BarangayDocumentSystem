using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Controls
{
    /// <summary>
    /// The THEME half of DashboardControl. Partial class, new file, zero edits
    /// to the original: OnLoad runs after InitializeComponent (designer-safe)
    /// and after the constructor's first RefreshData. The original refresh
    /// wiring is untouched - this file only listens to the StatisticsUpdated
    /// notification the main file raises at the end of RefreshData.
    /// </summary>
    public partial class DashboardControl
    {
        private FlowLayoutPanel chipFlow;
        private Panel barHost;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            BackColor = ModernTheme.Canvas;

            // ---- hero banner (new control, docked on top) ----
            var hero = new HeroBanner();
            Controls.Add(hero);
            hero.BringToFront();
            hero.Height = 150;

            // ---- the six stat cards, arranged like the reference ----
            // The designer's TableLayoutPanel holds them 3 x 2; the theme
            // re-orders them at RUN TIME (SetCellPosition) so the designer
            // file stays untouched. Labels are re-titled to the short forms
            // and each card gets its accent colour and a small caption.
            //
            // Every figure stays one the data really provides. The "RELEASED"
            // count comes from the same RequestsByStatus list the Statuses
            // grid is bound to - no new statistic is invented anywhere.
            tblCards.SetCellPosition(pnlPending,  new TableLayoutPanelCellPosition(1, 0));
            tblCards.SetCellPosition(pnlReady,    new TableLayoutPanelCellPosition(2, 0));
            tblCards.SetCellPosition(pnlRequests, new TableLayoutPanelCellPosition(0, 1));
            tblCards.SetCellPosition(pnlRevenue,  new TableLayoutPanelCellPosition(1, 1));
            // pnlResidents already sits at (0,0) and pnlFree at (2,1).

            Style(pnlResidents, lblResidentsHeading, lblResidents,
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

            // ---- breakdowns: keep gridStatuses as a styled table; replace
            //      gridTypes with navy bars and gridPuroks with pills ----
            ModernTheme.StyleGrid(gridStatuses);
            lblStatusesHeading.Font = ModernTheme.F(10f, true);
            lblStatusesHeading.ForeColor = ModernTheme.Ink;
            pnlStatuses.BackColor = Color.White;

            ModernTheme.StyleGrid(gridTypes);
            gridTypes.Visible = false;   // kept populated by the original wiring
            barHost = new Panel { BackColor = Color.White, Dock = DockStyle.Fill, AutoScroll = true };
            pnlTypes.Controls.Add(barHost);
            barHost.BringToFront();

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

            foreach (var heading in new[] { lblTypesHeading, lblPuroksHeading })
            {
                heading.Font = ModernTheme.F(10f, true);
                heading.ForeColor = ModernTheme.Ink;
            }
            pnlTypes.BackColor = Color.White;
            pnlPuroks.BackColor = Color.White;

            // first paint of the overlays from the current figures
            OnStatistics(reporting == null ? null : reporting.GetStatistics());

            // and every refresh afterwards (raised by RefreshData)
            StatisticsUpdated += OnStatistics;
        }

        /// <summary>Retitles a stat card, paints its accent and adds the
        /// small caption under the value. All three labels stay on the same
        /// panels the designer created; nothing is re-parented elsewhere.</summary>
        private void Style(Panel card, Label heading, Label value,
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
        }

        private void OnStatistics(BarangayStatistics statistics)
        {
            if (statistics == null) return;

            // the reference prices the collected card in pesos
            lblRevenue.Text = "\u20b1" + statistics.TotalCollected.ToString("N2");

            // the RELEASED figure: the same list the statuses grid binds to
            lblRequests.Text = CountOf(statistics, "Released").ToString();

            // ---- purok pills (display only - filtering is behaviour, and
            //      this restyle adds none) ----
            if (chipFlow != null)
            {
                chipFlow.SuspendLayout();
                chipFlow.Controls.Clear();
                foreach (var pair in statistics.ResidentsByPurok)
                {
                    string label = pair.Key + "  \u00b7  " + pair.Value;
                    var chip = new PillChip
                    {
                        Text = label,
                        Width = TextRenderer.MeasureText(label, chipFlow.Font).Width + 40,
                        Margin = new Padding(4, 4, 8, 6)
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
