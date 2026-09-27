// =====================================================================
//  PART:    Forms - resident dialog layout
//  ORIGIN:  the group's shared design - first modelled in Draft - Jonathan F. Del Rosario,
//           given this place in the tree by Fdraft - Frent Dhieniel Raborar;
//           the code and comments in this file are my v3.1 rewrite (leader_draft - Clint Wood Gado)
//  EDITS:   Clint Wood Gado - v3.1 layout, header
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Views;
using BarangayDocumentSystem.CustomControls;
namespace BarangayDocumentSystem.Forms;

/// <summary>
/// The designer half of the resident dialog.
///
/// Visual Studio opens the form from this file. The chrome only - the
/// metrics, the scrolling surface and the button bar - lives here; the
/// fields themselves are built in BuildFields() in the other half, because
/// they are generated from the registry's value lists and have nothing to
/// gain from being hand-dragged.
/// </summary>
partial class ResidentForm
{
    private System.ComponentModel.IContainer components = null;

    private BarangayDocumentSystem.UIHelpers.SmoothPanel residentScroll;
    private System.Windows.Forms.FlowLayoutPanel buttonBar;
    private BarangayDocumentSystem.UIHelpers.PillButton btnSave;
    private BarangayDocumentSystem.UIHelpers.PillButton btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.residentScroll = new BarangayDocumentSystem.UIHelpers.SmoothPanel();
        this.buttonBar = new System.Windows.Forms.FlowLayoutPanel();
        this.btnSave = new BarangayDocumentSystem.UIHelpers.PillButton();
        this.btnCancel = new BarangayDocumentSystem.UIHelpers.PillButton();

        //
        // residentScroll - the scrolling body of the dialog
        //
        this.residentScroll.Name = "residentScroll";
        this.residentScroll.Dock = System.Windows.Forms.DockStyle.Fill;
        this.residentScroll.AutoScroll = true;
        this.residentScroll.BackColor = System.Drawing.Color.Transparent;

        //
        // btnSave
        //
        this.btnSave.Name = "btnSave";
        this.btnSave.Text = "Save";
        this.btnSave.Width = 120;
        this.btnSave.Click += new System.EventHandler(this.Save);

        //
        // btnCancel
        //
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Text = "Cancel";
        this.btnCancel.Width = 110;
        this.btnCancel.Look = BarangayDocumentSystem.UIHelpers.PillButton.Style.Outline;
        this.btnCancel.Click += new System.EventHandler(this.Cancel);

        //
        // buttonBar - RightToLeft flow, so Save sits at the bottom right
        // and Cancel at its left.
        //
        this.buttonBar.Name = "buttonBar";
        this.buttonBar.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.buttonBar.Height = 60;
        this.buttonBar.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        this.buttonBar.WrapContents = false;
        this.buttonBar.BackColor = System.Drawing.Color.Transparent;
        this.buttonBar.Padding = new System.Windows.Forms.Padding(0, 12, 0, 0);
        this.buttonBar.Controls.Add(this.btnSave);
        this.buttonBar.Controls.Add(this.btnCancel);

        //
        // ResidentForm
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.Text = "Add resident";
        this.ClientSize = new System.Drawing.Size(720, 560);
        this.MinimumSize = new System.Drawing.Size(660, 500);
        this.CancelButton = this.btnCancel;

        this.Controls.Add(this.residentScroll);
        this.Controls.Add(this.buttonBar);

        this.SuspendLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
