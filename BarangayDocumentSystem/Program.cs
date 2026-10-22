#nullable enable
using System.Windows.Forms;
using System;
using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.BusinessRules.DocumentTemplates;
using BarangayDocumentSystem.Database;
using BarangayDocumentSystem.Forms;
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
        string connectionString;
        try
        {
            repository = ConnectToDatabase(feeSchedule, out connectionString);
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

        // ---- Sign in, then the main window ----
        // Logging out closes the window and returns here, so the next person
        // signs in on a fresh set of views.
        var users = new MySqlUserRepository(connectionString);

        while (true)
        {
            UserAccount? user;
            using (var login = new LoginForm(users))
            {
                if (login.ShowDialog() != DialogResult.OK || login.User is null)
                    return;
                user = login.User;
            }

            using var shell = BuildShell(repository, renderer, feeSchedule, user);
            Application.Run(shell);

            if (!shell.LoggedOut)
                return;
        }
    }

    private static MainShell BuildShell(
        IBarangayRepository repository, DocumentRenderer renderer,
        FeeSchedule feeSchedule, UserAccount user)
    {
        var shell = new MainShell(string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName);

        var dashboard = new DashboardView(repository);
        var residents = new ResidentsView(repository);
        string staffName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;
        var requests  = new RequestsView(repository, renderer, feeSchedule, staffName);
        var receipts  = new OfficialReceiptsView(repository);
        var records   = new RecordsView(repository, renderer);
        var archive   = new ArchiveView(repository);

        shell.AddView("dashboard", "Dashboard", "▦", dashboard);
        shell.AddView("residents", "Residents", "●", residents);
        shell.AddView("requests",  "Requests",  "▤", requests);
        shell.AddView("receipts",  "Receipts",  "▥", receipts);
        shell.AddView("records",   "Records",   "▧", records);
        shell.AddView("archive",   "Archive",   "▣", archive);

        shell.Start("dashboard");

        var stats = repository.GetStatistics();
        shell.SetStatus($"Connected to MySQL — {stats.TotalResidents} active resident(s), " +
                        $"{repository.Requests.Count} request(s) loaded.");
        return shell;
    }

    /// <summary>
    /// Reads the settings, creates the database and tables if they are missing,
    /// loads the data, and fills a brand-new database with sample data once.
    /// </summary>
    private static IBarangayRepository ConnectToDatabase(FeeSchedule feeSchedule, out string connectionString)
    {
        var settings = DatabaseSettings.Load();
        connectionString = settings.ConnectionString;

        DatabaseInitializer.EnsureCreated(settings.ConnectionString);

        var repository = new MySqlBarangayRepository(settings.ConnectionString, feeSchedule);

        if (settings.SeedSampleData && repository.Residents.Count == 0)
            SampleDataSeeder.Seed(repository);

        return repository;
    }
}
