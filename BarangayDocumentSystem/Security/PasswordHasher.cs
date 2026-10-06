// ---------------------------------------------------------------------------
//  PasswordHasher.cs - how I keep passwords safe.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Security.Cryptography;

namespace BarangayDocumentSystem.Security
{
    /// <summary>
    /// Turns a password into something I can safely keep in the database.
    ///
    /// The idea, in plain words: I never store the password itself, and if two
    /// clerks pick the same password I still want the stored values to look
    /// different. So each account gets its own random salt (16 bytes), and the
    /// password is scrambled with PBKDF2-SHA256 for a hundred thousand rounds.
    /// Scrambling that many times is what makes a stolen table useless: trying
    /// a billion guesses would take years instead of seconds.
    ///
    /// The stored text looks like this:
    ///     PBKDF2$100000$<base64 salt>$<base64 hash>
    /// The algorithm and the round count are part of the text, so a future
    /// version can raise the rounds and still verify the older accounts.
    /// </summary>
    public class PasswordHasher
    {
        private const string Prefix = "PBKDF2";
        private const int DefaultIterations = 100000;
        private const int SaltBytes = 16;
        private const int HashBytes = 32;

        public int Iterations { get; set; }

        public PasswordHasher()
        {
            Iterations = DefaultIterations;
        }

        public PasswordHasher(int iterations)
        {
            Iterations = iterations < 1000 ? DefaultIterations : iterations;
        }

        /// <summary>Builds the stored text for a brand-new password.</summary>
        public string Hash(string password, out string saltBase64)
        {
            if (password == null) throw new ArgumentNullException("password");

            byte[] salt = new byte[SaltBytes];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            byte[] hash = Derive(password, salt, Iterations);
            saltBase64 = Convert.ToBase64String(salt);
            return Prefix + "$" + Iterations + "$" + saltBase64 + "$" + Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Checks a typed password against the stored text.
        ///
        /// I compare the two byte arrays with a loop that always runs to the
        /// end (a fixed-time comparison) instead of stopping at the first
        /// difference. It costs me nothing and it removes a timing clue that a
        /// determined attacker could otherwise measure.
        /// </summary>
        public bool Verify(string password, string storedText)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedText)) return false;

            string[] pieces = storedText.Split('$');
            if (pieces.Length != 4) return false;
            if (!string.Equals(pieces[0], Prefix, StringComparison.Ordinal)) return false;

            int iterations;
            if (!int.TryParse(pieces[1], out iterations) || iterations < 1000) return false;

            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(pieces[2]);
                expected = Convert.FromBase64String(pieces[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actual = Derive(password, salt, iterations);
            return FixedTimeEquals(actual, expected);
        }

        /// <summary>True when the stored text was made with fewer rounds than
        /// I now use, so I can quietly re-hash the password the next time the
        /// person changes it.</summary>
        public bool NeedsUpgrade(string storedText)
        {
            if (string.IsNullOrEmpty(storedText)) return true;

            string[] pieces = storedText.Split('$');
            if (pieces.Length != 4) return true;

            int iterations;
            if (!int.TryParse(pieces[1], out iterations)) return true;
            return iterations < Iterations;
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                // SHA-256 rather than the old default (SHA-1). The framework
                // has supported this since 4.7.2, which is what we target.
                pbkdf2.HashAlgorithm = new HMACSHA256();
                return pbkdf2.GetBytes(HashBytes);
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null) return false;
            if (left.Length != right.Length) return false;

            int difference = 0;
            for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }
    }
}
