using System;
using System.Configuration;
using MySql.Data.MySqlClient;
using BarangayDocumentSystem.Database;
using System.Windows.Forms;
using BarangayDocumentSystem.Configuration;
using BarangayDocumentSystem.Data;
using BarangayDocumentSystem.Documents;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Helpers;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            ModernTheme.Resolve();   // navy theme: resolve the font stack before any form is built
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, args) => UiFeedback.Unexpected(null, args.Exception);
            try
            {
                var settings = AppSettings.Load();
                var database = DatabaseSettings.Load();
                var repository = new MySqlBarangayRepository(database.ConnectionString);
                repository.Initialize();   // creates barangay_db + tables + fee schedule first
                var feeSchedule = new FeeSchedule(database.ConnectionString);
                var renderer = new DocumentRenderer(settings.Profile, new IDocumentTemplate[]
                {
                    new ClearanceTemplate(), new ResidencyTemplate(), new IndigencyTemplate(),
                    new BusinessClearanceTemplate(), new BarangayIdTemplate(),
                    new JobseekerTemplate(), new GoodMoralTemplate()
                });
                var residents = new ResidentService(repository);
                var requests = new RequestService(repository, feeSchedule, renderer);
                var reporting = new ReportingService(repository);
                Application.Run(new MainForm(residents, requests, renderer, settings, reporting));
            }
            catch (ConfigurationErrorsException error)
            {
                MessageBox.Show(error.Message, "Check App.config", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (MySqlException error)
            {
                ErrorLogger.Write(error);
                MessageBox.Show("The database could not be opened. Start MySQL (XAMPP) and open phpMyAdmin once to confirm it is running, " +
                    "then restart the app. If MySQL is running, check the BarangayDatabase connection in App.config " +
                    "and the error log in your local application data folder.",
                    "Database connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception error)
            {
                ErrorLogger.Write(error);
                MessageBox.Show("The application could not start. Check App.config and the local application error log.",
                    "Startup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
