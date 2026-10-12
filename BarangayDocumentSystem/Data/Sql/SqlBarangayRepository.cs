// ---------------------------------------------------------------------------
//  SqlBarangayRepository.cs - the repository that talks to a real database.
//  Mine, in my own words.
//
//  This is a partial class. The rest of it lives in two neighbours:
//     SqlBarangayRepository.Records.cs  - receipts, users, the activity log
//     SqlBarangayRepository.Reports.cs  - the report tables and the health check
//  I split it that way because one file holding all of it would be a thousand
//  lines longer than anybody wants to scroll through.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Data.Sql
{
    /// <summary>
    /// The one repository that speaks SQL.
    ///
    /// I wrote it once instead of twice. MySQL and SQL Server differences are
    /// kept in the provider (how a connection is made, how the id of the row I
    /// just inserted is fetched) and in SqlText (how a date is trimmed) - and
    /// everywhere else the SQL is plain enough that both engines accept it
    /// unchanged. That means a bug I fix in the residents query is fixed for
    /// both engines, not for one of them.
    ///
    /// Ordinary work uses parameterised statements. The heavy reports call the
    /// stored procedures in Database/Scripts and quietly fall back to the same
    /// query in plain SQL if a procedure is missing on that machine.
    /// </summary>
    public partial class SqlBarangayRepository : IBarangayRepository
    {
        private readonly DBContext _context;
        private readonly DBHelper _db;
        private readonly string _engine;

        public SqlBarangayRepository(DBContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            _context = context;
            _db = context.Helper;
            _engine = context.Provider.Key;
        }

        public string Describe()
        {
            return _context.Description;
        }

        // ==================================================================
        //  Residents
        // ==================================================================

        public IList<Resident> GetResidents(ResidentQuery query)
        {
            if (query == null) query = new ResidentQuery();

            string sql = "SELECT " + SqlText.ResidentColumns + " FROM residents WHERE 1 = 1";
            SqlArguments arguments = new SqlArguments();

            if (!query.IncludeInactive)
                sql += " AND record_state = @state";
            else if (query.RecordState.HasValue)
                sql += " AND record_state = @state";

            if (query.RecordState.HasValue)
                arguments.Add("@state", query.RecordState.Value.ToString());

            if (!string.IsNullOrWhiteSpace(query.Purok))
            {
                sql += " AND purok = @purok";
                arguments.Add("@purok", query.Purok.Trim());
            }

            if (query.ResidencyStatus.HasValue)
            {
                sql += " AND residency_status = @residency";
                arguments.Add("@residency", query.ResidencyStatus.Value.ToString());
            }

            if (query.Classification.HasValue && query.Classification.Value != ResidentClassification.None)
            {
                // The classifications are stored as one number of bit flags,
                // so "has this tag" is a bitwise AND, not a text search.
                sql += " AND (classification & @flag) = @flag";
                arguments.Add("@flag", (int)query.Classification.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                sql += " AND (first_name LIKE @keyword OR middle_name LIKE @keyword " +
                       "OR last_name LIKE @keyword OR contact_number LIKE @keyword " +
                       "OR purok LIKE @keyword)";
                arguments.Add("@keyword", "%" + query.Keyword.Trim() + "%");
            }

            sql += " ORDER BY last_name, first_name";

            List<Resident> residents = _db.QueryList("loading the residents", sql, arguments, MapResident);

            // The dependents are loaded per resident only when that resident is
            // actually opened, because a list of two hundred people each with a
            // household would be two hundred extra round trips for a grid that
            // does not show them.
            return residents;
        }

        public Resident GetResident(int residentId)
        {
            const string sql = "SELECT " + SqlText.ResidentColumns +
                               " FROM residents WHERE resident_id = @id";

            Resident resident = _db.QuerySingle("loading the resident", sql,
                new SqlArguments().Add("@id", residentId), MapResident);

            if (resident != null)
            {
                foreach (Dependent dependent in GetDependents(resident.ResidentId))
                    resident.AddDependent(dependent);
            }

            return resident;
        }

        public IList<Resident> FindPossibleDuplicates(string firstName, string lastName,
                                                     DateTime dateOfBirth, int exceptResidentId)
        {
            const string sql = "SELECT " + SqlText.ResidentColumns + " FROM residents " +
                               "WHERE first_name = @first AND last_name = @last " +
                               "AND date_of_birth = @birth AND resident_id <> @id";

            return _db.QueryList("checking for a duplicate resident", sql,
                new SqlArguments()
                    .Add("@first", (firstName ?? string.Empty).Trim())
                    .Add("@last", (lastName ?? string.Empty).Trim())
                    .Add("@birth", dateOfBirth.Date)
                    .Add("@id", exceptResidentId),
                MapResident);
        }

        public int InsertResident(Resident resident)
        {
            const string sql =
                "INSERT INTO residents (" +
                "first_name, middle_name, last_name, suffix, date_of_birth, gender, civil_status, " +
                "purok, contact_number, occupation, date_of_residency, is_registered_voter, classification, " +
                "is_student_fee_category, is_business_owner, is_head_of_family, residency_status, record_state, " +
                "state_reason, state_changed_on, state_changed_by, has_availed_jobseeker, " +
                "created_on, created_by, updated_on, updated_by) VALUES (" +
                "@first_name, @middle_name, @last_name, @suffix, @date_of_birth, @gender, @civil_status, " +
                "@purok, @contact_number, @occupation, @date_of_residency, @voter, @classification, " +
                "@student, @business, @head, @residency, @state, " +
                "@state_reason, @state_changed_on, @state_changed_by, @jobseeker, " +
                "@created_on, @created_by, @updated_on, @updated_by)";

            SqlArguments arguments = ResidentArguments(resident, true);
            resident.ResidentId = InsertAndReturnId("saving the resident", sql, arguments);
            return resident.ResidentId;
        }

        public void UpdateResident(Resident resident)
        {
            const string sql =
                "UPDATE residents SET " +
                "first_name = @first_name, middle_name = @middle_name, last_name = @last_name, " +
                "suffix = @suffix, date_of_birth = @date_of_birth, gender = @gender, " +
                "civil_status = @civil_status, purok = @purok, contact_number = @contact_number, " +
                "occupation = @occupation, date_of_residency = @date_of_residency, " +
                "is_registered_voter = @voter, classification = @classification, " +
                "is_student_fee_category = @student, is_business_owner = @business, " +
                "is_head_of_family = @head, residency_status = @residency, record_state = @state, " +
                "state_reason = @state_reason, state_changed_on = @state_changed_on, " +
                "state_changed_by = @state_changed_by, has_availed_jobseeker = @jobseeker, " +
                "updated_on = @updated_on, updated_by = @updated_by " +
                "WHERE resident_id = @resident_id";

            SqlArguments arguments = ResidentArguments(resident, false);
            arguments.Add("@resident_id", resident.ResidentId);

            _db.ExecuteNonQuery("saving the resident", sql, arguments);
        }

        public void SetRecordState(int residentId, RecordState state, string reason,
                                   string changedBy, DateTime when)
        {
            const string sql =
                "UPDATE residents SET record_state = @state, state_reason = @reason, " +
                "state_changed_on = @when, state_changed_by = @by, updated_on = @when, updated_by = @by " +
                "WHERE resident_id = @id";

            _db.ExecuteNonQuery("changing the resident's status", sql, new SqlArguments()
                .Add("@state", state.ToString())
                .Add("@reason", reason ?? string.Empty)
                .Add("@when", when)
                .Add("@by", changedBy ?? string.Empty)
                .Add("@id", residentId));
        }

        public int CountResidents(bool activeOnly)
        {
            string sql = "SELECT COUNT(*) FROM residents";
            if (activeOnly) sql += " WHERE record_state = 'Active'";

            object value = _db.ExecuteScalar("counting the residents", sql, null);
            return value == null ? 0 : Convert.ToInt32(value);
        }

        // ==================================================================
        //  Dependents
        // ==================================================================

        public IList<Dependent> GetDependents(int headResidentId)
        {
            const string sql = "SELECT " + SqlText.DependentColumns + " FROM dependents " +
                               "WHERE head_resident_id = @id ORDER BY full_name";

            return _db.QueryList("loading the dependents", sql,
                new SqlArguments().Add("@id", headResidentId), MapDependent);
        }

        public int InsertDependent(Dependent dependent)
        {
            const string sql =
                "INSERT INTO dependents (head_resident_id, full_name, relation, date_of_birth, " +
                "is_studying, remarks, created_on, created_by) VALUES (" +
                "@head, @name, @relation, @birth, @studying, @remarks, @created_on, @created_by)";

            int id = InsertAndReturnId("saving the dependent", sql, DependentArguments(dependent));
            dependent.DependentId = id;
            return id;
        }

        public void UpdateDependent(Dependent dependent)
        {
            const string sql =
                "UPDATE dependents SET head_resident_id = @head, full_name = @name, relation = @relation, " +
                "date_of_birth = @birth, is_studying = @studying, remarks = @remarks " +
                "WHERE dependent_id = @id";

            SqlArguments arguments = DependentArguments(dependent);
            arguments.Add("@id", dependent.DependentId);

            _db.ExecuteNonQuery("saving the dependent", sql, arguments);
        }

        public void DeleteDependent(int dependentId)
        {
            _db.ExecuteNonQuery("removing the dependent", "DELETE FROM dependents WHERE dependent_id = @id",
                new SqlArguments().Add("@id", dependentId));
        }

        // ==================================================================
        //  Document requests
        // ==================================================================

        public IList<DocumentRequest> GetRequests(RequestQuery query)
        {
            if (query == null) query = new RequestQuery();

            string sql = SqlText.RequestSelect + " WHERE 1 = 1";
            SqlArguments arguments = new SqlArguments();

            // The per-day filter. I compare against the whole day, so choosing
            // "today" includes a request filed at 4:15 PM, not just midnight.
            if (query.From.HasValue)
            {
                sql += " AND r.date_requested >= @from";
                arguments.Add("@from", query.From.Value.Date);
            }

            if (query.To.HasValue)
            {
                sql += " AND r.date_requested <= @to";
                arguments.Add("@to", query.To.Value.Date.AddDays(1).AddSeconds(-1));
            }

            if (query.Status.HasValue)
            {
                sql += " AND r.status = @status";
                arguments.Add("@status", query.Status.Value.ToString());
            }

            if (query.DocumentType.HasValue)
            {
                sql += " AND r.document_type = @doctype";
                arguments.Add("@doctype", query.DocumentType.Value.ToString());
            }

            if (query.ResidentId.HasValue)
            {
                sql += " AND r.resident_id = @resident";
                arguments.Add("@resident", query.ResidentId.Value);
            }

            if (query.BusinessOnly)
                sql += " AND r.document_type = 'BarangayBusinessClearance'";

            if (!string.IsNullOrWhiteSpace(query.Purok))
            {
                sql += " AND res.purok = @purok";
                arguments.Add("@purok", query.Purok.Trim());
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                sql += " AND (r.reference_number LIKE @keyword OR res.first_name LIKE @keyword " +
                       "OR res.last_name LIKE @keyword OR r.purpose LIKE @keyword " +
                       "OR r.business_name LIKE @keyword)";
                arguments.Add("@keyword", "%" + query.Keyword.Trim() + "%");
            }

            sql += " ORDER BY r.date_requested DESC, r.request_id DESC";

            return _db.QueryList("loading the requests", sql, arguments, MapRequest);
        }

        public DocumentRequest GetRequest(int requestId)
        {
            string sql = SqlText.RequestSelect + " WHERE r.request_id = @id";

            DocumentRequest request = _db.QuerySingle("loading the request", sql,
                new SqlArguments().Add("@id", requestId), MapRequest);

            if (request != null) request.LoadHistory(GetRequestHistory(request.RequestId));
            return request;
        }

        private IList<RequestStatusChange> GetRequestHistory(int requestId)
        {
            const string sql = "SELECT status, changed_on, changed_by, reason FROM request_status_history " +
                               "WHERE request_id = @id ORDER BY changed_on, history_id";

            return _db.QueryList("loading the request history", sql,
                new SqlArguments().Add("@id", requestId),
                delegate (DbDataReader reader)
                {
                    RequestStatusChange change = new RequestStatusChange();
                    change.Status = RowReader.GetEnum(reader, "status", RequestStatus.Pending);
                    change.ChangedOn = RowReader.GetDate(reader, "changed_on");
                    change.ChangedBy = RowReader.GetString(reader, "changed_by");
                    change.Reason = RowReader.GetString(reader, "reason");
                    return change;
                });
        }

        public int InsertRequest(DocumentRequest request)
        {
            const string sql =
                "INSERT INTO document_requests (" +
                "reference_number, resident_id, document_type, purpose, date_requested, status, " +
                "fee, fee_basis, is_paid, official_receipt_no, or_control_number, payment_date, collected_by, " +
                "scope, assessed_amount, hours, gross_annual_income, detail, " +
                "apply_jobseeker_waiver, availed_under_jobseeker_act, requires_validation, " +
                "filed_during_office_window, business_name, business_nature, business_purok, " +
                "business_location, business_ownership, business_registration, business_previous_permit, " +
                "business_employees, business_is_renewal, date_released, released_by, received_by, " +
                "rejection_reason, remarks, last_status_on, last_status_by) VALUES (" +
                "@reference, @resident, @doctype, @purpose, @filed, @status, " +
                "@fee, @basis, @paid, @or, @orcontrol, @payment_date, @collected_by, " +
                "@scope, @assessed, @hours, @income, @detail, " +
                "@jobseeker_waiver, @jobseeker_used, @validation, " +
                "@in_window, @biz_name, @biz_nature, @biz_purok, " +
                "@biz_location, @biz_ownership, @biz_registration, @biz_permit, " +
                "@biz_employees, @biz_renewal, @released_on, @released_by, @received_by, " +
                "@rejection, @remarks, @last_on, @last_by)";

            int id = InsertAndReturnId("saving the request", sql, RequestArguments(request, true));
            request.RequestId = id;

            // The history is written with the request, in the same call path,
            // so the very first line of a request's story always exists.
            foreach (RequestStatusChange change in request.History)
            {
                if (change.ChangedOn == default(DateTime)) continue;
                AppendStatusHistory(request.RequestId, change);
            }

            return id;
        }

        public void UpdateRequest(DocumentRequest request)
        {
            const string sql =
                "UPDATE document_requests SET " +
                "reference_number = @reference, resident_id = @resident, document_type = @doctype, " +
                "purpose = @purpose, date_requested = @filed, status = @status, " +
                "fee = @fee, fee_basis = @basis, is_paid = @paid, official_receipt_no = @or, " +
                "or_control_number = @orcontrol, payment_date = @payment_date, collected_by = @collected_by, " +
                "scope = @scope, assessed_amount = @assessed, hours = @hours, gross_annual_income = @income, " +
                "detail = @detail, apply_jobseeker_waiver = @jobseeker_waiver, " +
                "availed_under_jobseeker_act = @jobseeker_used, requires_validation = @validation, " +
                "filed_during_office_window = @in_window, business_name = @biz_name, " +
                "business_nature = @biz_nature, business_purok = @biz_purok, " +
                "business_location = @biz_location, business_ownership = @biz_ownership, " +
                "business_registration = @biz_registration, business_previous_permit = @biz_permit, " +
                "business_employees = @biz_employees, business_is_renewal = @biz_renewal, " +
                "date_released = @released_on, released_by = @released_by, received_by = @received_by, " +
                "rejection_reason = @rejection, remarks = @remarks, " +
                "last_status_on = @last_on, last_status_by = @last_by " +
                "WHERE request_id = @request_id";

            SqlArguments arguments = RequestArguments(request, false);
            arguments.Add("@request_id", request.RequestId);

            _db.ExecuteNonQuery("saving the request", sql, arguments);
        }

        public void AppendStatusHistory(int requestId, RequestStatusChange change)
        {
            const string sql =
                "INSERT INTO request_status_history (request_id, status, changed_on, changed_by, reason) " +
                "VALUES (@request, @status, @on, @by, @reason)";

            _db.ExecuteNonQuery("writing the request history", sql, new SqlArguments()
                .Add("@request", requestId)
                .Add("@status", change.Status.ToString())
                .Add("@on", change.ChangedOn)
                .Add("@by", change.ChangedBy ?? string.Empty)
                .Add("@reason", change.Reason ?? string.Empty));
        }

        public IList<DocumentRequest> GetRequestsWaitingForWindow()
        {
            string sql = SqlText.RequestSelect +
                         " WHERE r.status = 'Pending' AND r.requires_validation = @no " +
                         "AND r.filed_during_office_window = @no ORDER BY r.date_requested";

            return _db.QueryList("finding the requests still waiting", sql,
                new SqlArguments().Add("@no", false), MapRequest);
        }

        /// <summary>
        /// Hands out the next counter number for the year, e.g. "2026-000124".
        ///
        /// I keep the running number in its own table and do the read and the
        /// increase inside one transaction, so two clerks saving at the same
        /// second can never hand the same number to two residents. This is the
        /// one place in the system where that could really happen, and it is
        /// the kind of mistake a resident notices.
        /// </summary>
        public string NextReferenceNumber(DateTime when)
        {
            int year = when.Year;
            int next = 0;

            _db.InTransaction<object>("taking the next reference number", delegate (DbTransaction transaction)
            {
                object current = ScalarInTransaction(transaction,
                    "SELECT last_number FROM reference_counters WHERE counter_year = @year",
                    new SqlArguments().Add("@year", year));

                if (current == null || current == DBNull.Value)
                {
                    next = 1;
                    ExecuteInTransaction(transaction,
                        "INSERT INTO reference_counters (counter_year, last_number) VALUES (@year, @number)",
                        new SqlArguments().Add("@year", year).Add("@number", next));
                }
                else
                {
                    next = Convert.ToInt32(current) + 1;
                    ExecuteInTransaction(transaction,
                        "UPDATE reference_counters SET last_number = @number WHERE counter_year = @year",
                        new SqlArguments().Add("@number", next).Add("@year", year));
                }

                return null;
            });

            return year.ToString("0000") + "-" + next.ToString("000000");
        }

        // ==================================================================
        //  Shared plumbing
        // ==================================================================

        /// <summary>Runs an INSERT and hands back the id the database gave the
        /// new row. Every table's key is generated by the database, so two
        /// machines can never both decide that this new resident is number 57.</summary>
        private int InsertAndReturnId(string operation, string sql, SqlArguments arguments)
        {
            return _db.InTransaction("saving the record", delegate (DbTransaction transaction)
            {
                ExecuteInTransaction(transaction, sql, arguments);

                object id = ScalarInTransaction(transaction, _context.Provider.LastInsertIdStatement, null);
                return id == null ? 0 : Convert.ToInt32(id);
            });
        }

        private int ExecuteInTransaction(DbTransaction transaction, string sql, SqlArguments arguments)
        {
            using (DbCommand command = _db.BuildOn(transaction.Connection, sql, arguments, CommandType.Text))
            {
                command.Transaction = transaction;
                return command.ExecuteNonQuery();
            }
        }

        private object ScalarInTransaction(DbTransaction transaction, string sql, SqlArguments arguments)
        {
            using (DbCommand command = _db.BuildOn(transaction.Connection, sql, arguments, CommandType.Text))
            {
                command.Transaction = transaction;
                object value = command.ExecuteScalar();
                return value == DBNull.Value ? null : value;
            }
        }

        /// <summary>True when the table answers. Used by the health check and
        /// by the initializer to decide whether the schema script was run.</summary>
        private bool TableExists(string tableName)
        {
            try
            {
                object value = _db.ExecuteScalar("checking the tables",
                    "SELECT COUNT(*) FROM " + tableName, null);
                return value != null;
            }
            catch (RepositoryException)
            {
                return false;
            }
        }

        private static SqlArguments ResidentArguments(Resident resident, bool isInsert)
        {
            SqlArguments arguments = new SqlArguments();
            arguments.Add("@first_name", resident.FirstName ?? string.Empty);
            arguments.Add("@middle_name", resident.MiddleName ?? string.Empty);
            arguments.Add("@last_name", resident.LastName ?? string.Empty);
            arguments.Add("@suffix", resident.Suffix ?? string.Empty);
            arguments.Add("@date_of_birth", resident.DateOfBirth.Date);
            arguments.Add("@gender", resident.Gender.ToString());
            arguments.Add("@civil_status", resident.CivilStatus.ToString());
            arguments.Add("@purok", resident.Purok ?? string.Empty);
            arguments.Add("@contact_number", resident.ContactNumber ?? string.Empty);
            arguments.Add("@occupation", resident.Occupation ?? string.Empty);
            arguments.Add("@date_of_residency", resident.DateOfResidency.Date);
            arguments.Add("@voter", resident.IsRegisteredVoter);
            arguments.Add("@classification", (int)resident.Classification);
            arguments.Add("@student", resident.IsStudentFeeCategory);
            arguments.Add("@business", resident.IsBusinessOwner);
            arguments.Add("@head", resident.IsHeadOfFamily);
            arguments.Add("@residency", resident.ResidencyStatus.ToString());
            arguments.Add("@state", resident.RecordState.ToString());
            arguments.Add("@state_reason", resident.StateReason ?? string.Empty);
            arguments.Add("@state_changed_on", (object)resident.StateChangedOn ?? DBNull.Value);
            arguments.Add("@state_changed_by", resident.StateChangedBy ?? string.Empty);
            arguments.Add("@jobseeker", resident.HasAvailedFirstTimeJobseeker);
            arguments.Add("@created_on", resident.CreatedOn == default(DateTime) ? DateTime.Now : resident.CreatedOn);
            arguments.Add("@created_by", resident.CreatedBy ?? string.Empty);
            arguments.Add("@updated_on", (object)resident.UpdatedOn ?? DBNull.Value);
            arguments.Add("@updated_by", resident.UpdatedBy ?? string.Empty);
            return arguments;
        }

        private static SqlArguments DependentArguments(Dependent dependent)
        {
            return new SqlArguments()
                .Add("@head", dependent.HeadResidentId)
                .Add("@name", dependent.FullName ?? string.Empty)
                .Add("@relation", dependent.Relation.ToString())
                .Add("@birth", dependent.DateOfBirth.Date)
                .Add("@studying", dependent.IsStudying)
                .Add("@remarks", dependent.Remarks ?? string.Empty)
                .Add("@created_on", dependent.CreatedOn == default(DateTime) ? DateTime.Now : dependent.CreatedOn)
                .Add("@created_by", dependent.CreatedBy ?? string.Empty);
        }

        private static SqlArguments RequestArguments(DocumentRequest request, bool isInsert)
        {
            BusinessDetails business = request.Business ?? new BusinessDetails();

            return new SqlArguments()
                .Add("@reference", request.ReferenceNumber ?? string.Empty)
                .Add("@resident", request.ResidentId)
                .Add("@doctype", request.DocumentType.ToString())
                .Add("@purpose", request.Purpose ?? string.Empty)
                .Add("@filed", request.DateRequested == default(DateTime) ? DateTime.Now : request.DateRequested)
                .Add("@status", request.Status.ToString())
                .Add("@fee", request.Fee)
                .Add("@basis", request.FeeBasis ?? string.Empty)
                .Add("@paid", request.IsPaid)
                .Add("@or", request.OfficialReceiptNumber ?? string.Empty)
                .Add("@orcontrol", request.OrControlNumber ?? string.Empty)
                .Add("@payment_date", (object)request.PaymentDate ?? DBNull.Value)
                .Add("@collected_by", request.CollectedBy ?? string.Empty)
                .Add("@scope", request.Scope.ToString())
                .Add("@assessed", request.AssessedAmount)
                .Add("@hours", request.Hours)
                .Add("@income", request.GrossAnnualIncome)
                .Add("@detail", request.Detail ?? string.Empty)
                .Add("@jobseeker_waiver", request.ApplyJobseekerWaiver)
                .Add("@jobseeker_used", request.AvailedUnderJobseekerAct)
                .Add("@validation", request.RequiresValidation)
                .Add("@in_window", request.FiledDuringOfficeWindow)
                .Add("@biz_name", business.BusinessName ?? string.Empty)
                .Add("@biz_nature", business.NatureOfBusiness ?? string.Empty)
                .Add("@biz_purok", business.Purok ?? string.Empty)
                .Add("@biz_location", business.LocationNote ?? string.Empty)
                .Add("@biz_ownership", business.OwnershipType ?? string.Empty)
                .Add("@biz_registration", business.RegistrationNumber ?? string.Empty)
                .Add("@biz_permit", business.PreviousPermitNumber ?? string.Empty)
                .Add("@biz_employees", business.EmployeeCount)
                .Add("@biz_renewal", business.IsRenewal)
                .Add("@released_on", (object)request.DateReleased ?? DBNull.Value)
                .Add("@released_by", request.ReleasedBy ?? string.Empty)
                .Add("@received_by", request.ReceivedBy ?? string.Empty)
                .Add("@rejection", request.RejectionReason ?? string.Empty)
                .Add("@remarks", request.Remarks ?? string.Empty)
                .Add("@last_on", request.LastStatusChangeOn == default(DateTime)
                    ? (object)request.DateRequested : request.LastStatusChangeOn)
                .Add("@last_by", request.LastStatusChangeBy ?? string.Empty);
        }

        /// <summary>
        /// Turns one row of the residents table into a Resident.
        ///
        /// I read every column through RowReader, which tolerates a missing or
        /// empty value, so an older row written before I added a column still
        /// loads instead of throwing in the middle of a search.
        /// </summary>
        private static Resident MapResident(DbDataReader reader)
        {
            Resident resident = new Resident();
            resident.ResidentId = RowReader.GetInt(reader, "resident_id");
            resident.FirstName = RowReader.GetString(reader, "first_name");
            resident.MiddleName = RowReader.GetString(reader, "middle_name");
            resident.LastName = RowReader.GetString(reader, "last_name");
            resident.Suffix = RowReader.GetString(reader, "suffix");
            resident.DateOfBirth = RowReader.GetDate(reader, "date_of_birth");
            resident.Gender = RowReader.GetEnum(reader, "gender", Gender.Male);
            resident.CivilStatus = RowReader.GetEnum(reader, "civil_status", CivilStatus.Single);
            resident.Purok = RowReader.GetString(reader, "purok");
            resident.ContactNumber = RowReader.GetString(reader, "contact_number");
            resident.Occupation = RowReader.GetString(reader, "occupation");
            resident.DateOfResidency = RowReader.GetDate(reader, "date_of_residency");
            resident.IsRegisteredVoter = RowReader.GetBool(reader, "is_registered_voter");
            resident.Classification = RowReader.GetFlags<ResidentClassification>(reader, "classification");
            resident.IsStudentFeeCategory = RowReader.GetBool(reader, "is_student_fee_category");
            resident.IsBusinessOwner = RowReader.GetBool(reader, "is_business_owner");
            resident.IsHeadOfFamily = RowReader.GetBool(reader, "is_head_of_family");
            resident.ResidencyStatus = RowReader.GetEnum(reader, "residency_status", ResidencyStatus.Newcomer);
            resident.RecordState = RowReader.GetEnum(reader, "record_state", RecordState.Active);
            resident.StateReason = RowReader.GetString(reader, "state_reason");
            resident.StateChangedOn = RowReader.GetNullableDate(reader, "state_changed_on");
            resident.StateChangedBy = RowReader.GetString(reader, "state_changed_by");
            resident.HasAvailedFirstTimeJobseeker = RowReader.GetBool(reader, "has_availed_jobseeker");
            resident.CreatedOn = RowReader.GetDate(reader, "created_on");
            resident.CreatedBy = RowReader.GetString(reader, "created_by");
            resident.UpdatedOn = RowReader.GetNullableDate(reader, "updated_on");
            resident.UpdatedBy = RowReader.GetString(reader, "updated_by");
            return resident;
        }

        private static Dependent MapDependent(DbDataReader reader)
        {
            Dependent dependent = new Dependent();
            dependent.DependentId = RowReader.GetInt(reader, "dependent_id");
            dependent.HeadResidentId = RowReader.GetInt(reader, "head_resident_id");
            dependent.FullName = RowReader.GetString(reader, "full_name");
            dependent.Relation = RowReader.GetEnum(reader, "relation", DependentRelation.Relative);
            dependent.DateOfBirth = RowReader.GetDate(reader, "date_of_birth");
            dependent.IsStudying = RowReader.GetBool(reader, "is_studying");
            dependent.Remarks = RowReader.GetString(reader, "remarks");
            dependent.CreatedOn = RowReader.GetDate(reader, "created_on");
            dependent.CreatedBy = RowReader.GetString(reader, "created_by");
            return dependent;
        }

        /// <summary>The requests grid shows the resident's name, so the join in
        /// SqlText.RequestSelect brings the name along and I copy it here.</summary>
        private static DocumentRequest MapRequest(DbDataReader reader)
        {
            DocumentRequest request = new DocumentRequest();
            request.RequestId = RowReader.GetInt(reader, "request_id");
            request.ReferenceNumber = RowReader.GetString(reader, "reference_number");
            request.ResidentId = RowReader.GetInt(reader, "resident_id");
            request.DocumentType = RowReader.GetEnum(reader, "document_type", DocumentType.BarangayClearance);
            request.Purpose = RowReader.GetString(reader, "purpose");
            request.DateRequested = RowReader.GetDate(reader, "date_requested");
            request.Status = RowReader.GetEnum(reader, "status", RequestStatus.Pending);
            request.Fee = RowReader.GetDecimal(reader, "fee");
            request.FeeBasis = RowReader.GetString(reader, "fee_basis");
            request.IsPaid = RowReader.GetBool(reader, "is_paid");
            request.OfficialReceiptNumber = RowReader.GetString(reader, "official_receipt_no");
            request.OrControlNumber = RowReader.GetString(reader, "or_control_number");
            request.PaymentDate = RowReader.GetNullableDate(reader, "payment_date");
            request.CollectedBy = RowReader.GetString(reader, "collected_by");
            request.Scope = RowReader.GetEnum(reader, "scope", ClearanceScope.Local);
            request.AssessedAmount = RowReader.GetDecimal(reader, "assessed_amount");
            request.Hours = RowReader.GetDecimal(reader, "hours");
            request.GrossAnnualIncome = RowReader.GetDecimal(reader, "gross_annual_income");
            request.Detail = RowReader.GetString(reader, "detail");
            request.ApplyJobseekerWaiver = RowReader.GetBool(reader, "apply_jobseeker_waiver");
            request.AvailedUnderJobseekerAct = RowReader.GetBool(reader, "availed_under_jobseeker_act");
            request.RequiresValidation = RowReader.GetBool(reader, "requires_validation");
            request.FiledDuringOfficeWindow = RowReader.GetBool(reader, "filed_during_office_window");
            request.DateReleased = RowReader.GetNullableDate(reader, "date_released");
            request.ReleasedBy = RowReader.GetString(reader, "released_by");
            request.ReceivedBy = RowReader.GetString(reader, "received_by");
            request.RejectionReason = RowReader.GetString(reader, "rejection_reason");
            request.Remarks = RowReader.GetString(reader, "remarks");
            request.LastStatusChangeOn = RowReader.GetDate(reader, "last_status_on");
            request.LastStatusChangeBy = RowReader.GetString(reader, "last_status_by");

            // The resident's name, brought along by the join.
            if (RowReader.HasColumn(reader, "first_name"))
            {
                string first = RowReader.GetString(reader, "first_name");
                string middle = RowReader.GetString(reader, "middle_name");
                string last = RowReader.GetString(reader, "last_name");
                string suffix = RowReader.GetString(reader, "suffix");

                string middleInitial = string.Empty;
                foreach (char c in middle)
                {
                    if (char.IsLetter(c))
                    {
                        middleInitial = " " + char.ToUpperInvariant(c) + ".";
                        break;
                    }
                }

                request.ResidentName = (first + middleInitial + " " + last +
                    (string.IsNullOrWhiteSpace(suffix) ? string.Empty : " " + suffix)).Trim();
            }

            if (request.DocumentType == DocumentType.BarangayBusinessClearance)
            {
                BusinessDetails business = new BusinessDetails();
                business.BusinessName = RowReader.GetString(reader, "business_name");
                business.NatureOfBusiness = RowReader.GetString(reader, "business_nature");
                business.Purok = RowReader.GetString(reader, "business_purok");
                business.LocationNote = RowReader.GetString(reader, "business_location");
                business.OwnershipType = RowReader.GetString(reader, "business_ownership");
                business.RegistrationNumber = RowReader.GetString(reader, "business_registration");
                business.PreviousPermitNumber = RowReader.GetString(reader, "business_previous_permit");
                business.EmployeeCount = RowReader.GetInt(reader, "business_employees");
                business.IsRenewal = RowReader.GetBool(reader, "business_is_renewal");
                request.Business = business;
            }

            return request;
        }
    }
}
