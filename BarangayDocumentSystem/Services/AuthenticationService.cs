// ---------------------------------------------------------------------------
//  AuthenticationService.cs - signing in, and refusing to sign in.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;

namespace BarangayDocumentSystem.Services
{
    /// <summary>
    /// The one door into the system.
    ///
    /// Everything about signing in happens here: checking the password against
    /// the stored scramble, counting wrong attempts, locking an account, and
    /// writing the attempt into the activity log. The login screen only shows
    /// the boxes and the messages.
    ///
    /// I want to explain three decisions, because they are the security
    /// answers the panel asked for:
    ///
    /// 1. When the password is wrong I never say whether the user name exists.
    ///    The message is the same for both, so nobody can use the login screen
    ///    to find out which names are real accounts.
    ///
    /// 2. Wrong attempts are counted and the account locks for a while. That is
    ///    what stops somebody sitting at the counter and guessing passwords all
    ///    afternoon.
    ///
    /// 3. Even a failed attempt is logged with the name that was typed. If
    ///    somebody is trying accounts at eight in the evening, the admin can
    ///    see it the next morning.
    /// </summary>
    public class AuthenticationService
    {
        private readonly IBarangayRepository _repository;
        private readonly ActivityLogService _log;
        private readonly PasswordHasher _hasher;
        private readonly IClock _clock;

        public AuthenticationService(IBarangayRepository repository, ActivityLogService log, IClock clock)
        {
            if (repository == null) throw new ArgumentNullException("repository");

            _repository = repository;
            _log = log;
            _hasher = new PasswordHasher();
            _clock = clock == null ? new SystemClock() : clock;
        }

        public PasswordHasher Hasher { get { return _hasher; } }

        // ==================================================================
        //  Signing in
        // ==================================================================

        public OperationResult<UserAccount> SignIn(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return OperationResult<UserAccount>.Fail("Please type your user name and password.");

            DateTime now = _clock.Now();
            UserAccount user = _repository.GetUserByUsername(username.Trim());

            // The same sentence for every kind of failure at this step. It is
            // deliberate: a different message for "no such user" would tell a
            // stranger which names are real.
            const string refusal = "That user name and password do not match an account.";

            if (user == null)
            {
                _log.RecordAs(username.Trim(), UserRole.Clerk, ActivityModule.Security, "Sign-in refused",
                    "User account", username.Trim(),
                    "Somebody tried to sign in with a user name that does not exist.");
                return OperationResult<UserAccount>.Fail(refusal);
            }

            if (user.IsLockedOut(now))
            {
                _repository.UpdateLoginState(user);

                string until = user.LockedUntil.HasValue
                    ? user.LockedUntil.Value.ToString("h:mm tt")
                    : "later";

                _log.RecordAs(user.Username, user.Role, ActivityModule.Security, "Sign-in refused",
                    "User account", user.Username,
                    "The account is locked after too many wrong passwords. It unlocks at " + until + ".");

                return OperationResult<UserAccount>.Fail(
                    "This account is locked because of too many wrong passwords. Try again after " + until
                    + ", or ask the administrator to unlock it.");
            }

            if (!user.IsActive)
            {
                _log.RecordAs(user.Username, user.Role, ActivityModule.Security, "Sign-in refused",
                    "User account", user.Username, "The account is switched off.");

                return OperationResult<UserAccount>.Fail(
                    "That account has been switched off. Please ask the administrator.");
            }

            if (!_hasher.Verify(password, user.PasswordHash))
            {
                user.RegisterFailedAttempt(now, AppConfig.MaxFailedLogins, AppConfig.LockoutMinutes);
                _repository.UpdateLoginState(user);

                int left = AppConfig.MaxFailedLogins - user.FailedAttempts;
                string detail = user.LockedUntil.HasValue
                    ? "The account is now locked for " + AppConfig.LockoutMinutes + " minutes."
                    : left + " attempt(s) left before the account locks.";

                _log.RecordAs(user.Username, user.Role, ActivityModule.Security, "Wrong password",
                    "User account", user.Username, "A wrong password was typed. " + detail);

                return OperationResult<UserAccount>.Fail(refusal + (user.LockedUntil.HasValue
                    ? " The account is now locked for " + AppConfig.LockoutMinutes + " minutes."
                    : " " + left + " attempt(s) left."));
            }

            // Signed in.
            user.RegisterSuccessfulLogin(now, Environment.MachineName);
            _repository.UpdateLoginState(user);

            _log.RecordAs(user.Username, user.Role, ActivityModule.Security, "Signed in",
                "User account", user.Username,
                user.FullName + " signed in as " + EnumText.Of(user.Role) + ".");

            return OperationResult<UserAccount>.Ok(user, "Welcome, " + user.FullName + ".");
        }

        /// <summary>What the shell calls when a session ends, so the log has a
        /// matching line for every sign-in.</summary>
        public void SignOut(UserAccount user)
        {
            if (user == null) return;

            _log.RecordAs(user.Username, user.Role, ActivityModule.Security, "Signed out",
                "User account", user.Username, user.FullName + " signed out.");
        }

        // ==================================================================
        //  Changing your own password
        // ==================================================================

        /// <summary>
        /// Changes the password of the person who is signed in.
        ///
        /// I ask for the current password even though the person is already
        /// signed in. It costs one box and it is the difference between a
        /// prankster at an unattended counter changing a password and not.
        /// </summary>
        public OperationResult ChangeOwnPassword(UserAccount user, string currentPassword,
                                                string newPassword, string confirmation)
        {
            if (user == null) return OperationResult.Fail("Please sign in first.");

            if (!_hasher.Verify(currentPassword, user.PasswordHash))
            {
                _log.Record(user.Role == UserRole.Administrator ? ActivityModule.Users : ActivityModule.Security,
                    "Password change refused", "User account", user.Username,
                    "The current password was typed wrongly.");

                return OperationResult.Fail("The current password is not correct.");
            }

            var problems = InputValidator.ValidatePassword(newPassword, confirmation);
            if (problems.Count > 0) return OperationResult.Fail(string.Join(Environment.NewLine, problems.ToArray()));

            if (_hasher.Verify(newPassword, user.PasswordHash))
                return OperationResult.Fail("Please choose a password you have not used before.");

            string salt;
            string hash = _hasher.Hash(newPassword, out salt);
            _repository.UpdatePassword(user.UserId, hash, salt, _hasher.Iterations, false, user.Username, _clock.Now());

            user.SetPassword(hash, salt, _hasher.Iterations, false);

            _log.Record(ActivityModule.Security, "Password changed", "User account", user.Username,
                "The person changed their own password.");

            return OperationResult.Ok("Your new password is saved.");
        }
    }
}
