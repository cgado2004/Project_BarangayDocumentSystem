using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Helper;

namespace BarangayDocumentSystem;

/// <summary>
/// Base for every page shown in the content area.
///
/// ── DRY ─────────────────────────────────────────────────────────────────
/// Each view needs the same scaffolding: a repository reference, a title
/// header, a status-message channel back to the shell, and a Refresh hook.
/// Writing that three times invites three slightly different versions.
///
/// ── LISKOV SUBSTITUTION ─────────────────────────────────────────────────
/// MainShell holds views only as ViewBase. Any subclass can be swapped in and
/// the shell keeps working, because every subclass honours the same contract:
/// construct with a repository, expose a Title, respond to Refresh().
///
/// ── OPEN/CLOSED ─────────────────────────────────────────────────────────
/// A new page means a new subclass plus one registration line in MainShell.
/// No existing view is touched.
/// </summary>
public abstract class ViewBase : UserControl
{
    /// <summary>
    /// Depends on the INTERFACE, never the concrete repository (DIP).
    /// This is what makes the views testable with a fake store.
    /// </summary>
    protected IBarangayRepository Repository { get; }

    /// <summary>Shown in the content header.</summary>
    public abstract string Title { get; }

    /// <summary>Optional line under the title.</summary>
    public virtual string Subtitle => string.Empty;

    /// <summary>Lets a view push a message to the shell's status bar.</summary>
    public event EventHandler<string>? StatusChanged;

    protected ViewBase(IBarangayRepository repository)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));

        Dock = DockStyle.Fill;
        BackColor = AppTheme.Background;
        Padding = new Padding(AppTheme.SpaceLg);
    }

    /// <summary>Re-reads data. Called when the view is shown or data changes.</summary>
    public abstract void RefreshData();

    protected void SetStatus(string message) => StatusChanged?.Invoke(this, message);
}
