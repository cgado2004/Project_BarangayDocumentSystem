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

            // ---- the six existing stat panels become accent cards ----
            // Mapping to the design's six accents uses the panels the data
            // already provides. The spec's "Released" tile has no matching
            // statistic in this project, so the sixth card keeps its real
            // meaning (all document requests) and takes the emerald slot.
            Style(pnlResidents, lblResidentsHeading, lblResidents, ModernTheme.PrimaryNavy);
            Style(pnlPending, lblPendingHeading, lblPending, ModernTheme.Amber);
            Style(pnlReady, lblReadyHeading, lblReady, ModernTheme.Sky);
            Style(pnlRequests, lblRequestsHeading, lblRequests, ModernTheme.Emerald);
            Style(pnlRevenue, lblRevenueHeading, lblRevenue, ModernTheme.SlateInk);
            Style(pnlFree, lblFreeHeading, lblFree, ModernTheme.Crimson);

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

        private void Style(Panel card, Label heading, Label value, Color accent)
        {
            ModernTheme.StyleStatCard(card, heading, value, accent);
            card.Paint += delegate { }; // paint handler wired inside StyleStatCard
        }

        private void OnStatistics(BarangayStatistics statistics)
        {
            if (statistics == null) return;

            // the spec prices the collected card in pesos
            lblRevenue.Text = "\u20b1" + statistics.TotalCollected.ToString("N2");

            // ---- purok pills ----
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
    }
}
