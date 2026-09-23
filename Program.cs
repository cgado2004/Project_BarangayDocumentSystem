using System;
using System.Configuration;
using System.Data.SqlClient;
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
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, args) => UiFeedback.Unexpected(null, args.Exception);
            try
            {
                var settings = AppSettings.Load();
                var repository = new SqlBarangayRepository(settings.ConnectionString);
                var feeSchedule = new FeeSchedule();
                var renderer = new DocumentRenderer(settings.Profile, new IDocumentTemplate[]
                {
                    new ClearanceTemplate(), new ResidencyTemplate(), new IndigencyTemplate(),
                    new BusinessClearanceTemplate(), new BarangayIdTemplate(),
                    new JobseekerTemplate(), new GoodMoralTemplate()
                });
                var residents = new ResidentService(repository);
                var requests = new RequestService(repository, feeSchedule, renderer);
                repository.Initialize(settings.LoadSampleData, () => SampleData.Load(residents, requests));
                var reporting = new ReportingService(repository);
                Application.Run(new MainForm(residents, requests, renderer, settings, reporting));
            }
            catch (ConfigurationErrorsException error)
            {
                MessageBox.Show(error.Message, "Check App.config", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (SqlException error)
            {
                ErrorLogger.Write(error);
                MessageBox.Show("The database could not be opened. Install SQL Server Express LocalDB using Visual Studio Installer, " +
                    "then restart the app. If LocalDB is already installed, check the BarangayDatabase connection in App.config " +
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
