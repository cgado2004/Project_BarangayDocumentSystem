using BarangayDocumentSystem.Domain.Abstractions;
using BarangayDocumentSystem.Domain.Services;
using BarangayDocumentSystem.Domain.Templates;
using BarangayDocumentSystem.Infrastructure;
using BarangayDocumentSystem.UI.Views;

namespace BarangayDocumentSystem.UI;

internal static class Program
{
    /// <summary>
    /// Composition root — the ONE place that names concrete classes.
    ///
    /// Everywhere else depends on interfaces. All wiring happens here, by
    /// hand, which keeps the dependency graph visible without pulling in a DI
    /// container (and its NuGet package).
    ///
    /// To move to MySQL: change ONE line below. No view, no form, no template
    /// is touched.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // ---- Domain services ----
        var feeSchedule = new FeeSchedule();
        var profile = BarangayProfile.MagugpoPoblacion;

        // OCP: adding a document = adding a class + one line here.
        // The renderer never changes.
        var renderer = new DocumentRenderer(new IDocumentTemplate[]
        {
            new ClearanceTemplate(),
            new ResidencyTemplate(),
            new IndigencyTemplate(),
            new JobseekerTemplate(),
            new BusinessClearanceTemplate(),
            new GoodMoralTemplate(),
            new BarangayIdTemplate()
        }, profile);

        // ---- Infrastructure ----
        // The only line to change for MySQL:
        //   IBarangayRepository repository = new MySqlBarangayRepository(conn, feeSchedule);
        IBarangayRepository repository = new InMemoryBarangayRepository(feeSchedule);

        // ---- UI ----
        var shell = new MainShell();

        var residents = new ResidentsView(repository, feeSchedule);
        var requests  = new RequestsView(repository, renderer);
        var dashboard = new DashboardView(repository);

        // Filing a request from the Residents page must update the other two.
        residents.RequestFiled += (_, _) =>
        {
            requests.RefreshData();
            dashboard.RefreshData();
        };

        shell.AddView("dashboard", "Dashboard", "▦", dashboard);
        shell.AddView("residents", "Residents", "●", residents);
        shell.AddView("requests",  "Requests",  "▤", requests);

        shell.Start("dashboard");
        shell.SetStatus("Loaded sample data for Barangay Magugpo Poblacion.");

        Application.Run(shell);
    }
}
