using System;
using System.Configuration;
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
                IBarangayRepository repository = new InMemoryBarangayRepository();
                var feeSchedule = new FeeSchedule();
                var renderer = new DocumentRenderer(settings.Profile, new IDocumentTemplate[]
                {
                    new ClearanceTemplate(), new ResidencyTemplate(), new IndigencyTemplate(),
                    new BusinessClearanceTemplate(), new BarangayIdTemplate(),
                    new JobseekerTemplate(), new GoodMoralTemplate()
                });
                var residents = new ResidentService(repository);
                var requests = new RequestService(repository, feeSchedule, renderer);
                if (settings.LoadSampleData) SampleData.Load(residents, requests);
                Application.Run(new MainForm(residents, requests, renderer, settings));
            }
            catch (ConfigurationErrorsException error)
            {
                MessageBox.Show(error.Message, "Check App.config", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
