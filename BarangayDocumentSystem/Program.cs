// =====================================================================
//  PART:    Program - the composition root
//  ORIGIN:  leader_draft - Clint Wood Gado (profile, fees and store from
//           App.config; every screen gets its dependencies handed in)
//           Fdraft - Frent Dhieniel Raborar (the start-up order for MySQL:
//           settings, create the database, open the store, seed if empty,
//           and explain-then-exit when the server cannot be reached)
//  EDITS:   Clint Wood Gado - merged the two; AppSettings moved to its own
//           file; the "MySQL is not wired up yet" fallback is gone because
//           MySQL is wired up now
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Windows.Forms;
using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.Database;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem;

/// <summary>
/// Where my program starts, and the ONLY place that decides which concrete
/// classes get used.
///
/// Everything below this point is handed what it needs through its
/// constructor, so no screen ever creates its own repository. That is the
/// dependency-inversion principle in one sentence, and it is what let me
/// drop Frent's MySQL store in without touching a single form: the views
/// were already written against IBarangayRepository.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

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

        // The Citizen's Charter rates and the two statutory rules. The
        // numbers live in App.config; the LAW behind each one lives in
        // FeeSchedule and docs/07.
        var fees = new FeeSchedule(
            AppSettings.Money("Fee.Clearance.Local", 100m),
            AppSettings.Money("Fee.Clearance.Abroad", 200m),
            AppSettings.Money("Fee.Certification", 100m),
            AppSettings.Money("Fee.BusinessClearance.Standard", 200m),
            AppSettings.Money("Fee.LuponFiling", 150m),
            AppSettings.Money("Fee.Facility.Hourly", 200m),
            AppSettings.Money("Fee.CommunityTax.Base", 5m),
            AppSettings.Money("Fee.CommunityTax.PerThousand", 1m),
            AppSettings.Money("Fee.CommunityTax.Cap", 5000m),
            AppSettings.Count("Rule.JobseekerResidencyMonths", 6),
            AppSettings.Count("Rule.RA11032.SimpleWorkingDays", 3));

        IBarangayRepository repository;
        try
        {
            repository = CreateRepository(fees);
        }
        catch (RepositoryException ex)
        {
            // Frent's rule, kept on purpose: if the database cannot be
            // reached I say exactly why and stop. I do NOT quietly fall back
            // to sample data, because a clerk who then spends the morning
            // encoding residents into a store that forgets everything at
            // closing time would have every right to be angry with me.
            Dialog.Error(null,
                ex.Message + "\n\n" +
                "The program will close. Start MySQL (or fix the BarangayDb connection " +
                "string in BarangayDocumentSystem.exe.config) and open it again.\n\n" +
                "For a demonstration without a database, set Storage to \"Memory\" in " +
                "the same file.",
                "Cannot open the barangay database");
            return;
        }
        catch (ArgumentException ex)
        {
            // MySqlConnectionStringBuilder throws this for a malformed
            // connection string - a typo in App.config, in other words.
            Dialog.Error(null,
                "The BarangayDb connection string in BarangayDocumentSystem.exe.config " +
                "could not be read:\n\n" + ex.Message,
                "Cannot open the barangay database");
            return;
        }

        Application.Run(new MainShell(repository, fees));
    }

    /// <summary>
    /// I pick the store named in App.config and get it ready to use.
    ///
    /// MySQL is the default, and Frent's start-up order makes it painless on
    /// a fresh XAMPP: create the database and tables if they are missing,
    /// open the store, and if it is empty and the config allows, fill it
    /// with my sample residents so the first screen is never blank.
    ///
    /// "Memory" keeps the demo store for a machine with no MySQL at all,
    /// and for my RuleChecks harness. The status bar says which one is
    /// running, so nobody can mistake the demo for the real thing.
    /// </summary>
    private static IBarangayRepository CreateRepository(FeeSchedule fees)
    {
        if (!AppSettings.UseMySql())
            return new InMemoryBarangayRepository(fees);

        var settings = DatabaseSettings.Load();

        DatabaseInitializer.EnsureCreated(settings.ConnectionString);

        var repository = new MySqlBarangayRepository(settings.ConnectionString, fees);

        if (settings.SeedSampleData && repository.Residents.Count == 0)
            SampleData.Seed(repository);

        return repository;
    }
}
