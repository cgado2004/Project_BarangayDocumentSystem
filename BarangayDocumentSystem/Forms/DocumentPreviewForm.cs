using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Views;
using BarangayDocumentSystem.CustomControls;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BarangayDocumentSystem.UIHelpers;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.BusinessRules;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem.Forms;

/// <summary>
/// The multi-monitor scaled print preview, v3.1 core fix 6.
///
/// The preview is a real PrintPreviewControl over the same PrintDocument
/// the Print button uses, so what you see is exactly what prints. The zoom
/// starts at 100% and can be stepped, and - the fix - when the form lands
/// on a monitor with a different DPI, WM_DPICHANGED arrives and I rescale
/// the zoom by the same ratio, so the page keeps its apparent size on
/// screen instead of snapping smaller or larger.
/// </summary>
public partial class DocumentPreviewForm : DialogBase
{
    private const int WM_DPICHANGED = 0x02E0;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    private DocumentRequest? _request;
    private PrintDocument? _document;

    private readonly PrintPreviewControl _preview = new();
    private readonly PillButton _zoomOut = new() { Text = "–", Width = 44 };
    private readonly PillButton _zoomIn  = new() { Text = "+", Width = 44 };
    private readonly Label _zoomLabel = new();
    private readonly PillButton _print  = new() { Text = "Print", Width = 100 };
    private readonly PillButton _close  = new() { Text = "Close", Width = 100, Look = PillButton.Style.Outline };

    private double _zoom = 1.0;
    private int _lastDpi = 96;

    public DocumentPreviewForm()
    {
        InitializeComponent();
        BuildUi();

        // The DPI the form opened at. When WM_DPICHANGED arrives I compare
        // against this, then keep it current.
        try { _lastDpi = DeviceDpi; } catch { _lastDpi = 96; }
    }

    public DocumentPreviewForm(DocumentRequest request) : this()
    {
        _request = request;

        Text = $"Preview — {request.GetReferenceNumber()}";

        var renderer = new DocumentRenderer(BarangayProfile.Current);
        _document = renderer.CreatePrintDocument(request);
        _preview.Document = _document;
        ApplyZoom();
    }

    private void BuildUi()
    {
        _preview.Dock = DockStyle.Fill;
        _preview.AutoZoom = false;
        _preview.Zoom = 1.0;
        _preview.BackColor = System.Drawing.Color.FromArgb(0xE8, 0xEC, 0xF4);
        _preview.BorderStyle = BorderStyle.None;
        _preview.StartPage = 0;

        var bar = new System.Windows.Forms.FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft,
            BackColor = System.Drawing.Color.Transparent,
            Padding = new Padding(0, 10, 0, 0),
            WrapContents = false
        };

        _close.Width = 100;
        _print.Width = 110;
        _zoomIn.Width = 46;
        _zoomOut.Width = 46;

        _zoomLabel.Text = "100 %";
        _zoomLabel.Font = SmallBold;
        _zoomLabel.ForeColor = Muted;
        _zoomLabel.AutoSize = true;
        _zoomLabel.Margin = new Padding(10, 14, 10, 0);
        _zoomLabel.BackColor = System.Drawing.Color.Transparent;

        bar.Controls.Add(_close);
        bar.Controls.Add(_print);
        bar.Controls.Add(_zoomLabel);
        bar.Controls.Add(_zoomIn);
        bar.Controls.Add(_zoomOut);

        _zoomIn.Click  += (_, _) => { _zoom = Math.Min(4.0, _zoom * 1.25); ApplyZoom(); };
        _zoomOut.Click += (_, _) => { _zoom = Math.Max(0.20, _zoom / 1.25); ApplyZoom(); };
        _print.Click   += (_, _) => PrintDocument();
        _close.Click   += (_, _) => Close();
        CancelButton = _close;

        previewHost.Controls.Add(_preview);
        Controls.Add(bar);
        Controls.Add(previewHost);
    }

    private void ApplyZoom()
    {
        _preview.Zoom = _zoom;
        _zoomLabel.Text = $"{(int)Math.Round(_zoom * 100)} %";
    }

    private void PrintDocument()
    {
        if (_document is null) return;

        try
        {
            _document.Print();
        }
        catch (SystemException ex)
        {
            // No printer, spooler stopped, whatever it is - the preview must
            // survive it and say what happened.
            Dialog.Error(this,
                $"The document could not be printed on this machine.\n\n{ex.Message}",
                "Print");
        }
    }

    // =================================================================
    //  WM_DPICHANGED - the multi-monitor handler, core fix 6
    // =================================================================

    /// <summary>
    /// Windows sends WM_DPICHANGED when the form crosses onto a monitor with
    /// a different DPI. lParam carries the rectangle Windows suggests for
    /// the window at the new DPI, and wParam's low word is the new DPI. I
    /// apply the suggested rectangle and scale the preview zoom by the same
    /// ratio, so the paper holds its on-screen size instead of jumping.
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_DPICHANGED)
        {
            int newDpi = m.WParam.ToInt32() & 0xFFFF;

            if (newDpi > 0 && _lastDpi > 0 && newDpi != _lastDpi)
            {
                double ratio = (double)newDpi / _lastDpi;
                _zoom = Math.Max(0.20, Math.Min(4.0, _zoom * ratio));
                _lastDpi = newDpi;
                ApplyZoom();
            }

            var rect = Marshal.PtrToStructure<RECT>(m.LParam);
            Bounds = System.Drawing.Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        base.WndProc(ref m);
    }
}
