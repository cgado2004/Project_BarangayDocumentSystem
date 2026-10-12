// ---------------------------------------------------------------------------
//  Dialog.cs - the sentences the program says to whoever is using it.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace BarangayDocumentSystem.UI
{
    /// <summary>
    /// Every message the program shows, in one place.
    ///
    /// I keep them here for two reasons. The first is that the wording of a
    /// refusal should be written once - if the clerk is told "that fee is not
    /// settled yet, write the receipt first" on one screen, the same sentence
    /// should come up on the next. The second is that a message box is the
    /// only part of a program some people ever read, and it should sound like a
    /// person wrote it rather than like a machine shouted it.
    ///
    /// So: a warning says what is wrong AND what to do next, in that order, and
    /// never just "Invalid input".
    /// </summary>
    public static class Dialog
    {
        public static void Info(IWin32Window owner, string message, string title)
        {
            MessageBox.Show(owner, message, Text(title, "Done"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void Warn(IWin32Window owner, string message, string title)
        {
            MessageBox.Show(owner, message, Text(title, "Please check this"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>The message for something the program refused to do, in
        /// the words the service used. Not an error - a rule.</summary>
        public static void Refused(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, "Not allowed",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void Error(IWin32Window owner, string message, string title)
        {
            MessageBox.Show(owner, message, Text(title, "Something went wrong"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Reports a caught exception without showing anybody a stack trace.
        ///
        /// The clerk gets the one sentence that matters and the information
        /// that the details are in the log file; whoever supports the system
        /// can open the log and find the whole thing. Both of them get what
        /// they need, and neither gets the other's half.
        /// </summary>
        public static void FromException(IWin32Window owner, string whatWasHappening, Exception error)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine(whatWasHappening);
            text.AppendLine();

            if (error is Interfaces.RepositoryException)
                text.AppendLine(error.Message);
            else
                text.AppendLine("The program could not finish that. " + Shorten(error.Message));

            text.AppendLine();
            text.AppendLine("The technical details were written to the log folder next to the program, "
                          + "in a file named for today's date. Nothing you typed has been lost unless "
                          + "the message above says so.");

            Error(owner, text.ToString(), "I could not finish that");
        }

        private static string Shorten(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return string.Empty;
            return message.Length > 300 ? message.Substring(0, 297) + "..." : message;
        }

        /// <summary>
        /// A yes-or-no question where the button said to be safe is the one
        /// already selected: "No" for anything that changes a record.
        /// </summary>
        public static bool Confirm(IWin32Window owner, string message, string title, string okButtonText)
        {
            return MessageBox.Show(owner, message, Text(title, "Please confirm"),
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
                == DialogResult.OK;
        }

        public static bool Confirm(IWin32Window owner, string message, string title)
        {
            return Confirm(owner, message, title, null);
        }

        /// <summary>
        /// The confirmation used before anything that writes to the database.
        ///
        /// I always show what is about to happen in the clerk's own words - the
        /// person, the document, the amount, the status - rather than a bare
        /// "Save?". That is the confirmation step the barangay asked for, and
        /// it is the last chance to catch a wrong name before it is printed on
        /// a signed paper.
        /// </summary>
        public static bool ConfirmChange(IWin32Window owner, string whatWillHappen)
        {
            string text = whatWillHappen + Environment.NewLine + Environment.NewLine
                        + "Do you want to continue?";

            return Confirm(owner, text, "Confirm", null);
        }

        /// <summary>
        /// Turns a list of problems into one message. The service hands back
        /// sentences; this puts them in a list with a dash in front of each, so
        /// the clerk sees all of them at once instead of fixing one and meeting
        /// the next.
        /// </summary>
        public static string Problems(IList<string> problems)
        {
            if (problems == null || problems.Count == 0) return string.Empty;

            StringBuilder text = new StringBuilder();
            foreach (string problem in problems) text.AppendLine("- " + problem);

            return text.ToString().TrimEnd();
        }

        private static string Text(string given, string fallback)
        {
            return string.IsNullOrWhiteSpace(given) ? fallback : given;
        }
    }
}
