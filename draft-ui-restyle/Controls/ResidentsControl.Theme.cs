using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Controls
{
    /// <summary>
    /// The THEME half of ResidentsControl. Partial class, new file; OnLoad
    /// runs after InitializeComponent, designer untouched, handlers untouched.
    /// </summary>
    public partial class ResidentsControl
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            BackColor = ModernTheme.Canvas;

            ModernTheme.StyleGrid(gridResidents);

            ModernTheme.StylePrimary(btnRegister);
            ModernTheme.StyleSecondary(btnEdit);
            ModernTheme.StyleSecondary(btnDelete);
            ModernTheme.StyleSecondary(btnRequest);

            lblSearch.Font = ModernTheme.F(9f, false);
            lblSearch.ForeColor = ModernTheme.Muted;
            lblSearch.BackColor = ModernTheme.Canvas;
            txtSearch.BackColor = Color.White;
            txtSearch.Font = ModernTheme.F(10f, false);

            lblCount.Font = ModernTheme.F(9f, false);
            lblCount.ForeColor = ModernTheme.Muted;
            lblCount.BackColor = ModernTheme.Canvas;
        }
    }
}
