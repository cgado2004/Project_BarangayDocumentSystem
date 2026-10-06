// ---------------------------------------------------------------------------
//  SessionManager.cs - who is signed in right now.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Security
{
    /// <summary>
    /// Knows who is using the program, what they are allowed to do, and
    /// whether they have walked away from the computer.
    ///
    /// Two reasons this is not just a variable on the main form:
    ///
    /// 1. Every service asks it before doing something sensitive - taking a
    ///    payment, voiding a receipt, creating a user. The screen also hides
    ///    what a person may not do, but hiding a button is not a security
    ///    measure; the check that matters is the one the service makes.
    ///
    /// 2. The idle timeout. A barangay hall is a busy place and clerks walk
    ///    away from the counter. After the configured number of idle minutes
    ///    the program locks itself and asks for the password again, so a
    ///    resident's records are not left open on the screen for the next
    ///    person in the queue.
    /// </summary>
    public class SessionManager
    {
        private DateTime _lastActivity;

        /// <summary>The session the running program is using. Program.cs sets
        /// it once, the same way the barangay profile is set, so a dialog can
        /// reach the current user without it being passed through ten
        /// constructors.</summary>
        public static SessionManager Current { get; set; }

        public UserAccount User { get; private set; }

        public bool IsSignedIn { get { return User != null; } }

        public string Username
        {
            get { return User == null ? string.Empty : User.Username; }
        }

        public string DisplayName
        {
            get { return User == null ? string.Empty : User.FullName; }
        }

        public UserRole Role
        {
            get { return User == null ? UserRole.Clerk : User.Role; }
        }

        public SessionManager()
        {
            _lastActivity = DateTime.Now;
        }

        // ==================================================================
        //  Signing in and out
        // ==================================================================

        public void SignIn(UserAccount user)
        {
            User = user;
            Touch();
            Current = this;
        }

        public void SignOut()
        {
            User = null;
        }

        /// <summary>Called by the shell whenever the person does something -
        /// a click, a key press, a save. It is what keeps the session alive.</summary>
        public void Touch()
        {
            _lastActivity = DateTime.Now;
        }

        /// <summary>
        /// True when the person has been idle for longer than the configured
        /// time. Zero minutes in App.config turns the timeout off, which is
        /// useful during a demonstration where nobody wants to be logged out
        /// in the middle of a walkthrough.
        /// </summary>
        public bool IsExpired
        {
            get
            {
                int minutes = AppConfig.SessionTimeoutMinutes;
                if (minutes <= 0) return false;

                return (DateTime.Now - _lastActivity).TotalMinutes >= minutes;
            }
        }

        public int IdleMinutes
        {
            get { return (int)(DateTime.Now - _lastActivity).TotalMinutes; }
        }

        // ==================================================================
        //  What this person may do
        // ==================================================================

        public bool Has(Permission permission)
        {
            if (User == null) return false;
            return PermissionSet.Allows(User.Role, permission);
        }

        /// <summary>
        /// The sentence a refusal shows. A clerk who is told what they cannot
        /// do, and who to ask, does not need to phone me.
        /// </summary>
        public string RefusalFor(Permission permission)
        {
            if (User == null) return "Please sign in first.";
            return PermissionSet.ExplainRefusal(User.Role, permission);
        }

        /// <summary>Records the refusal in the log. I want attempts that were
        /// turned away to be visible to the admin - not to punish anybody, but
        /// because a clerk who keeps trying to void a receipt is telling me
        /// something about the workflow.</summary>
        public string DescribeForLog(Permission permission)
        {
            return "Tried to " + EnumText.Spaced(permission.ToString()).ToLowerInvariant()
                 + " and was refused because a " + EnumText.Of(Role) + " may not do that.";
        }
    }
}
