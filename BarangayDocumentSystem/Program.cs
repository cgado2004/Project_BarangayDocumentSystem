#nullable enable
using System.Windows.Forms;
using System;
using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.BusinessRules.DocumentTemplates;
using BarangayDocumentSystem.Database;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Views;

namespace BarangayDocumentSystem;

internal static class Program
{







    [STAThread]
    static void Main()
    {





        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);


        var feeSchedule = new FeeSchedule();
        var profile = BarangayProfile.MagugpoPoblacion;



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




        IBarangayRepository repository;
        string connectionString;
        try
        {
            repository = ConnectToDatabase(feeSchedule, out connectionString);
        }
        catch (Exception ex) when (ex is RepositoryException or ArgumentException)
        {
            MessageBox.Show(
                "The system is unavailable. Contact the system administrator and try again later.",
                "Cannot start the system",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }




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

            using var shell = BuildShell(repository, users, renderer, feeSchedule, profile, user);
            Application.Run(shell);

            if (!shell.LoggedOut)
                return;
        }
    }

    private static MainShell BuildShell(
        IBarangayRepository repository, IUserRepository users, DocumentRenderer renderer,
        FeeSchedule feeSchedule, BarangayProfile profile, UserAccount user)
    {
        var shell = new MainShell();

        string staffName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;
        repository.CurrentUser = staffName;

        var dashboard = new DashboardView(repository);
        var residents = new ResidentsView(repository);
        var requests  = new RequestsView(repository, renderer, feeSchedule, staffName);
        var records   = new RecordsView(repository, renderer);
        var receipts  = new OfficialReceiptsView(repository);

        shell.AddView("dashboard", "Dashboard", "▦", dashboard);
        shell.AddView("residents", "Residents", "●", residents);
        shell.AddView("requests",  "Requests",  "▤", requests);
        shell.AddView("records",   "Records",   "▧", records);
        shell.AddView("receipts",  "Receipts",  "₱", receipts);

        if (user.Role == UserRole.Admin)
        {
            var archive   = new ArchiveView(repository);
            var activity  = new ActivityLogView(repository);
            shell.AddView("archive",   "Archive",   "▣", archive);
            shell.AddView("activity",  "Activity",  "◷", activity);
        }

        var settings = new SettingsView(repository, users, profile, user);
        settings.LogoutRequested += (_, _) => shell.LogOut();
        shell.AddView("settings", "Settings", "⚙", settings);

        shell.Start("dashboard");

        return shell;
    }





    private static IBarangayRepository ConnectToDatabase(FeeSchedule feeSchedule, out string connectionString)
    {
        var settings = DatabaseSettings.Load();
        connectionString = settings.ConnectionString;

        DatabaseInitializer.EnsureCreated(settings.ConnectionString);
        DatabaseInitializer.EnsureDefaultAdmin(settings.ConnectionString);

        var repository = new MySqlBarangayRepository(settings.ConnectionString, feeSchedule);

        if (settings.SeedSampleData && repository.Residents.Count == 0)
            SampleDataSeeder.Seed(repository);

        return repository;
    }
}
