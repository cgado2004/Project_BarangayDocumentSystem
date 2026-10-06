// ---------------------------------------------------------------------------
//  Permissions.cs - who is allowed to do what.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Security
{
    /// <summary>
    /// The things a signed-in person can do. I wrote them as a list of named
    /// jobs rather than as roles scattered through the screens, because the
    /// minute a sentence like "if the user is an admin" appears in a form, the
    /// rule exists in two places and they will drift apart.
    /// </summary>
    public enum Permission
    {
        ViewResidents,
        ManageResidents,
        ArchiveResidents,
        ViewRequests,
        FileRequests,
        ValidateRequests,
        ReleaseDocuments,
        CollectPayments,
        VoidReceipts,
        ManageReceiptSeries,
        ViewCensus,
        ViewReports,
        ViewActivityLog,
        ManageUsers,
        ChangeSettings,
        ViewFeeBasis
    }

    /// <summary>
    /// The rule book: which role gets which permission.
    ///
    /// How I decided:
    ///  * The clerk runs the counter - residents, requests, collections,
    ///    census - but cannot void a receipt, manage users, or change settings.
    ///  * The punong barangay validates and approves (that is the signature on
    ///    the paper), reads every report and the activity log, but does not
    ///    encode residents or handle money at the counter.
    ///  * The administrator runs the system: users, receipt booklets, settings,
    ///    and everything the other two can do, so a small barangay with one
    ///    computer is not locked out of its own records.
    ///
    /// Two permissions deserve a note. ViewFeeBasis is administrator-only:
    /// the legal reason behind an amount is internal, which is what the
    /// barangay asked for when the clerk's screen was cleaned up. VoidReceipts
    /// is administrator-only because a voided collection is exactly the thing
    /// an audit looks at first.
    /// </summary>
    public static class PermissionSet
    {
        private static readonly Dictionary<UserRole, HashSet<Permission>> Map = BuildMap();

        private static Dictionary<UserRole, HashSet<Permission>> BuildMap()
        {
            Dictionary<UserRole, HashSet<Permission>> map =
                new Dictionary<UserRole, HashSet<Permission>>();

            map[UserRole.Clerk] = new HashSet<Permission>
            {
                Permission.ViewResidents,
                Permission.ManageResidents,
                Permission.ArchiveResidents,
                Permission.ViewRequests,
                Permission.FileRequests,
                Permission.ReleaseDocuments,
                Permission.CollectPayments,
                Permission.ViewCensus,
                Permission.ViewReports
            };

            map[UserRole.PunongBarangay] = new HashSet<Permission>
            {
                Permission.ViewResidents,
                Permission.ViewRequests,
                Permission.ValidateRequests,
                Permission.ViewCensus,
                Permission.ViewReports,
                Permission.ViewActivityLog
            };

            map[UserRole.Administrator] = new HashSet<Permission>
            {
                Permission.ViewResidents,
                Permission.ManageResidents,
                Permission.ArchiveResidents,
                Permission.ViewRequests,
                Permission.FileRequests,
                Permission.ValidateRequests,
                Permission.ReleaseDocuments,
                Permission.CollectPayments,
                Permission.VoidReceipts,
                Permission.ManageReceiptSeries,
                Permission.ViewCensus,
                Permission.ViewReports,
                Permission.ViewActivityLog,
                Permission.ManageUsers,
                Permission.ChangeSettings,
                Permission.ViewFeeBasis
            };

            return map;
        }

        public static bool Allows(UserRole role, Permission permission)
        {
            HashSet<Permission> allowed;
            if (!Map.TryGetValue(role, out allowed)) return false;
            return allowed.Contains(permission);
        }

        /// <summary>Everything a role may do, for the "what can this person
        /// see" list on the users screen.</summary>
        public static IList<Permission> For(UserRole role)
        {
            HashSet<Permission> allowed;
            if (!Map.TryGetValue(role, out allowed)) return new List<Permission>();

            List<Permission> list = new List<Permission>(allowed);
            list.Sort();
            return list;
        }

        /// <summary>A short sentence explaining a refusal, so a message box
        /// can say something better than "access denied".</summary>
        public static string ExplainRefusal(UserRole role, Permission permission)
        {
            return "A " + EnumText.Of(role) + " account is not allowed to " +
                   EnumText.Spaced(permission.ToString()).ToLowerInvariant() +
                   ". Ask the administrator if you need this.";
        }

        /// <summary>
        /// What a role is for, in a sentence a barangay clerk can read.
        ///
        /// I keep this here rather than on the users screen because the same
        /// sentence is used in three places: the users screen, the account
        /// window, and the message that tells somebody their role is not
        /// allowed to do something. One sentence, one meaning.
        /// </summary>
        public static string ExplainRole(UserRole role)
        {
            switch (role)
            {
                case UserRole.Administrator:
                    return "The administrator runs the system: staff accounts, the receipt booklets and the "
                         + "settings, on top of everything the other roles can do. This is also the only role "
                         + "that can see the reason behind a fee.";

                case UserRole.PunongBarangay:
                    return "The punong barangay checks the record and approves: requests can be sent for "
                         + "processing, cleared, prepared, released or rejected, and every report and the "
                         + "activity log can be read. Residents are not encoded and money is not collected "
                         + "with this role.";

                default:
                    return "The clerk runs the counter: registering residents, filing requests, collecting "
                         + "fees and reading the census. Voiding a receipt, managing accounts and changing "
                         + "settings are not available to this role.";
            }
        }
    }
}
