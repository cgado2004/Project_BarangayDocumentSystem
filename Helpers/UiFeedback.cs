using System;
using System.Windows.Forms;

namespace BarangayDocumentSystem.Helpers
{
    public static class UiFeedback
    {
        public static void Run(IWin32Window owner, Action action)
        {
            try { action(); }
            catch (ArgumentException error) { Warn(owner, error.Message); }
            catch (InvalidOperationException error) { Warn(owner, error.Message); }
            catch (Exception error) { Unexpected(owner, error); }
        }

        public static void Warn(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, "Please check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static bool Confirm(IWin32Window owner, string message)
        {
            return MessageBox.Show(owner, message, "Confirm action", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        public static void Unexpected(IWin32Window owner, Exception error)
        {
            ErrorLogger.Write(error);
            MessageBox.Show(owner, "The action could not be completed. Please try again. " +
                "If it continues, restart the app and check the error log in your local application data folder.",
                "Unexpected error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
