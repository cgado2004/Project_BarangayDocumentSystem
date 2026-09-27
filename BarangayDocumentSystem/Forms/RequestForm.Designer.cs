// =====================================================================
//  PART:    Forms - request dialog layout
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
/// The designer half of the new-request dialog. The chrome lives here; the
/// fields, the variable-fee area and the live assessment are built in
/// BuildUi() in the other half.
/// </summary>
partial class RequestForm
{
    private System.ComponentModel.IContainer components = null;

    private BarangayDocumentSystem.UIHelpers.SmoothPanel requestScroll;
    private System.Windows.Forms.FlowLayoutPanel buttonBar;
    private BarangayDocumentSystem.UIHelpers.PillButton btnConfirm;
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
        this.requestScroll = new BarangayDocumentSystem.UIHelpers.SmoothPanel();
        this.buttonBar = new System.Windows.Forms.FlowLayoutPanel();
        this.btnConfirm = new BarangayDocumentSystem.UIHelpers.PillButton();
        this.btnCancel = new BarangayDocumentSystem.UIHelpers.PillButton();

        //
        // requestScroll - the scrolling body of the dialog
        //
        this.requestScroll.Name = "requestScroll";
        this.requestScroll.Dock = System.Windows.Forms.DockStyle.Fill;
        this.requestScroll.AutoScroll = true;
        this.requestScroll.BackColor = System.Drawing.Color.Transparent;

        //
        // btnConfirm
        //
        this.btnConfirm.Name = "btnConfirm";
        this.btnConfirm.Text = "File request";
        this.btnConfirm.Width = 130;
        this.btnConfirm.Click += new System.EventHandler(this.Confirm);

        //
        // btnCancel
        //
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Text = "Cancel";
        this.btnCancel.Width = 110;
        this.btnCancel.Look = BarangayDocumentSystem.UIHelpers.PillButton.Style.Outline;
        this.btnCancel.Click += new System.EventHandler(this.Cancel);

        //
        // buttonBar
        //
        this.buttonBar.Name = "buttonBar";
        this.buttonBar.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.buttonBar.Height = 60;
        this.buttonBar.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        this.buttonBar.WrapContents = false;
        this.buttonBar.BackColor = System.Drawing.Color.Transparent;
        this.buttonBar.Padding = new System.Windows.Forms.Padding(0, 12, 0, 0);
        this.buttonBar.Controls.Add(this.btnConfirm);
        this.buttonBar.Controls.Add(this.btnCancel);

        //
        // RequestForm
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.Text = "New request";
        this.ClientSize = new System.Drawing.Size(660, 640);
        this.MinimumSize = new System.Drawing.Size(600, 540);
        this.CancelButton = this.btnCancel;

        this.Controls.Add(this.requestScroll);
        this.Controls.Add(this.buttonBar);

        this.SuspendLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
