// ---------------------------------------------------------------------------
//  ActivityLogService.cs - the record of who did what.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;

namespace BarangayDocumentSystem.Services
{
    /// <summary>
    /// Writes and reads the activity log.
    ///
    /// Every service in this program calls this before it finishes, so the log
    /// is written by the code that did the work rather than by the screen that
    /// asked for it. If a future screen forgets to log something, the record
    /// is still there, because the service wrote it.
    ///
    /// Nothing here can edit or delete a line. There is no such method, and
    /// that is on purpose: a log an administrator can rewrite is not evidence
    /// of anything, and the one time it matters - a resident disputing that a
    /// clearance was ever issued - is exactly the time nobody can be trusted to
    /// leave it alone.
    /// </summary>
    public class ActivityLogService
    {
        private readonly IBarangayRepository _repository;
        private readonly SessionManager _session;
        private readonly IClock _clock;

        public ActivityLogService(IBarangayRepository repository, SessionManager session, IClock clock)
        {
            if (repository == null) throw new ArgumentNullException("repository");

            _repository = repository;
            _session = session;
            _clock = clock == null ? new SystemClock() : clock;
        }

        // ==================================================================
        //  Writing
        // ==================================================================

        /// <summary>
        /// The usual call: the module, the verb, what it was about, and a
        /// sentence in plain words.
        /// </summary>
        public void Record(ActivityModule module, string action, string targetType,
                           string targetReference, string details)
        {
            RecordAs(_session == null ? string.Empty : _session.Username,
                     _session == null ? UserRole.Clerk : _session.Role,
                     module, action, targetType, targetReference, details);
        }

        /// <summary>The same, for the two moments when there is nobody signed
        /// in yet or any more: a failed sign-in and a sign-out.</summary>
        public void RecordAs(string username, UserRole role, ActivityModule module, string action,
                             string targetType, string targetReference, string details)
        {
            ActivityLogEntry entry = ActivityLogEntry.Create(
                username, role, module, action, targetType, targetReference, details,
                _clock.Now(), Environment.MachineName);

            try
            {
                _repository.AppendActivityLog(entry);
            }
            catch (RepositoryException error)
            {
                // I never let a logging problem stop the work the clerk was
                // doing. The log is important; the resident standing at the
                // counter is more important.
                AppLog.Warn("The activity log could not be written: " + error.Message);
            }
        }

        /// <summary>Convenience for the workflow: one line per state change,
        /// written the same way for every kind of request.</summary>
        public void RecordStatusChange(DocumentRequest request, RequestStatus previousStatus, string reason)
        {
            if (request == null) return;

            string details = "Moved request " + request.ReferenceNumber + " ("
                           + request.GetDocumentName() + ") from " + EnumText.Of(previousStatus)
                           + " to " + EnumText.Of(request.Status) + ".";

            if (!string.IsNullOrWhiteSpace(reason)) details += " Reason: " + reason;
            if (request.FeeBasis.Length > 0 && request.Status == RequestStatus.Released)
                details += " Fee on file: " + request.FeeBasis;

            Record(ActivityModule.Requests, ActionFor(request.Status), "Document request",
                request.ReferenceNumber, details);
        }

        /// <summary>Convenience for money: every collection names the receipt
        /// and the booklet, because that is what an audit asks to see.</summary>
        public void RecordCollection(OfficialReceipt receipt, string residentName)
        {
            if (receipt == null) return;

            string details = "Collected P" + receipt.Amount.ToString("#,##0.00") + " from "
                           + (string.IsNullOrWhiteSpace(residentName) ? receipt.PayerName : residentName)
                           + ". OR " + receipt.OrNumber + ", series " + receipt.SeriesCode
                           + ", control " + receipt.ControlNumber + ".";

            Record(ActivityModule.Payments, "Collected", "Official receipt", receipt.OrNumber, details);
        }

        private static string ActionFor(RequestStatus status)
        {
            switch (status)
            {
                case RequestStatus.Pending: return "Returned to pending";
                case RequestStatus.Processing: return "Sent for processing";
                case RequestStatus.Cleared: return "Cleared";
                case RequestStatus.ReadyForRelease: return "Prepared for release";
                case RequestStatus.Released: return "Released";
                case RequestStatus.Rejected: return "Rejected";
                default: return "Updated";
            }
        }

        // ==================================================================
        //  Reading
        // ==================================================================

        public IList<ActivityLogEntry> GetLog(ActivityLogQuery query)
        {
            return _repository.GetActivityLog(query);
        }

        public DataTable GetLogTable(ActivityLogQuery query)
        {
            return _repository.GetActivityLogTable(query);
        }

        /// <summary>
        /// The per-day count on the dashboard, and the "is anything happening
        /// at all" question the panel asked me. It also quietly proves the log
        /// is being written: a day with zero entries after a day of work would
        /// mean something is wrong with the code, not with the barangay.
        /// </summary>
        public int CountForDay(DateTime day)
        {
            return _repository.CountActivityForDay(day);
        }

        /// <summary>The people who appear in the log, for the filter box on
        /// the activity screen. I collect them from the log itself instead of
        /// from the users table, so an account that was switched off still
        /// shows up under its own name.</summary>
        public IList<string> GetUsernames()
        {
            ActivityLogQuery query = new ActivityLogQuery();
            query.From = DateTime.Today.AddMonths(-6);
            query.MaximumRows = 1000;

            List<string> names = new List<string>();
            foreach (ActivityLogEntry entry in _repository.GetActivityLog(query))
                if (!string.IsNullOrWhiteSpace(entry.Username) && !names.Contains(entry.Username))
                    names.Add(entry.Username);

            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }
    }
}
