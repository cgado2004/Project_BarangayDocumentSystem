#nullable enable
using System;
using System.Windows.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Views;

public record NavigationRequest(string Key, string? Argument = null);

public abstract class ViewBase : UserControl
{
    protected IBarangayRepository Repository { get; }

    public abstract string Title { get; }
    public virtual string Subtitle => string.Empty;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<NavigationRequest>? NavigateRequested;

    protected ViewBase(IBarangayRepository repository)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Dock = DockStyle.Fill;
        BackColor = AppTheme.Background;
        Margin = new Padding(0);
        Padding = new Padding(0);
    }

    public abstract void RefreshData();

    protected void SetStatus(string message) => StatusChanged?.Invoke(this, message);

    protected void NavigateTo(string key, string? argument = null) =>
        NavigateRequested?.Invoke(this, new NavigationRequest(key, argument));

    protected bool Attempt(Action action)
    {
        try { action(); return true; }
        catch (RepositoryException ex) { RecoverFrom(ex); return false; }
    }

    protected T? AttemptGet<T>(Func<T> func) where T : class
    {
        try { return func(); }
        catch (RepositoryException ex) { RecoverFrom(ex); return null; }
    }

    private void RecoverFrom(RepositoryException ex)
    {
        Dialog.Error(ex.Message + "\n\nYour last change was NOT saved.", "Database problem");
        try { Repository.Reload(); } catch (RepositoryException) { }
        RefreshData();
    }
}