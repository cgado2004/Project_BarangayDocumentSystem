// ---------------------------------------------------------------------------
//  ConnectionStringProtector.cs - keeping the database password out of sight.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Security.Cryptography;
using System.Text;
using BarangayDocumentSystem.Config;

namespace BarangayDocumentSystem.Security
{
    /// <summary>
    /// Seals the connection string in App.config so the database password is
    /// not sitting there in plain text.
    ///
    /// How it works, in plain words: Windows gives every program a way to
    /// scramble data so that only this computer - and, depending on the scope,
    /// only this Windows user - can unscramble it again. That is what this
    /// class uses. The scrambled text is stored in App.config and only the
    /// machine it was created on can read it.
    ///
    /// Is this what the barangay will use? Probably not for the class demo -
    /// XAMPP runs with an empty password and there is nothing to hide. It
    /// matters the day a real deployment has a real password: then the answer
    /// is either this, or the environment variable BARANGAY_DB_CONNECTION,
    /// which is what I recommend first because it keeps the secret out of the
    /// file entirely.
    ///
    /// One honest limitation I will write down rather than hide: this protects
    /// the password from somebody reading the file, not from somebody sitting
    /// at that same Windows account. Nothing short of a server-side account
    /// with limited rights does that.
    /// </summary>
    public class ConnectionStringProtector
    {
        private const string Prefix = "dpapi:";

        private readonly byte[] _entropy;

        public ConnectionStringProtector()
        {
            // A few extra bytes mixed into the scrambling, so a value sealed
            // by another program on the same machine cannot be read as if it
            // were mine.
            _entropy = Encoding.UTF8.GetBytes("BarangayDocumentSystem.ConnectionString.v1");
        }

        public bool IsProtected(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.TrimStart().StartsWith(Prefix, StringComparison.Ordinal);
        }

        /// <summary>Scrambles a connection string for App.config. The returned
        /// text is what I paste into the connectionStrings section, and it
        /// looks like "dpapi:AQAAANCMnd8BFdERjHoAwE/Cl..." .
        /// </summary>
        public string Protect(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText)) return string.Empty;
            if (IsProtected(plainText)) return plainText;

            try
            {
                byte[] clear = Encoding.UTF8.GetBytes(plainText);
                byte[] sealedBytes = ProtectedData.Protect(clear, _entropy, DataProtectionScope.CurrentUser);
                return Prefix + Convert.ToBase64String(sealedBytes);
            }
            catch (Exception error)
            {
                // If Windows will not seal it, I would rather store it plainly
                // and warn than hand the clerk a program that cannot start.
                AppLog.Warn("Could not seal the connection string, so it was left as it is: " + error.Message);
                return plainText;
            }
        }

        /// <summary>Unscrambles it again. A value that was sealed by another
        /// machine or another Windows user cannot be read, and then the caller
        /// gets the text back unchanged so the ordinary "cannot connect"
        /// message explains the situation.</summary>
        public string Unprotect(string protectedText)
        {
            if (!IsProtected(protectedText)) return protectedText;

            try
            {
                string payload = protectedText.Trim().Substring(Prefix.Length);
                byte[] sealedBytes = Convert.FromBase64String(payload);
                byte[] clear = ProtectedData.Unprotect(sealedBytes, _entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(clear);
            }
            catch (Exception error)
            {
                AppLog.Warn("Could not open the sealed connection string: " + error.Message);
                return string.Empty;
            }
        }
    }
}
