// =====================================================================
//  PART:    CustomControls - the flat navigation sidebar
//  ORIGIN:  Draft - Jonathan F. Del Rosario (the design: seal at the top, brand, stacked flat
//           navigation buttons, city footer, one active highlight)
//           Fdraft - Frent Dhieniel Raborar (this file's place in the tree)
//  EDITS:   Clint Wood Gado - my palette (Deep / Primary / GoldSoft), my logo, ApplyTheme, comments
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem.CustomControls;

/// <summary>
/// The navigation rail on the left of the window.
///
/// The design is Jonathan's, from his Draft: the barangay seal at the top,
/// the brand under it, flat full-width buttons stacked below, the city and
/// province in the footer, and exactly one button highlighted as the
/// current page. I kept that shape and painted it in my palette - Deep
/// navy for the rail, Primary for the active page, GoldSoft for the brand -
/// so it matches every other screen. I also made it a control of its own,
/// so the shell only has to listen for <see cref="Navigate"/> and never
/// touches a button.
///
/// The seal is a detached bitmap I own, and I dispose the old one whenever
/// a new one is set, so the file on disk is never locked and nothing leaks.
/// </summary>
public sealed class NavigationSidebar : Panel
{
    public event EventHandler<string>? Navigate;
    private readonly PictureBox _logo = new() { SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Top, Height = 86, TabStop = false };
    private readonly Label _brand = new() { Text = "BARANGAY\r\nMagugpo Poblacion", Dock = DockStyle.Top, Height = 64, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Dictionary<string, Button> _buttons = new();
    private string _active = "dashboard";

    public NavigationSidebar()
    {
        Width = SidebarW;
        Dock = DockStyle.Left;
        Padding = new Padding(12, 20, 12, 12);
        AddNavigation("requests", "Document requests");
        AddNavigation("residents", "Residents");
        AddNavigation("dashboard", "Dashboard");
        Controls.Add(_brand);
        Controls.Add(_logo);
        Controls.Add(new Label { Text = "City of Tagum\r\nDavao del Norte", Dock = DockStyle.Bottom, Height = 56, ForeColor = Color.White, TextAlign = ContentAlignment.MiddleLeft });
        ApplyTheme();
    }

    private void AddNavigation(string key, string caption)
    {
        var button = new Button
        {
            Name = "nav" + key, Text = caption, Dock = DockStyle.Top, Height = 52,
            FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0), Cursor = Cursors.Hand,
            AccessibleName = caption, UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => Navigate?.Invoke(this, key);
        _buttons.Add(key, button);
        Controls.Add(button);
    }

    public Image? Logo
    {
        get => _logo.Image;
        set { var old = _logo.Image; _logo.Image = value; if (!ReferenceEquals(old, value)) old?.Dispose(); }
    }

    public void SetActive(string key) { _active = key; ApplyTheme(); }

    public void ApplyTheme()
    {
        BackColor = Deep;
        _brand.ForeColor = GoldSoft;
        _brand.Font = Subhead;
        foreach (var entry in _buttons)
        {
            entry.Value.Font = Body;
            entry.Value.ForeColor = Color.White;
            entry.Value.BackColor = entry.Key == _active ? Primary : Deep;
            entry.Value.FlatAppearance.MouseOverBackColor = PrimaryDim;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Logo = null;
        base.Dispose(disposing);
    }
}
