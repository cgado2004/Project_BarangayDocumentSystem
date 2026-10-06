// ---------------------------------------------------------------------------
//  ActivityLogEntry.cs - one line of the record of who did what.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;

namespace BarangayDocumentSystem.Models
{
    /// <summary>
    /// A single entry in the activity log.
    ///
    /// I write one of these for anything that changes data: signing in and
    /// out, registering or editing a resident, filing a request, moving a
    /// request along, collecting money, voiding a receipt, creating or
    /// switching off a user, and generating a report.
    ///
    /// Two rules I set for myself. First, the log is append-only - there is no
    /// edit and no delete anywhere in the system, because a log that can be
    /// edited is not evidence of anything. Second, every entry names a person
    /// and a module, so the admin can answer the only two questions that
    /// matter in a barangay dispute: who did this, and when.
    /// </summary>
    public class ActivityLogEntry
    {
        public long LogId { get; internal set; }

        public DateTime OccurredOn { get; set; }

        /// <summary>The login name, not the display name, so the entry cannot
        /// be confused by two clerks sharing a first name.</summary>
        public string Username { get; set; }

        public UserRole Role { get; set; }

        public ActivityModule Module { get; set; }

        /// <summary>A short verb in the past tense: "Registered", "Edited",
        /// "Filed", "Cleared", "Collected", "Voided", "Signed in".</summary>
        public string Action { get; set; }

        /// <summary>What was touched: "Resident", "Document request",
        /// "Official receipt", "User account".</summary>
        public string TargetType { get; set; }

        /// <summary>The id or reference of what was touched, so an admin can
        /// jump back to the record. Empty when there is nothing to point at.</summary>
        public string TargetReference { get; set; }

        /// <summary>The sentence a person reads. I write it in plain words,
        /// e.g. "Cleared request 2026-000123 (Barangay Clearance) filed
        /// outside office hours".</summary>
        public string Details { get; set; }

        /// <summary>The computer the action came from. Useful when two
        /// machines share one login, which happens more often than it should.</summary>
        public string MachineName { get; set; }

        public ActivityLogEntry()
        {
            Username = string.Empty;
            Action = string.Empty;
            TargetType = string.Empty;
            TargetReference = string.Empty;
            Details = string.Empty;
            MachineName = string.Empty;
        }

        /// <summary>Builds the row I am about to save. The screen calls this
        /// through ActivityLogService, but the shape of an entry lives here so
        /// every module writes the same fields.</summary>
        public static ActivityLogEntry Create(string username, UserRole role, ActivityModule module,
                                              string action, string targetType, string targetReference,
                                              string details, DateTime when, string machineName)
        {
            ActivityLogEntry entry = new ActivityLogEntry();
            entry.Username = username ?? string.Empty;
            entry.Role = role;
            entry.Module = module;
            entry.Action = action ?? string.Empty;
            entry.TargetType = targetType ?? string.Empty;
            entry.TargetReference = targetReference ?? string.Empty;
            entry.Details = details ?? string.Empty;
            entry.OccurredOn = when;
            entry.MachineName = machineName ?? string.Empty;
            return entry;
        }

        public string GetModuleText()
        {
            return EnumText.Spaced(Module.ToString());
        }

        /// <summary>The one-line version shown in the grid.</summary>
        public string ToDisplay()
        {
            string text = OccurredOn.ToString("dd MMM yyyy h:mm tt") + "  " + Action;
            if (!string.IsNullOrWhiteSpace(TargetType)) text += " " + TargetType.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(TargetReference)) text += " " + TargetReference;
            if (!string.IsNullOrWhiteSpace(Details)) text += " - " + Details;
            return text;
        }

        public override string ToString()
        {
            return ToDisplay();
        }
    }
}
