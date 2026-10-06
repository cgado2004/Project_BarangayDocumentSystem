// ---------------------------------------------------------------------------
//  Repositories.cs - the contracts the screens talk to.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces
{
    /// <summary>
    /// The filters the residents screen can ask with. I made it an object
    /// instead of a list of arguments because the screen passes the same set
    /// of choices to three places (the grid, the count line and the export).
    /// </summary>
    public class ResidentQuery
    {
        public string Keyword { get; set; }
        public string Purok { get; set; }
        public RecordState? RecordState { get; set; }
        public ResidencyStatus? ResidencyStatus { get; set; }
        public ResidentClassification? Classification { get; set; }

        /// <summary>True to include people who were deactivated or archived.
        /// The residents screen has a tick box for exactly this, because the
        /// default list should show the people the barangay is serving today.</summary>
        public bool IncludeInactive { get; set; }

        public ResidentQuery()
        {
            Keyword = string.Empty;
            Purok = string.Empty;
        }
    }

    /// <summary>
    /// The filters the requests screen can ask with - including the per-day
    /// filter the barangay asked for, which is why From and To are dates the
    /// screen fills in from its "Today / This week / This month / Custom"
    /// choice.
    /// </summary>
    public class RequestQuery
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public RequestStatus? Status { get; set; }
        public DocumentType? DocumentType { get; set; }
        public string Purok { get; set; }
        public string Keyword { get; set; }
        public int? ResidentId { get; set; }
        public bool BusinessOnly { get; set; }

        public RequestQuery()
        {
            Purok = string.Empty;
            Keyword = string.Empty;
        }
    }

    public class ReceiptQuery
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string Keyword { get; set; }
        public bool IncludeVoid { get; set; }

        public ReceiptQuery()
        {
            Keyword = string.Empty;
            IncludeVoid = true;
        }
    }

    public class ActivityLogQuery
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string Username { get; set; }
        public ActivityModule? Module { get; set; }
        public string Keyword { get; set; }
        public int MaximumRows { get; set; }

        public ActivityLogQuery()
        {
            Username = string.Empty;
            Keyword = string.Empty;
            MaximumRows = 500;
        }
    }

    /// <summary>
    /// Everything the system needs to know about residents.
    ///
    /// I kept the storage contract small on purpose: the screens and the
    /// services only ever see this, so MySQL can be swapped for SQL Server (or
    /// for the in-memory store my tests use) without a single screen changing.
    /// </summary>
    public interface IResidentRepository
    {
        IList<Resident> GetResidents(ResidentQuery query);
        Resident GetResident(int residentId);
        int InsertResident(Resident resident);
        void UpdateResident(Resident resident);

        /// <summary>Deactivate, archive or reactivate. It is a separate call
        /// from UpdateResident because it always carries a reason and always
        /// writes the reason into the record.</summary>
        void SetRecordState(int residentId, RecordState state, string reason, string changedBy, DateTime when);

        /// <summary>Looks for somebody who may already be on the registry, so
        /// the same person is not encoded twice.</summary>
        IList<Resident> FindPossibleDuplicates(string firstName, string lastName, DateTime dateOfBirth, int exceptResidentId);

        IList<Dependent> GetDependents(int headResidentId);
        int InsertDependent(Dependent dependent);
        void UpdateDependent(Dependent dependent);
        void DeleteDependent(int dependentId);

        /// <summary>How many people the barangay is counting today: active
        /// residents only, unless the caller says otherwise.</summary>
        int CountResidents(bool activeOnly);
    }

    public interface IRequestRepository
    {
        IList<DocumentRequest> GetRequests(RequestQuery query);
        DocumentRequest GetRequest(int requestId);
        int InsertRequest(DocumentRequest request);
        void UpdateRequest(DocumentRequest request);

        void AppendStatusHistory(int requestId, RequestStatusChange change);

        /// <summary>The next counter number for the year, e.g. "2026-000124".
        /// The database hands this out so two clerks on two machines cannot
        /// hand out the same number.</summary>
        string NextReferenceNumber(DateTime when);

        /// <summary>The requests still waiting for the next office window to
        /// open - the ones filed after 4:00 PM or before 8:00 AM.</summary>
        IList<DocumentRequest> GetRequestsWaitingForWindow();
    }

    public interface IReceiptRepository
    {
        IList<OfficialReceipt> GetReceipts(ReceiptQuery query);
        OfficialReceipt GetReceipt(int receiptId);
        int InsertReceipt(OfficialReceipt receipt);
        void UpdateReceipt(OfficialReceipt receipt);

        /// <summary>True when that OR number is already in the register. I
        /// check it before saving, and the database also has a unique key, so
        /// a duplicate cannot slip through even if two clerks click at once.</summary>
        bool ReceiptNumberExists(string seriesCode, string orNumber, int exceptReceiptId);

        IList<ReceiptSeries> GetReceiptSeries(bool activeOnly);
        int InsertReceiptSeries(ReceiptSeries series);
        void UpdateReceiptSeries(ReceiptSeries series);

        /// <summary>Which booklet covers this control number? It is how I stop
        /// a receipt being written against a booklet that was never issued to
        /// the barangay.</summary>
        ReceiptSeries FindSeriesForControlNumber(string controlNumber);
    }

    public interface IUserRepository
    {
        IList<UserAccount> GetUsers();
        UserAccount GetUser(int userId);
        UserAccount GetUserByUsername(string username);
        int InsertUser(UserAccount user);
        void UpdateUser(UserAccount user);
        void UpdatePassword(int userId, string hash, string salt, int iterations, bool mustChange, string changedBy, DateTime when);
        void UpdateLoginState(UserAccount user);
        bool UsernameExists(string username, int exceptUserId);

        /// <summary>I refuse to switch off the last administrator, because
        /// that is how a system locks its own barangay out.</summary>
        int CountActiveAdministrators();
    }

    public interface IActivityLogRepository
    {
        void AppendActivityLog(ActivityLogEntry entry);
        IList<ActivityLogEntry> GetActivityLog(ActivityLogQuery query);
        int CountActivityForDay(DateTime day);
        DataTable GetActivityLogTable(ActivityLogQuery query);
    }

    /// <summary>
    /// The reporting side of storage.
    ///
    /// Reports come back as a DataTable on purpose: that is the shape Crystal
    /// Reports binds to, and it is also the shape the built-in viewer and the
    /// printer use, so one query feeds all three.
    /// </summary>
    public interface IReportRepository
    {
        DataTable GetDashboardSummary(DateTime from, DateTime to);
        DataTable GetPerDayTransactions(DateTime from, DateTime to, RequestStatus? status, string purok);
        DataTable GetDocumentRegister(DateTime from, DateTime to, RequestStatus? status);
        DataTable GetCollections(DateTime from, DateTime to);
        DataTable GetBusinessClearances(DateTime from, DateTime to);
        DataTable GetCensusSummary(string purok);
        DataTable GetPopulationByPurok(bool activeOnly);

        /// <summary>Population by age bracket, for the census sheet. The
        /// brackets themselves are defined once, in the Resident class.</summary>
        DataTable GetPopulationByAgeBracket(string purok, bool activeOnly);
        DataTable GetResidentMasterList(string purok, RecordState? state, ResidencyStatus? residency);
    }

    /// <summary>
    /// One object that offers all of the contracts above.
    ///
    /// I grouped them so a screen can be handed a single store instead of six,
    /// while each contract stays small enough to read. MySqlBarangayRepository
    /// is the only class that implements it for real, and it is named in
    /// exactly one place in the whole program - Program.cs.
    /// </summary>
    public interface IBarangayRepository : IResidentRepository, IRequestRepository,
                                            IReceiptRepository, IUserRepository,
                                            IActivityLogRepository, IReportRepository
    {
        /// <summary>A short description for the status bar, e.g.
        /// "MySQL - barangay_db on localhost".</summary>
        string Describe();

        /// <summary>Creates the database, the tables, the stored procedures
        /// and the first administrator if they are missing. Returns a sentence
        /// describing what it did.</summary>
        string EnsureDatabaseReady();

        /// <summary>Quick check that the store answers. The login screen calls
        /// this so a database problem is reported before anybody types a
        /// password.</summary>
        void HealthCheck();
    }

    /// <summary>
    /// Where the clock comes from.
    ///
    /// This looks like an unnecessary extra, and it is the one abstraction I
    /// would defend hardest: the 8:00 AM to 4:00 PM rule is the heart of the
    /// request workflow, and no test can verify it if the code calls
    /// DateTime.Now directly. In production this is the real clock; my rule
    /// checks hand it a clock frozen at 4:05 PM.
    /// </summary>
    public interface IClock
    {
        DateTime Now();
    }

    public class SystemClock : IClock
    {
        public DateTime Now()
        {
            return DateTime.Now;
        }
    }

    /// <summary>A clock that does not move, for the rule checks.</summary>
    public class FixedClock : IClock
    {
        private DateTime _now;

        public FixedClock(DateTime now)
        {
            _now = now;
        }

        public void Set(DateTime now)
        {
            _now = now;
        }

        public DateTime Now()
        {
            return _now;
        }
    }
}
