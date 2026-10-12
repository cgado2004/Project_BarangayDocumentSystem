// ---------------------------------------------------------------------------
//  UserAccount.cs - a staff account that can sign in.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;

namespace BarangayDocumentSystem.Models
{
    /// <summary>
    /// One staff account.
    ///
    /// Three things I want to point out, because they are about keeping the
    /// barangay's data safe:
    ///
    /// 1. I never store the password. I store a scramble of it (PBKDF2, see
    ///    PasswordHasher) with a random salt that is different for every
    ///    person, so two clerks who choose the same password still have
    ///    different rows in the table.
    ///
    /// 2. The account locks itself after a few wrong passwords. The count and
    ///    the lock length come from App.config, so the barangay can tighten
    ///    them without me rebuilding anything.
    ///
    /// 3. Accounts are switched off, never deleted, for the same reason
    ///    residents are: the activity log points at a username, and a row in
    ///    the log that points at nothing is worse than no log at all.
    /// </summary>
    public class UserAccount
    {
        public int UserId { get; internal set; }

        /// <summary>The name typed at the login screen.</summary>
        public string Username { get; set; }

        /// <summary>The person's real name, shown on the top bar and written
        /// on the activity log so a log entry says "Marites Santos", not "msantos123".</summary>
        public string FullName { get; set; }

        public UserRole Role { get; set; }

        /// <summary>Their job title at the barangay hall, purely for display.</summary>
        public string Position { get; set; }

        // ---- the password, scrambled ----
        public string PasswordHash { get; set; }

        /// <summary>Base64 of the random salt. Different for every account.</summary>
        public string PasswordSalt { get; set; }

        /// <summary>How many rounds the scramble used. I keep it per account
        /// so that if I raise the number later, the old accounts still sign in
        /// and get re-hashed quietly at the next password change.</summary>
        public int HashIterations { get; set; }

        public bool MustChangePassword { get; set; }

        // ---- state ----
        public bool IsActive { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime? LastLoginOn { get; set; }
        public string LastLoginMachine { get; set; }

        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }

        public UserAccount()
        {
            Username = string.Empty;
            FullName = string.Empty;
            Position = string.Empty;
            PasswordHash = string.Empty;
            PasswordSalt = string.Empty;
            LastLoginMachine = string.Empty;
            CreatedBy = string.Empty;
            UpdatedBy = string.Empty;
            CreatedOn = DateTime.Now;
            IsActive = true;
        }

        /// <summary>
        /// Is the account locked right now?
        ///
        /// A lock with a time in the past is over, so I clear it here and let
        /// the person try again. That saves the admin a phone call.
        /// </summary>
        public bool IsLockedOut(DateTime now)
        {
            if (LockedUntil.HasValue && LockedUntil.Value > now) return true;
            if (LockedUntil.HasValue && LockedUntil.Value <= now) LockedUntil = null;
            return false;
        }

        public bool CanSignIn(DateTime now)
        {
            return IsActive && !IsLockedOut(now);
        }

        /// <summary>Count a wrong password. On the last allowed attempt the
        /// account locks for the configured number of minutes.</summary>
        public void RegisterFailedAttempt(DateTime now, int maxAttempts, int lockMinutes)
        {
            FailedAttempts++;
            if (FailedAttempts >= maxAttempts)
            {
                LockedUntil = now.AddMinutes(lockMinutes);
                FailedAttempts = 0;
            }
        }

        public void RegisterSuccessfulLogin(DateTime now, string machineName)
        {
            FailedAttempts = 0;
            LockedUntil = null;
            LastLoginOn = now;
            LastLoginMachine = machineName ?? string.Empty;
        }

        /// <summary>Replaces the stored password with a freshly scrambled one.</summary>
        public void SetPassword(string hash, string salt, int iterations, bool mustChange)
        {
            PasswordHash = hash;
            PasswordSalt = salt;
            HashIterations = iterations;
            MustChangePassword = mustChange;
        }

        /// <summary>True while the person is still using the password the
        /// admin gave them, which the app forces them to change.</summary>
        public bool NeedsPasswordChange()
        {
            return MustChangePassword || string.IsNullOrEmpty(PasswordHash);
        }

        public void Activate(string changedBy, DateTime when)
        {
            IsActive = true;
            UpdatedBy = changedBy;
            UpdatedOn = when;
        }

        /// <summary>Switch the account off. This is what I do instead of
        /// deleting a resigned clerk's account.</summary>
        public void Deactivate(string changedBy, DateTime when)
        {
            IsActive = false;
            UpdatedBy = changedBy;
            UpdatedOn = when;
        }

        public string GetRoleText()
        {
            return EnumText.Of(Role);
        }

        public override string ToString()
        {
            return FullName + " (" + Username + ")";
        }
    }
}
