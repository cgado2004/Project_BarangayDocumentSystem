using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Controls
{
    /// <summary>
    /// The THEME half of RequestsControl. Partial class, new file; OnLoad
    /// runs after InitializeComponent, designer untouched, handlers untouched.
    /// </summary>
    public partial class RequestsControl
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            BackColor = ModernTheme.Canvas;

            ModernTheme.StyleGrid(gridRequests);

            ModernTheme.StylePrimary(btnNewRequest);
            ModernTheme.StylePrimary(btnRelease);
            ModernTheme.StyleSecondary(btnProcess);
            ModernTheme.StyleSecondary(btnReady);
            ModernTheme.StyleSecondary(btnPay);
            ModernTheme.StyleSecondary(btnReject);
            ModernTheme.StyleSecondary(btnPreview);

            lblSearch.Font = ModernTheme.F(9f, false);
            lblSearch.ForeColor = ModernTheme.Muted;
            lblSearch.BackColor = ModernTheme.Canvas;
            txtSearch.BackColor = Color.White;
            txtSearch.Font = ModernTheme.F(10f, false);

            lblStatus.Font = ModernTheme.F(9f, false);
            lblStatus.ForeColor = ModernTheme.Muted;
            lblStatus.BackColor = ModernTheme.Canvas;

            // the details panel reads as a card
            txtDetails.BackColor = Color.White;
            txtDetails.BorderStyle = BorderStyle.FixedSingle;
            txtDetails.Font = ModernTheme.F(9.5f, false);
            txtDetails.ForeColor = ModernTheme.Ink;
        }
    }
}
