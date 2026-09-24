using System.Windows.Forms;
using System;
using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.BusinessRules.DocumentTemplates;
using BarangayDocumentSystem.Database;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Views;

namespace BarangayDocumentSystem;

internal static class Program
{
    /// <summary>
    /// Composition root — the ONE place that names concrete classes.
    ///
    /// Everywhere else depends on interfaces. All wiring happens here, by
    /// hand, which keeps the dependency graph visible without pulling in a DI
    /// container (and its NuGet package).
    /// </summary>
    [STAThread]
    static void Main()
    {
        // The .NET 6+ WinForms template generates this pair as
        // ApplicationConfiguration.Initialize(); .NET Framework has no such
        // generator, so it is written out by hand here. Per-monitor DPI
        // awareness itself is declared in App.config, since .NET Framework
        // reads that setting before Main() ever runs.
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // ---- Business rules ----
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

        // ---- Database ----
        // If MySQL is not reachable there is nothing useful to show, so explain
        // why and exit instead of opening an empty window or crashing.
        IBarangayRepository repository;
        try
        {
            repository = ConnectToDatabase(feeSchedule);
        }
        catch (Exception ex) when (ex is RepositoryException or ArgumentException)
        {
            MessageBox.Show(
                ex.Message + "\n\n" +
                $"Check that the MySQL server is running, then check {"BarangayDocumentSystem.exe.config"} " +
                "(next to the program) for the server, user name and password.",
                "Cannot connect to the database",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

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
        shell.SetStatus($"Connected to MySQL — {repository.Residents.Count} resident(s), " +
                        $"{repository.Requests.Count} request(s) loaded.");

        Application.Run(shell);
    }

    /// <summary>
    /// Reads the settings, creates the database and tables if they are missing,
    /// loads the data, and fills a brand-new database with sample data once.
    /// </summary>
    private static IBarangayRepository ConnectToDatabase(FeeSchedule feeSchedule)
    {
        var settings = DatabaseSettings.Load();

        DatabaseInitializer.EnsureCreated(settings.ConnectionString);

        var repository = new MySqlBarangayRepository(settings.ConnectionString, feeSchedule);

        if (settings.SeedSampleData && repository.Residents.Count == 0)
            SampleDataSeeder.Seed(repository);

        return repository;
    }
}
