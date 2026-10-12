// ---------------------------------------------------------------------------
//  AppLog.cs - a plain text log so the app can explain itself later.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Globalization;
using System.IO;

namespace BarangayDocumentSystem.Config
{
    /// <summary>
    /// Writes one line at a time into a text file under the logs folder.
    ///
    /// I added this because the first time we ran the system on another
    /// computer, the program closed with a message box and nothing else, and
    /// there was no way to find out what had happened. Now every startup, every
    /// database problem and every error that a clerk does not understand
    /// leaves a trace.
    ///
    /// Nothing secret goes in here: no passwords, no connection string with a
    /// password in it. I strip those before writing (see Scrub).
    /// </summary>
    public static class AppLog
    {
        private static readonly object Gate = new object();
        private static string _folder;

        public static string Folder
        {
            get
            {
                if (_folder != null) return _folder;

                string configured = AppConfig.LogFolder;
                string path = Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(AppConfig.ApplicationFolder, configured);

                try
                {
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                }
                catch (IOException)
                {
                    // If I cannot even create a folder I fall back to the
                    // program's own folder rather than losing the log entirely.
                    path = AppConfig.ApplicationFolder;
                }

                _folder = path;
                return _folder;
            }
        }

        public static void Info(string message)
        {
            Write("INFO ", message);
        }

        public static void Warn(string message)
        {
            Write("WARN ", message);
        }

        public static void Error(string message, Exception error)
        {
            string text = message;
            if (error != null)
            {
                text += " | " + error.GetType().Name + ": " + error.Message;
                if (error.InnerException != null)
                    text += " | inner: " + error.InnerException.Message;
            }
            Write("ERROR", text);
        }

        private static void Write(string level, string message)
        {
            try
            {
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                            + " [" + level + "] " + Scrub(message) + Environment.NewLine;

                lock (Gate)
                {
                    string file = Path.Combine(Folder, "barangay-" + DateTime.Today.ToString("yyyy-MM") + ".log");
                    File.AppendAllText(file, line);
                }
            }
            catch (Exception)
            {
                // A logger that throws is worse than no logger. I let this go.
            }
        }

        /// <summary>
        /// Removes anything that looks like a password from a line before it
        /// is written. A log file that leaks the database password is worse
        /// than no log file at all, and it is exactly the kind of thing that
        /// ends up pasted into a group chat while debugging.
        /// </summary>
        public static string Scrub(string message)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;

            string text = message;
            text = ReplaceAfter(text, "Password=", "***");
            text = ReplaceAfter(text, "password=", "***");
            text = ReplaceAfter(text, "Pwd=", "***");
            text = ReplaceAfter(text, "pwd=", "***");
            return text;
        }

        private static string ReplaceAfter(string text, string marker, string replacement)
        {
            int start = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            while (start >= 0)
            {
                int valueStart = start + marker.Length;
                int valueEnd = text.IndexOf(';', valueStart);
                if (valueEnd < 0) valueEnd = text.Length;

                text = text.Substring(0, valueStart) + replacement + text.Substring(valueEnd);
                start = text.IndexOf(marker, valueStart + replacement.Length, StringComparison.OrdinalIgnoreCase);
            }
            return text;
        }
    }
}
