// ---------------------------------------------------------------------------
//  RepositoryException.cs - the one error type the storage layer throws.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;

namespace BarangayDocumentSystem.Interfaces
{
    /// <summary>
    /// What the storage layer throws when it cannot do its job.
    ///
    /// Why not let the raw database error escape? Because "MySqlException:
    /// Unable to connect to any of the specified MySQL hosts" means nothing to
    /// a barangay clerk, and a stack trace on screen looks like the system is
    /// broken. I catch the real error, keep it as the inner exception for
    /// whoever debugs this later, and put a sentence a person can act on at
    /// the front.
    /// </summary>
    public class RepositoryException : Exception
    {
        /// <summary>Which step failed - "loading residents", "saving the
        /// request". I put it in the message the clerk sees so they can tell
        /// the admin something specific.</summary>
        public string Operation { get; private set; }

        public RepositoryException(string operation, string message)
            : base(message)
        {
            Operation = operation;
        }

        public RepositoryException(string operation, string message, Exception inner)
            : base(message, inner)
        {
            Operation = operation;
        }

        /// <summary>
        /// Wraps a database error with a sentence a clerk can read.
        ///
        /// The three cases below are the ones that actually happen in the
        /// barangay hall: the server is not running, the password changed, and
        /// the database is gone or was never created.
        /// </summary>
        public static RepositoryException FromDatabase(string operation, Exception inner)
        {
            string text = inner == null ? string.Empty : inner.Message;

            string friendly;
            if (Contains(text, "Unable to connect") || Contains(text, "network-related")
                || Contains(text, "No such host") || Contains(text, "could not open")
                || Contains(text, "server was not found"))
            {
                friendly = "I cannot reach the database server. Please start MySQL (or SQL Server) "
                         + "and check the connection settings in App.config.";
            }
            else if (Contains(text, "Access denied") || Contains(text, "Login failed"))
            {
                friendly = "The database refused the user name or password in the connection settings.";
            }
            else if (Contains(text, "Unknown database") || Contains(text, "Cannot open database"))
            {
                friendly = "The database does not exist yet. It is created automatically the first "
                         + "time the program starts, but the connection settings have to point at a "
                         + "server this computer can reach.";
            }
            else if (Contains(text, "Duplicate entry") || Contains(text, "duplicate key")
                  || Contains(text, "UNIQUE KEY") || Contains(text, "Violation of UNIQUE KEY"))
            {
                friendly = "That record already exists - the same number was entered twice.";
            }
            else if (Contains(text, "foreign key") || Contains(text, "FOREIGN KEY"))
            {
                friendly = "That record is still used by another record, so it cannot be removed.";
            }
            else
            {
                friendly = "The database reported a problem while " + operation + ".";
            }

            return new RepositoryException(operation, friendly, inner);
        }

        private static bool Contains(string text, string piece)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.IndexOf(piece, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
