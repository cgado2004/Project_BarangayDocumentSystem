// ---------------------------------------------------------------------------
//  Program.cs - where the program starts, and the only place that decides
//  which concrete classes get used.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Configuration;
using System.Windows.Forms;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Data;
using BarangayDocumentSystem.Database;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.UI;
using BarangayDocumentSystem.UI.Forms;

namespace BarangayDocumentSystem
{
    /// <summary>
    /// The start of everything.
    ///
    /// This file is the composition root, which is a grand name for a simple
    /// idea: the one place where the program decides WHAT it is made of. Every
    /// screen is handed the store and the services it needs through its
    /// constructor and never builds its own - which is why swapping MySQL for
    /// SQL Server, or the database for the in-memory store, changed this file
    /// and nothing else. That is the object-oriented design the barangay asked
    /// for, doing actual work rather than being a diagram in a document.
    ///
    /// The order here matters and I keep it the same every time:
    ///
    ///   1. read the settings file, so the barangay's own details and fees are
    ///      in place before anything is drawn;
    ///   2. work out which fonts this computer really has;
    ///   3. prepare the database (create it, run the scripts, upgrade, seed the
    ///      first administrator and, if asked, the sample data);
    ///   4. open the sign-in window, and only after that open the main window.
    ///
    /// Nothing is written by this file. It only wires things together.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ---- 1. the settings file ----
            AppConfig.Load();

            // ---- 2. the lettering ----
            AppTheme.Resolve();

            // A crash nobody saw is a crash that gets reported as "the computer
            // is broken", so I write the details to the log and say so on screen.
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender,
                UnhandledExceptionEventArgs e)
            {
                Exception error = e.ExceptionObject as Exception;
                AppLog.Error("Something went wrong and the program had to stop.", error);

                MessageBox.Show(
                    "The program had to stop because of a problem it could not get past."
                    + Environment.NewLine + Environment.NewLine
                    + (error == null ? string.Empty : error.Message) + Environment.NewLine + Environment.NewLine
                    + "The details were written to the log folder next to the program. Please give that file "
                    + "to whoever looks after the system.",
                    "Barangay Document System", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            // ---- 3. the store ----
            IBarangayRepository repository = null;
            string preparation = string.Empty;

            try
            {
                repository = BuildRepository(out preparation);
            }
            catch (RepositoryException error)
            {
                Report(repository, error.Message);
                return;
            }
            catch (ConfigurationErrorsException error)
            {
                Report(repository, "The settings file could not be read:" + Environment.NewLine
                    + error.Message);
                return;
            }
            catch (Exception error)
            {
                AppLog.Error("The store could not be opened.", error);
                Report(repository, "The barangay database could not be opened." + Environment.NewLine
                    + Environment.NewLine + error.Message);
                return;
            }

            if (repository == null) return;

            // ---- 4. signing in ----
            SessionManager session = new SessionManager();
            SessionManager.Current = session;

            IClock clock = new SystemClock();
            ActivityLogService activity = new ActivityLogService(repository, session, clock);
            AuthenticationService authentication = new AuthenticationService(repository, activity, clock);

            try
            {
                using (LoginForm login = new LoginForm(authentication, repository))
                {
                    login.ShowDialog();

                    if (login.SignedInUser == null)
                    {
                        AppTheme.Release();
                        return;
                    }

                    session.SignIn(login.SignedInUser);
                }

                activity.Record(ActivityModule.Security, "Signed in", "Session", session.Username,
                    "Signed in on " + Environment.MachineName + "." + preparation);

                using (ShellForm shell = new ShellForm(repository, session, clock, authentication))
                {
                    Application.Run(shell);
                }
            }
            catch (RepositoryException error)
            {
                // The database was there at the start of the morning and is not
                // there now. Saying so plainly beats a stack trace on a counter.
                Report(repository, error.Message);
            }
            catch (Exception error)
            {
                AppLog.Error("The main window stopped.", error);
                Report(repository, "The program stopped because of a problem it could not get past."
                    + Environment.NewLine + Environment.NewLine + error.Message);
            }
            finally
            {
                AppTheme.Release();
            }
        }

        // ==================================================================
        //  Choosing and preparing the store
        // ==================================================================

        /// <summary>
        /// Picks the store named in the settings file and gets it ready.
        ///
        /// MySQL is the default because that is what the barangay hall runs,
        /// and SQL Server is there for an office that already has it - the
        /// screens cannot tell the difference, because both sit behind the same
        /// contract. "Memory" is the third choice and it exists for two honest
        /// reasons: a machine with no database server at all, and my rule
        /// checks, which run the same services against a store that forgets
        /// everything when the window closes.
        /// </summary>
        private static IBarangayRepository BuildRepository(out string preparation)
        {
            preparation = string.Empty;

            if (AppConfig.IsMemory)
            {
                preparation = " Using the in-memory store: nothing is saved when the program closes.";
                AppLog.Warn(preparation.Trim());

                return new InMemoryBarangayRepository();
            }

            // The provider is chosen by AppConfig, which reads the same
            // settings file the person can open and edit. DBContext follows
            // that choice; this line is why there is only one place to change
            // it, and the rule checks use the same path with a different value.
            //
            // The context is deliberately left open: the repository keeps it
            // for the whole working day, because every screen asks it for
            // something and opening a connection per screen is slower and
            // harder to keep track of.
            DBContext context = DBContext.FromConfiguration();

            DatabaseInitializer initializer = new DatabaseInitializer(context);
            preparation = " " + initializer.Run();

            return new SqlBarangayRepository(context);
        }

        private static void Report(IBarangayRepository repository, string message)
        {
            string storage = repository == null ? AppConfig.StorageProvider : repository.Describe();

            MessageBox.Show(
                message + Environment.NewLine + Environment.NewLine
                + "Storage in the settings file: " + storage + Environment.NewLine
                + "Settings file: " + AppConfig.SettingsFile + Environment.NewLine
                + "Log folder: " + AppConfig.LogFolder + Environment.NewLine + Environment.NewLine
                + "Fix the database (or the connection string) and open the program again."
                + Environment.NewLine
                + "To run without a database for a demonstration, set Storage to \"Memory\" in the same file.",
                "Barangay Document System", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
