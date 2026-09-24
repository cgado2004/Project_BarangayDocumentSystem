using System.Windows.Forms;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Views;

/// <summary>
/// Dashboard: statistic cards plus two breakdown lists.
///
/// v1 rendered all of this as one monospaced text blob in a read-only TextBox.
/// This version uses real cards — the same information, legible at a glance.
/// </summary>
public class DashboardView : ViewBase
{
    private readonly FlowLayoutPanel _cards = new();
    private readonly Panel _breakdowns = new();

    public override string Title => "Dashboard";
    public override string Subtitle => "Registry and request statistics at a glance";

    public DashboardView(IBarangayRepository repository) : base(repository)
    {
        _cards.Dock = DockStyle.Top;
        _cards.AutoSize = true;
        _cards.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _cards.AutoScroll = false;
        _cards.WrapContents = true;
        _cards.BackColor = AppTheme.Background;

        _breakdowns.Dock = DockStyle.Fill;
        _breakdowns.BackColor = AppTheme.Background;
        _breakdowns.Padding = new Padding(0, AppTheme.SpaceMd, 0, 0);

        Controls.Add(_breakdowns);
        Controls.Add(_cards);
    }

    public override void RefreshData()
    {
        var stats = Repository.GetStatistics();

        // Dispose the old controls before clearing — WinForms controls hold
        // unmanaged window handles, and Clear() alone does not release them.
        DisposeChildren(_cards);
        DisposeChildren(_breakdowns);

        // DRY: one helper builds every card, so all ten look identical.
        _cards.Controls.Add(StatCard("Total Residents", stats.TotalResidents.ToString(),
                                     AppTheme.Primary));
        _cards.Controls.Add(StatCard("Registered Voters", stats.RegisteredVoters.ToString(),
                                     AppTheme.Info));
        _cards.Controls.Add(StatCard("Senior Citizens", stats.SeniorCitizens.ToString(),
                                     AppTheme.Accent));
        _cards.Controls.Add(StatCard("Total Requests", stats.TotalRequests.ToString(),
                                     AppTheme.Primary));
        _cards.Controls.Add(StatCard("Pending", stats.Pending.ToString(), AppTheme.Warning));
        _cards.Controls.Add(StatCard("Processing", stats.Processing.ToString(), AppTheme.Info));
        _cards.Controls.Add(StatCard("Ready for Release", stats.ReadyForRelease.ToString(),
                                     AppTheme.Accent));
        _cards.Controls.Add(StatCard("Released", stats.Released.ToString(), AppTheme.Success));
        _cards.Controls.Add(StatCard("Total Collected", $"₱{stats.TotalCollected:N2}",
                                     AppTheme.Success));
        _cards.Controls.Add(StatCard("Issued Free", stats.IssuedFreeOfCharge.ToString(),
                                     AppTheme.TextSecondary));

        var right = BreakdownPanel("Residents by Purok", stats.ResidentsByPurok);
        right.Dock = DockStyle.Right;
        right.Width = 340;

        var left = BreakdownPanel("Requests by Document Type", stats.RequestsByDocumentType);
        left.Dock = DockStyle.Fill;

        _breakdowns.Controls.Add(left);
        _breakdowns.Controls.Add(right);
    }

    /// <summary>One statistic card. Built once, reused ten times (DRY).</summary>
    private static Panel StatCard(string caption, string value, Color accent)
    {
        var card = new Panel
        {
            Width = 210,
            Height = 104,
            BackColor = AppTheme.Surface,
            Margin = new Padding(0, 0, AppTheme.SpaceMd, AppTheme.SpaceMd),
            Padding = new Padding(AppTheme.SpaceMd, AppTheme.SpaceSm, AppTheme.SpaceSm, AppTheme.SpaceSm)
        };

        // A coloured stripe down the left edge, drawn rather than imaged so
        // the app ships with no asset files.
        var stripe = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = accent };

        var valueLabel = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 19f, FontStyle.Bold),
            ForeColor = accent,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 46,
            TextAlign = ContentAlignment.BottomLeft
        };

        var captionLabel = new Label
        {
            Text = caption.ToUpper(),
            Font = AppTheme.SmallFont,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.TopLeft
        };

        card.Controls.Add(captionLabel);
        card.Controls.Add(valueLabel);
        card.Controls.Add(stripe);
        return card;
    }

    private static Panel BreakdownPanel(string heading, IReadOnlyDictionary<string, int> data)
    {
        var panel = new Panel
        {
            BackColor = AppTheme.Surface,
            Padding = new Padding(AppTheme.SpaceMd),
            Margin = new Padding(AppTheme.SpaceSm)
        };

        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            BorderStyle = BorderStyle.None,
            Font = AppTheme.BodyFont,
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };

        list.Columns.Add("Item", 240);
        list.Columns.Add("Count", 70, HorizontalAlignment.Right);

        if (data.Count == 0)
        {
            // Empty state — better than an unexplained blank box.
            list.Items.Add(new ListViewItem(new[] { "(no data yet)", "" })
            {
                ForeColor = AppTheme.TextSecondary
            });
        }
        else
        {
            foreach (var pair in data.OrderByDescending(p => p.Value))
                list.Items.Add(new ListViewItem(new[] { pair.Key, pair.Value.ToString() }));
        }

        var title = new Label
        {
            Text = heading,
            Dock = DockStyle.Top,
            Height = 32,
            Font = AppTheme.SubheadFont,
            ForeColor = AppTheme.TextPrimary
        };

        panel.Controls.Add(list);
        panel.Controls.Add(title);
        return panel;
    }

    private static void DisposeChildren(Control parent)
    {
        // ToList() first — mutating Controls while enumerating it throws.
        foreach (Control child in parent.Controls.Cast<Control>().ToList())
        {
            parent.Controls.Remove(child);
            child.Dispose();
        }
    }
}
