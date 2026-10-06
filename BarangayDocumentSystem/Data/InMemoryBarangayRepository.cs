// ---------------------------------------------------------------------------
//  InMemoryBarangayRepository.cs - the store that keeps everything in a list.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Data
{
    /// <summary>
    /// A complete store that lives in memory and saves nothing.
    ///
    /// Two jobs, and both matter:
    ///
    /// 1. The demo. A laptop with no MySQL can still show the whole system
    ///    with the sample barangay data, clearly labelled so nobody thinks a
    ///    real record was saved.
    ///
    /// 2. The rule checks. My tests run the real services - the real fee
    ///    schedule, the real workflow guards, the real 8:00 AM to 4:00 PM
    ///    rule - against this store, with no database and no MySQL server.
    ///    That is the only way I could verify the money rules on a machine
    ///    that has neither.
    ///
    /// It implements the same interface as the SQL store, so anything that
    /// works here works there. The reports are built by RepositoryBase, which
    /// returns the same column names the stored procedures return.
    /// </summary>
    public class InMemoryBarangayRepository : RepositoryBase, IBarangayRepository
    {
        private readonly List<Resident> _residents = new List<Resident>();
        private readonly List<Dependent> _dependents = new List<Dependent>();
        private readonly List<DocumentRequest> _requests = new List<DocumentRequest>();
        private readonly List<OfficialReceipt> _receipts = new List<OfficialReceipt>();
        private readonly List<ReceiptSeries> _series = new List<ReceiptSeries>();
        private readonly List<UserAccount> _users = new List<UserAccount>();
        private readonly List<ActivityLogEntry> _activity = new List<ActivityLogEntry>();

        private int _nextResidentId = 1;
        private int _nextDependentId = 1;
        private int _nextRequestId = 1;
        private int _nextReceiptId = 1;
        private int _nextSeriesId = 1;
        private int _nextUserId = 1;
        private long _nextLogId = 1;

        public string Describe()
        {
            return "In-memory sample data (nothing is saved)";
        }

        public string EnsureDatabaseReady()
        {
            return "The in-memory store needs no database. Nothing typed here is saved - change Storage in App.config to keep records.";
        }

        public void HealthCheck()
        {
            // There is nothing to reach, so there is nothing to fail.
        }

        // ==================================================================
        //  Residents
        // ==================================================================

        public IList<Resident> GetResidents(ResidentQuery query)
        {
            if (query == null) query = new ResidentQuery();

            IEnumerable<Resident> found = _residents;

            if (!query.IncludeInactive)
                found = query.RecordState.HasValue
                    ? found.Where(r => r.RecordState == query.RecordState.Value)
                    : found.Where(r => r.RecordState == RecordState.Active);

            if (!string.IsNullOrWhiteSpace(query.Purok))
                found = found.Where(r => string.Equals(r.Purok, query.Purok.Trim(), StringComparison.OrdinalIgnoreCase));

            if (query.ResidencyStatus.HasValue)
                found = found.Where(r => r.ResidencyStatus == query.ResidencyStatus.Value);

            if (query.Classification.HasValue && query.Classification.Value != ResidentClassification.None)
                found = found.Where(r => r.HasClassification(query.Classification.Value));

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                string keyword = query.Keyword.Trim();
                found = found.Where(r =>
                    Contains(r.FirstName, keyword) || Contains(r.MiddleName, keyword) ||
                    Contains(r.LastName, keyword) || Contains(r.ContactNumber, keyword) ||
                    Contains(r.Purok, keyword));
            }

            return found.OrderBy(r => r.LastName).ThenBy(r => r.FirstName).ToList();
        }

        public Resident GetResident(int residentId)
        {
            Resident resident = _residents.FirstOrDefault(r => r.ResidentId == residentId);
            if (resident == null) return null;

            resident.ClearDependents();
            foreach (Dependent dependent in _dependents.Where(d => d.HeadResidentId == residentId))
                resident.AddDependent(dependent);

            return resident;
        }

        public IList<Resident> FindPossibleDuplicates(string firstName, string lastName,
                                                     DateTime dateOfBirth, int exceptResidentId)
        {
            return _residents.Where(r =>
                string.Equals(r.FirstName, (firstName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.LastName, (lastName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase) &&
                r.DateOfBirth.Date == dateOfBirth.Date &&
                r.ResidentId != exceptResidentId).ToList();
        }

        public int InsertResident(Resident resident)
        {
            resident.ResidentId = _nextResidentId++;
            _residents.Add(resident);
            return resident.ResidentId;
        }

        public void UpdateResident(Resident resident)
        {
            // The list already holds the same object, so there is nothing to
            // copy. I keep the method so both stores look the same to callers.
        }

        public void SetRecordState(int residentId, RecordState state, string reason,
                                   string changedBy, DateTime when)
        {
            Resident resident = _residents.FirstOrDefault(r => r.ResidentId == residentId);
            if (resident == null) throw new RepositoryException("changing the resident's status",
                "That resident is no longer on the list.");

            resident.SetRecordState(state, reason, changedBy, when);
        }

        public int CountResidents(bool activeOnly)
        {
            return activeOnly
                ? _residents.Count(r => r.RecordState == RecordState.Active)
                : _residents.Count;
        }

        // ==================================================================
        //  Dependents
        // ==================================================================

        public IList<Dependent> GetDependents(int headResidentId)
        {
            return _dependents.Where(d => d.HeadResidentId == headResidentId)
                .OrderBy(d => d.FullName).ToList();
        }

        public int InsertDependent(Dependent dependent)
        {
            dependent.DependentId = _nextDependentId++;
            _dependents.Add(dependent);
            return dependent.DependentId;
        }

        public void UpdateDependent(Dependent dependent)
        {
        }

        public void DeleteDependent(int dependentId)
        {
            Dependent dependent = _dependents.FirstOrDefault(d => d.DependentId == dependentId);
            if (dependent != null) _dependents.Remove(dependent);
        }

        // ==================================================================
        //  Requests
        // ==================================================================

        public IList<DocumentRequest> GetRequests(RequestQuery query)
        {
            if (query == null) query = new RequestQuery();

            IEnumerable<DocumentRequest> found = _requests;

            if (query.From.HasValue)
                found = found.Where(r => r.DateRequested.Date >= query.From.Value.Date);

            if (query.To.HasValue)
                found = found.Where(r => r.DateRequested.Date <= query.To.Value.Date);

            if (query.Status.HasValue)
                found = found.Where(r => r.Status == query.Status.Value);

            if (query.DocumentType.HasValue)
                found = found.Where(r => r.DocumentType == query.DocumentType.Value);

            if (query.ResidentId.HasValue)
                found = found.Where(r => r.ResidentId == query.ResidentId.Value);

            if (query.BusinessOnly)
                found = found.Where(r => r.IsBusinessRequest);

            if (!string.IsNullOrWhiteSpace(query.Purok))
            {
                string purok = query.Purok.Trim();
                found = found.Where(r => PurokOf(r.ResidentId, purok));
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                string keyword = query.Keyword.Trim();
                found = found.Where(r =>
                    Contains(r.ReferenceNumber, keyword) || Contains(r.ResidentName, keyword) ||
                    Contains(r.Purpose, keyword) ||
                    (r.Business != null && Contains(r.Business.BusinessName, keyword)));
            }

            return found.OrderByDescending(r => r.DateRequested).ThenByDescending(r => r.RequestId).ToList();
        }

        public DocumentRequest GetRequest(int requestId)
        {
            return _requests.FirstOrDefault(r => r.RequestId == requestId);
        }

        public int InsertRequest(DocumentRequest request)
        {
            request.RequestId = _nextRequestId++;
            request.ResidentName = NameOf(request.ResidentId);
            _requests.Add(request);
            return request.RequestId;
        }

        public void UpdateRequest(DocumentRequest request)
        {
            request.ResidentName = NameOf(request.ResidentId);
        }

        public void AppendStatusHistory(int requestId, RequestStatusChange change)
        {
            // The request object keeps its own history in memory, so there is
            // nothing separate to write here.
        }

        public string NextReferenceNumber(DateTime when)
        {
            return BuildReferenceNumber(when.Year, NextSequence(_requests, when.Year));
        }

        public IList<DocumentRequest> GetRequestsWaitingForWindow()
        {
            return _requests.Where(r => r.Status == RequestStatus.Pending
                                     && !r.RequiresValidation
                                     && !r.FiledDuringOfficeWindow).ToList();
        }

        // ==================================================================
        //  Receipts
        // ==================================================================

        public IList<OfficialReceipt> GetReceipts(ReceiptQuery query)
        {
            if (query == null) query = new ReceiptQuery();

            IEnumerable<OfficialReceipt> found = _receipts;

            if (query.From.HasValue) found = found.Where(r => r.OrDate.Date >= query.From.Value.Date);
            if (query.To.HasValue) found = found.Where(r => r.OrDate.Date <= query.To.Value.Date);
            if (!query.IncludeVoid) found = found.Where(r => !r.IsVoid);

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                string keyword = query.Keyword.Trim();
                found = found.Where(r => Contains(r.OrNumber, keyword) || Contains(r.ControlNumber, keyword) ||
                                         Contains(r.PayerName, keyword) || Contains(r.SeriesCode, keyword));
            }

            return found.OrderByDescending(r => r.OrDate).ThenByDescending(r => r.ReceiptId).ToList();
        }

        public OfficialReceipt GetReceipt(int receiptId)
        {
            return _receipts.FirstOrDefault(r => r.ReceiptId == receiptId);
        }

        public int InsertReceipt(OfficialReceipt receipt)
        {
            receipt.ReceiptId = _nextReceiptId++;
            _receipts.Add(receipt);
            return receipt.ReceiptId;
        }

        public void UpdateReceipt(OfficialReceipt receipt)
        {
        }

        public bool ReceiptNumberExists(string seriesCode, string orNumber, int exceptReceiptId)
        {
            return _receipts.Any(r => r.ReceiptId != exceptReceiptId
                && string.Equals(r.SeriesCode, (seriesCode ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)
                && string.Equals(r.OrNumber, (orNumber ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public IList<ReceiptSeries> GetReceiptSeries(bool activeOnly)
        {
            return activeOnly
                ? _series.Where(s => s.IsActive).OrderBy(s => s.SeriesCode).ToList()
                : _series.OrderBy(s => s.SeriesCode).ToList();
        }

        public int InsertReceiptSeries(ReceiptSeries series)
        {
            series.SeriesId = _nextSeriesId++;
            _series.Add(series);
            return series.SeriesId;
        }

        public void UpdateReceiptSeries(ReceiptSeries series)
        {
        }

        public ReceiptSeries FindSeriesForControlNumber(string controlNumber)
        {
            return _series.FirstOrDefault(s => s.IsActive && s.ContainsControlNumber(controlNumber));
        }

        // ==================================================================
        //  Users
        // ==================================================================

        public IList<UserAccount> GetUsers()
        {
            return _users.OrderBy(u => u.FullName).ToList();
        }

        public UserAccount GetUser(int userId)
        {
            return _users.FirstOrDefault(u => u.UserId == userId);
        }

        public UserAccount GetUserByUsername(string username)
        {
            string wanted = (username ?? string.Empty).Trim();
            return _users.FirstOrDefault(u => string.Equals(u.Username, wanted, StringComparison.OrdinalIgnoreCase));
        }

        public int InsertUser(UserAccount user)
        {
            user.UserId = _nextUserId++;
            _users.Add(user);
            return user.UserId;
        }

        public void UpdateUser(UserAccount user)
        {
        }

        public void UpdatePassword(int userId, string hash, string salt, int iterations,
                                  bool mustChange, string changedBy, DateTime when)
        {
            UserAccount user = GetUser(userId);
            if (user == null) return;

            user.SetPassword(hash, salt, iterations, mustChange);
            user.UpdatedBy = changedBy;
            user.UpdatedOn = when;
        }

        public void UpdateLoginState(UserAccount user)
        {
        }

        public bool UsernameExists(string username, int exceptUserId)
        {
            string wanted = (username ?? string.Empty).Trim();
            return _users.Any(u => u.UserId != exceptUserId
                && string.Equals(u.Username, wanted, StringComparison.OrdinalIgnoreCase));
        }

        public int CountActiveAdministrators()
        {
            return _users.Count(u => u.Role == UserRole.Administrator && u.IsActive);
        }

        // ==================================================================
        //  Activity log
        // ==================================================================

        public void AppendActivityLog(ActivityLogEntry entry)
        {
            entry.LogId = _nextLogId++;
            _activity.Add(entry);
        }

        public IList<ActivityLogEntry> GetActivityLog(ActivityLogQuery query)
        {
            if (query == null) query = new ActivityLogQuery();

            IEnumerable<ActivityLogEntry> found = _activity;

            if (query.From.HasValue) found = found.Where(e => e.OccurredOn.Date >= query.From.Value.Date);
            if (query.To.HasValue) found = found.Where(e => e.OccurredOn.Date <= query.To.Value.Date);

            if (!string.IsNullOrWhiteSpace(query.Username))
                found = found.Where(e => string.Equals(e.Username, query.Username.Trim(), StringComparison.OrdinalIgnoreCase));

            if (query.Module.HasValue) found = found.Where(e => e.Module == query.Module.Value);

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                string keyword = query.Keyword.Trim();
                found = found.Where(e => Contains(e.Details, keyword) || Contains(e.Action, keyword) ||
                                         Contains(e.TargetReference, keyword) || Contains(e.Username, keyword));
            }

            List<ActivityLogEntry> ordered = found.OrderByDescending(e => e.OccurredOn).ToList();
            if (query.MaximumRows > 0 && ordered.Count > query.MaximumRows)
                ordered = ordered.Take(query.MaximumRows).ToList();

            return ordered;
        }

        public int CountActivityForDay(DateTime day)
        {
            return _activity.Count(e => e.OccurredOn.Date == day.Date);
        }

        public DataTable GetActivityLogTable(ActivityLogQuery query)
        {
            return BuildActivityTable(GetActivityLog(query));
        }

        // ==================================================================
        //  Reports
        // ==================================================================

        public DataTable GetDashboardSummary(DateTime from, DateTime to)
        {
            List<Dependent> dependents = new List<Dependent>(_dependents);
            return BuildDashboardTable(_residents, dependents, _requests, _receipts,
                CountActivityForDay(DateTime.Today), from, to);
        }

        public DataTable GetPerDayTransactions(DateTime from, DateTime to, RequestStatus? status, string purok)
        {
            return BuildPerDayTable(_requests, from, to, status, purok, ResidentsById());
        }

        public DataTable GetDocumentRegister(DateTime from, DateTime to, RequestStatus? status)
        {
            return BuildRegisterTable(_requests, from, to, status);
        }

        public DataTable GetCollections(DateTime from, DateTime to)
        {
            return BuildCollectionsTable(_receipts, from, to);
        }

        public DataTable GetBusinessClearances(DateTime from, DateTime to)
        {
            return BuildBusinessTable(_requests, from, to);
        }

        public DataTable GetCensusSummary(string purok)
        {
            return BuildCensusTable(_residents, purok);
        }

        public DataTable GetPopulationByPurok(bool activeOnly)
        {
            return BuildPopulationByPurokTable(_residents, activeOnly);
        }

        public DataTable GetPopulationByAgeBracket(string purok, bool activeOnly)
        {
            return BuildPopulationByAgeTable(_residents, purok, activeOnly);
        }

        public DataTable GetResidentMasterList(string purok, RecordState? state, ResidencyStatus? residency)
        {
            return BuildMasterListTable(_residents, purok, state, residency);
        }

        // ==================================================================
        //  Helpers
        // ==================================================================

        private static bool Contains(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Dictionary<int, Resident> ResidentsById()
        {
            Dictionary<int, Resident> map = new Dictionary<int, Resident>();
            foreach (Resident resident in _residents) map[resident.ResidentId] = resident;
            return map;
        }

        private string NameOf(int residentId)
        {
            Resident resident = _residents.FirstOrDefault(r => r.ResidentId == residentId);
            return resident == null ? string.Empty : resident.GetFullName();
        }

        private bool PurokOf(int residentId, string purok)
        {
            Resident resident = _residents.FirstOrDefault(r => r.ResidentId == residentId);
            return resident != null && string.Equals(resident.Purok, purok, StringComparison.OrdinalIgnoreCase);
        }
    }
}
