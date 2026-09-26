// =====================================================================
//  PART:    Views - what every screen inherits from
//  ORIGIN:  leader_draft - Clint Wood Gado
//  EDITS:   Clint Wood Gado - v3.2: holds the repository for its subclasses
//           and adds Persist, the one place a failed database write is
//           handled (Frent's reload-after-failure rule, applied once)
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.UIHelpers;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem.Views;

/// <summary>
/// What every screen in my application inherits from.
///
/// I wrote this so scrolling, flicker and padding are solved once instead of
/// three times. It IS a SmoothPanel, so the whole surface is composited and
/// double-buffered, and AutoScroll together with AutoScrollMinSize means
/// that when the window is made smaller than the content, scrollbars appear
/// rather than controls being silently cut off. That is what makes the app
/// usable on a small laptop as well as on a desk monitor.
///
/// v3.2 gives it a second job: talking to storage safely. Now that the
/// repository can be a real database, any write can fail, and three screens
/// handling that three different ways is how inconsistencies start. So
/// <see cref="Persist"/> is the one wrapper, and the screens call it.
/// </summary>
public abstract class ViewBase : SmoothPanel
{
    /// <summary>The store this screen reads from and writes to. Handed in,
    /// never created here - the screens do not know which one it is.</summary>
    protected IBarangayRepository Repository { get; }

    protected ViewBase(IBarangayRepository repository)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));

        Dock = DockStyle.Fill;
        BackColor = Canvas;
        AutoScroll = true;
        AutoScrollMinSize = new Size(860, 520);
        Padding = new Padding(PagePad);
    }

    /// <summary>The shell calls this whenever it shows my view, so the screen
    /// can reload itself from the repository. I made it virtual so each screen
    /// decides for itself what refreshing means.</summary>
    public virtual void OnShown() { }

    /// <summary>
    /// Run one change against storage and tell the clerk if it did not
    /// take.
    ///
    /// A <see cref="RepositoryException"/> means the database refused or
    /// vanished. I show the reason, then ask the repository to reload, so
    /// the grid goes back to what is really on disk instead of showing a
    /// change that never happened. I return false so the caller can skip
    /// whatever it was going to do next (navigate, print a receipt).
    ///
    /// Rule violations - InvalidOperationException from the request's state
    /// machine - are NOT caught here on purpose. Those are the clerk's to
    /// read and act on, and the screens already show them as warnings.
    /// </summary>
    protected bool Persist(Action work, string whatFailed)
    {
        try
        {
            work();
            return true;
        }
        catch (RepositoryException ex)
        {
            Dialog.Error(this,
                whatFailed + " was not saved.\n\n" + ex.Message,
                "Database problem");

            try
            {
                Repository.Reload();
            }
            catch (RepositoryException)
            {
                // The server is still down. The working set stays as it was;
                // the message above already told the clerk what to do.
            }

            return false;
        }
    }
}
