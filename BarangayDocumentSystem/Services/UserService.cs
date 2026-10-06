// ---------------------------------------------------------------------------
//  UserService.cs - creating and switching off staff accounts.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;

namespace BarangayDocumentSystem.Services
{
    /// <summary>
    /// The administrator's side of the accounts.
    ///
    /// There is no "register" screen for staff, and that is the point: the
    /// barangay asked me to remove self-registration, and it is the safe choice
    /// anyway. Only an administrator creates accounts, and every creation,
    /// edit, password reset and switch-off is written into the activity log, so
    /// there is always an answer to "who gave this account access?".
    ///
    /// Two guards I put in after thinking about how a small office really
    /// works:
    ///
    ///  * the last active administrator cannot be switched off, and
    ///  * nobody can switch off their own account.
    ///
    /// Both of those have the same failure in mind: a barangay with one
    /// computer and one administrator who clicks the wrong row and locks the
    /// whole office out of its own records.
    /// </summary>
    public class UserService
    {
        private readonly IBarangayRepository _repository;
        private readonly ActivityLogService _log;
        private readonly PasswordHasher _hasher;
        private readonly SessionManager _session;
        private readonly IClock _clock;

        public UserService(IBarangayRepository repository, ActivityLogService log,
                           SessionManager session, IClock clock)
        {
            if (repository == null) throw new ArgumentNullException("repository");

            _repository = repository;
            _log = log;
            _session = session;
            _hasher = new PasswordHasher();
            _clock = clock == null ? new SystemClock() : clock;
        }

        public IList<UserAccount> GetUsers()
        {
            return _repository.GetUsers();
        }

        // ==================================================================
        //  Creating
        // ==================================================================

        public OperationResult<UserAccount> CreateUser(string username, string fullName, UserRole role,
                                                      string position, string password, string confirmation,
                                                      bool mustChangePassword)
        {
            if (!Allowed(Permission.ManageUsers, out string refusal)) return OperationResult<UserAccount>.Fail(refusal);

            var problems = InputValidator.ValidateUser(username, fullName, role.ToString(), true, password, confirmation);
            if (problems.Count > 0)
                return OperationResult<UserAccount>.Fail(string.Join(Environment.NewLine, ToArray(problems)));

            if (_repository.UsernameExists(username.Trim(), 0))
                return OperationResult<UserAccount>.Fail("The user name \"" + username.Trim() + "\" is already taken.");

            UserAccount user = new UserAccount();
            user.Username = username.Trim();
            user.FullName = fullName.Trim();
            user.Role = role;
            user.Position = position == null ? string.Empty : position.Trim();
            user.IsActive = true;
            user.CreatedBy = _session.Username;
            user.CreatedOn = _clock.Now();
            user.MustChangePassword = mustChangePassword;

            string salt;
            string hash = _hasher.Hash(password, out salt);
            user.SetPassword(hash, salt, _hasher.Iterations, mustChangePassword);

            _repository.InsertUser(user);

            _log.Record(ActivityModule.Users, "Created account", "User account", user.Username,
                "Created the account of " + user.FullName + " as " + EnumText.Of(role)
                + (mustChangePassword ? ", and the password has to be changed at the first sign-in." : "."));

            return OperationResult<UserAccount>.Ok(user, "The account of " + user.FullName + " is ready.");
        }

        // ==================================================================
        //  Editing
        // ==================================================================

        public OperationResult UpdateUser(UserAccount user, string fullName, UserRole role, string position)
        {
            if (!Allowed(Permission.ManageUsers, out string refusal)) return OperationResult.Fail(refusal);
            if (user == null) return OperationResult.Fail("Please choose an account first.");

            if (string.IsNullOrWhiteSpace(fullName))
                return OperationResult.Fail("The person's full name is required.");

            UserRole previousRole = user.Role;

            // Removing the last administrator would lock the barangay out of
            // the screens that fix everything else.
            if (previousRole == UserRole.Administrator && role != UserRole.Administrator
                && _repository.CountActiveAdministrators() <= 1 && user.IsActive)
            {
                return OperationResult.Fail(
                    "This is the only active administrator. Please make another administrator first, "
                    + "so the barangay cannot be locked out of its own records.");
            }

            user.FullName = fullName.Trim();
            user.Role = role;
            user.Position = position == null ? string.Empty : position.Trim();
            user.UpdatedBy = _session.Username;
            user.UpdatedOn = _clock.Now();

            _repository.UpdateUser(user);

            string detail = "Updated the account of " + user.FullName + ".";
            if (previousRole != role)
                detail += " The role changed from " + EnumText.Of(previousRole) + " to " + EnumText.Of(role) + ".";

            _log.Record(ActivityModule.Users, "Edited account", "User account", user.Username, detail);

            return OperationResult.Ok("The account is updated.");
        }

        /// <summary>Switches an account on or off. This is what happens
        /// instead of deleting it: the activity log keeps pointing at a real
        /// user name, which is the whole value of the log.</summary>
        public OperationResult SetActive(UserAccount user, bool active)
        {
            if (!Allowed(Permission.ManageUsers, out string refusal)) return OperationResult.Fail(refusal);
            if (user == null) return OperationResult.Fail("Please choose an account first.");

            if (!active && _session.User != null && user.UserId == _session.User.UserId)
                return OperationResult.Fail("You cannot switch off the account you are signed in with.");

            if (!active && user.Role == UserRole.Administrator
                && _repository.CountActiveAdministrators() <= 1)
            {
                return OperationResult.Fail(
                    "This is the only active administrator, so switching it off would leave nobody able "
                    + "to manage the system.");
            }

            if (active) user.Activate(_session.Username, _clock.Now());
            else user.Deactivate(_session.Username, _clock.Now());

            _repository.UpdateUser(user);

            _log.Record(ActivityModule.Users, active ? "Switched account on" : "Switched account off",
                "User account", user.Username,
                (active ? "Switched the account back on: " : "Switched the account off: ") + user.FullName);

            return OperationResult.Ok(active
                ? user.FullName + " can sign in again."
                : user.FullName + " can no longer sign in. The record stays in the log.");
        }

        // ==================================================================
        //  Passwords
        // ==================================================================

        /// <summary>
        /// Gives an account a new password, for the call that always happens in
        /// a barangay office: somebody forgot theirs.
        ///
        /// The account is always flagged so that the password has to be changed
        /// at the next sign-in. That way the administrator knows the password
        /// for a few minutes at most, and never keeps it.
        /// </summary>
        public OperationResult ResetPassword(UserAccount user, string newPassword, string confirmation)
        {
            if (!Allowed(Permission.ManageUsers, out string refusal)) return OperationResult.Fail(refusal);
            if (user == null) return OperationResult.Fail("Please choose an account first.");

            var problems = InputValidator.ValidatePassword(newPassword, confirmation);
            if (problems.Count > 0) return OperationResult.Fail(string.Join(Environment.NewLine, ToArray(problems)));

            string salt;
            string hash = _hasher.Hash(newPassword, out salt);

            _repository.UpdatePassword(user.UserId, hash, salt, _hasher.Iterations, true, _session.Username, _clock.Now());
            user.SetPassword(hash, salt, _hasher.Iterations, true);

            _log.Record(ActivityModule.Users, "Reset password", "User account", user.Username,
                "The administrator gave " + user.FullName + " a new password, which has to be changed "
                + "at the next sign-in.");

            return OperationResult.Ok("The new password is set. " + user.FullName
                + " has to change it at the next sign-in.");
        }

        /// <summary>Clears a lock-out, for the resident clerk who typed their
        /// password wrong five times on a Monday morning.</summary>
        public OperationResult Unlock(UserAccount user)
        {
            if (!Allowed(Permission.ManageUsers, out string refusal)) return OperationResult.Fail(refusal);
            if (user == null) return OperationResult.Fail("Please choose an account first.");

            user.FailedAttempts = 0;
            user.LockedUntil = null;
            _repository.UpdateLoginState(user);

            _log.Record(ActivityModule.Users, "Unlocked account", "User account", user.Username,
                "Cleared the lock-out on " + user.FullName + "'s account.");

            return OperationResult.Ok(user.FullName + " can sign in again.");
        }

        // ==================================================================
        //  Internals
        // ==================================================================

        /// <summary>Every method asks the same question first. The screens
        /// also hide what a person may not do, but this is the check that
        /// actually decides.</summary>
        private bool Allowed(Permission permission, out string refusal)
        {
            refusal = string.Empty;

            if (_session == null || !_session.IsSignedIn)
            {
                refusal = "Please sign in first.";
                return false;
            }

            if (_session.Has(permission)) return true;

            refusal = _session.RefusalFor(permission);
            _log.Record(ActivityModule.Users, "Refused", "User account", string.Empty,
                _session.DescribeForLog(permission));
            return false;
        }

        private static string[] ToArray(IList<string> messages)
        {
            string[] array = new string[messages.Count];
            messages.CopyTo(array, 0);
            return array;
        }
    }
}
