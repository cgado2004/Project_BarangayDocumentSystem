// ---------------------------------------------------------------------------
//  TimeWindowPolicy.cs - the 8:00 AM to 4:00 PM rule.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services
{
    /// <summary>
    /// The rule the barangay asked for in one sentence:
    ///
    ///   A request that needs no validation and is filed between 8:00 AM and
    ///   4:00 PM is CLEARED on the spot. Anything filed from 4:01 PM to
    ///   7:59 AM waits as PENDING until the next working day.
    ///
    /// I put it in its own class for two reasons. First, it is the kind of
    /// rule that gets asked about at the counter ("why is mine still pending
    /// when I filed yesterday?"), so it should be explainable in one place.
    /// Second, it reads the clock - and a rule that reads the clock cannot be
    /// tested unless the clock can be handed to it, which is why it takes an
    /// IClock instead of calling DateTime.Now itself.
    ///
    /// A request that DOES need validation - a business clearance, always -
    /// goes to Processing regardless of the time, because a person has to look
    /// at it. The window cannot approve what a validator has not seen.
    /// </summary>
    public class TimeWindowPolicy
    {
        private readonly IClock _clock;

        public TimeWindowPolicy() : this(new SystemClock())
        {
        }

        public TimeWindowPolicy(IClock clock)
        {
            _clock = clock == null ? new SystemClock() : clock;
        }

        public TimeSpan WindowStart { get { return AppConfig.OfficeWindowStart; } }
        public TimeSpan WindowEnd { get { return AppConfig.OfficeWindowEnd; } }

        /// <summary>
        /// True when that moment is inside the working window.
        ///
        /// I compare on the time of day only, so the rule holds whatever date
        /// it is, and I include both ends: 8:00:00 AM and 4:00:00 PM exactly
        /// are inside, 4:00:01 PM is not. That single second is the difference
        /// between a clerk being able to clear a request and having to explain
        /// to a resident why they must come back tomorrow, so it is written
        /// out rather than left to a comparison operator I might misread later.
        /// </summary>
        public bool IsInsideOfficeWindow(DateTime moment)
        {
            TimeSpan time = moment.TimeOfDay;
            return time >= WindowStart && time <= WindowEnd;
        }

        /// <summary>
        /// What status a freshly filed request should start in.
        ///
        /// The clerk can force validation on any request (there is a tick box),
        /// which is how a clearance for somebody with a pending complaint gets
        /// looked at before it is signed.
        /// </summary>
        public RequestStatus SuggestStartingStatus(DocumentRequest request, bool forceValidation)
        {
            if (request == null) throw new ArgumentNullException("request");

            bool needsValidation = forceValidation || request.RequiresValidation;
            if (needsValidation) return RequestStatus.Processing;

            return request.FiledDuringOfficeWindow ? RequestStatus.Cleared : RequestStatus.Pending;
        }

        /// <summary>The sentence written into the request's history, so the
        /// record explains itself two years later.</summary>
        public string ExplainStartingStatus(DocumentRequest request, RequestStatus status)
        {
            if (request == null) return string.Empty;

            string when = request.DateRequested.ToString("h:mm tt");

            if (status == RequestStatus.Processing)
                return "Filed at " + when + " and needs validation, so it went to Processing first.";

            if (status == RequestStatus.Cleared)
                return "Filed at " + when + ", inside the " + GetWindowText()
                     + " window, and needs no validation, so it was cleared on the spot.";

            return "Filed at " + when + ", outside the " + GetWindowText()
                 + " window, so it waits as Pending until the next working day.";
        }

        /// <summary>A plain sentence for the screen, e.g. what the queue shows
        /// above the status of a request filed at 4:20 in the afternoon.</summary>
        public string DescribeShift(DateTime filedAt, bool needsValidation)
        {
            if (needsValidation)
                return "This document is checked before it is cleared, whatever time it was filed.";

            if (IsInsideOfficeWindow(filedAt))
                return "Filed inside the " + GetWindowText() + " window - cleared without waiting.";

            return "Filed at " + filedAt.ToString("h:mm tt") + ", after the "
                 + WindowEnd.ToString(@"h\:mm") + " cut-off - it waits as Pending until 8:00 AM.";
        }

        /// <summary>
        /// When the office window next opens, for the message a clerk reads out
        /// to a resident. I skip ahead to 8:00 AM the same day when the office
        /// has not opened yet, and to the next day when it has closed.
        /// </summary>
        public DateTime NextWindowOpening()
        {
            DateTime now = _clock.Now();
            DateTime todayOpening = now.Date.Add(WindowStart);

            if (now <= todayOpening) return todayOpening;
            return todayOpening.AddDays(1);
        }

        /// <summary>
        /// The requests that are due to be cleared now: filed outside the
        /// window, no validation needed, and the window has opened since.
        ///
        /// This is what makes the queue honest on a Monday morning. The
        /// requests filed at 5:00 PM on Friday still say Pending, and the
        /// program clears them the moment somebody opens it on Monday.
        /// </summary>
        public bool ShouldClearNow(DocumentRequest request, DateTime now)
        {
            if (request == null) return false;
            if (request.Status != RequestStatus.Pending) return false;
            if (request.RequiresValidation) return false;
            if (request.FiledDuringOfficeWindow) return false;

            // It only clears after the window has opened on a day after it was
            // filed, so a request filed at 7:00 AM is not cleared at 7:30 AM
            // by a clerk who is already at the counter.
            if (request.DateRequested.Date == now.Date && !IsInsideOfficeWindow(now)) return false;

            return now >= request.DateRequested.Date.AddDays(1).Add(WindowStart)
                || IsInsideOfficeWindow(now);
        }

        /// <summary>"8:00 AM to 4:00 PM" - the phrase I use on the screens and
        /// in the confirmation dialog.</summary>
        public string GetWindowText()
        {
            return Friendly(WindowStart) + " to " + Friendly(WindowEnd);
        }

        /// <summary>The cut-off sentence shown above the request queue.</summary>
        public string GetCutOffText()
        {
            DateTime now = _clock.Now();
            if (IsInsideOfficeWindow(now))
                return "Now " + now.ToString("h:mm tt") + " - inside the " + GetWindowText()
                     + " window. Requests needing no validation are cleared on the spot.";

            return "Now " + now.ToString("h:mm tt") + " - outside the " + GetWindowText()
                 + " window. New requests are recorded as Pending and cleared when the window opens at "
                 + Friendly(WindowStart) + ".";
        }

        private static string Friendly(TimeSpan time)
        {
            DateTime moment = DateTime.Today.Add(time);
            return moment.ToString("h:mm tt");
        }
    }
}
