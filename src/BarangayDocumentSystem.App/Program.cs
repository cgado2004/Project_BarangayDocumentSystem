using System;
using System.Windows.Forms;
using BarangayDocumentSystem.Core.Data;
using BarangayDocumentSystem.App.Theme;
using BarangayDocumentSystem.Core.Rules;

namespace BarangayDocumentSystem.App;

/// <summary>
/// Where my program starts, and the only place that decides which concrete
/// classes get used.
///
/// Everything below this point is handed what it needs through its
/// constructor, so no screen ever creates its own repository. That is what
/// lets me swap the storage without touching a single form.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // I work out which fonts this machine actually has before I create a
        // single control, so every form is built with the right family from
        // the start rather than being restyled afterwards.
        AppTheme.Resolve();

        // I read App.config first, so the barangay details and the fees on
        // every printed document come from the file rather than from numbers
        // I hard-coded months ago.
        BarangayProfile.Current = new BarangayProfile
        {
            BarangayName   = AppSettings.Text("Barangay.Name", "Barangay Magugpo Poblacion"),
            CityName       = AppSettings.Text("Barangay.City", "City of Tagum"),
            ProvinceName   = AppSettings.Text("Barangay.Province", "Davao del Norte"),
            PunongBarangay = AppSettings.Text("Barangay.PunongBarangay", "HON. EUGENIA SOLIS HINGPIT, MD"),
            OfficeHours    = AppSettings.Text("Barangay.OfficeHours", "Monday to Friday, 8:00 AM - 5:00 PM")
        };

        var fees = new FeeSchedule(
            AppSettings.Money("Fee.Clearance.Local", 100m),
            AppSettings.Money("Fee.Clearance.Abroad", 200m),
            AppSettings.Money("Fee.Certification", 100m),
            AppSettings.Money("Fee.BusinessClearance", 200m),
            AppSettings.Count("Rule.JobseekerResidencyMonths", 6));

        // This is the one decision I make that moves the whole system onto a
        // real database. Every screen only ever sees IBarangayRepository, so
        // nothing else in my program has to change.
        IBarangayRepository repository = CreateRepository(fees);

        Application.Run(new Form1(repository, fees));
    }

    /// <summary>
    /// I pick the store named in App.config.
    ///
    /// If MySQL is asked for but cannot be reached, I do NOT let the program
    /// die on a raw driver exception. I explain what went wrong and fall back
    /// to the in-memory data, because a group-mate who has not set up XAMPP
    /// yet should still be able to open the app and see it work.
    /// </summary>
    private static IBarangayRepository CreateRepository(FeeSchedule fees)
    {
        if (!AppSettings.UseMySql())
            return new InMemoryBarangayRepository(fees);

        string connection = AppSettings.ConnectionString();

        if (string.IsNullOrWhiteSpace(connection))
        {
            Warn("App.config asks for MySQL storage but the BarangayDb "
               + "connection string is missing.\n\n"
               + "I am starting with the built-in sample data instead.");
            return new InMemoryBarangayRepository(fees);
        }

        // NOTE FOR MY GROUP-MATES
        // The MySQL repository class is not in this version yet. The database
        // scripts in db/ are complete and ready to run, so when the repository
        // is added the only change needed here is the line below:
        //
        //     return new MySqlBarangayRepository(connection, fees);
        //
        // Until then, asking for MySQL gives the sample data and this warning,
        // which is honest rather than pretending the database is wired up.
        Warn("MySQL storage is selected in App.config, but the MySQL "
           + "repository is not included in this build yet.\n\n"
           + "The scripts in the db folder are ready to run - see "
           + "docs/05-database-guide.md.\n\n"
           + "I am starting with the built-in sample data for now.");

        return new InMemoryBarangayRepository(fees);
    }

    private static void Warn(string message) =>
        MessageBox.Show(message, "Storage", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
