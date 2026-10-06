// ---------------------------------------------------------------------------
//  SqlBarangayRepository.Records.cs - the money, the accounts and the log.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Data.Sql
{
    public partial class SqlBarangayRepository
    {
        // ==================================================================
        //  Official receipts - the government OR the treasurer asks about
        // ==================================================================

        public IList<OfficialReceipt> GetReceipts(ReceiptQuery query)
        {
            if (query == null) query = new ReceiptQuery();

            string sql = "SELECT " + SqlText.ReceiptColumns + " FROM official_receipts WHERE 1 = 1";
            SqlArguments arguments = new SqlArguments();

            if (query.From.HasValue)
            {
                sql += " AND or_date >= @from";
                arguments.Add("@from", query.From.Value.Date);
            }

            if (query.To.HasValue)
            {
                sql += " AND or_date <= @to";
                arguments.Add("@to", query.To.Value.Date);
            }

            if (!query.IncludeVoid)
                sql += " AND is_void = @void";

            if (!query.IncludeVoid)
                arguments.Add("@void", false);

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                sql += " AND (or_number LIKE @keyword OR control_number LIKE @keyword " +
                       "OR payer_name LIKE @keyword OR series_code LIKE @keyword)";
                arguments.Add("@keyword", "%" + query.Keyword.Trim() + "%");
            }

            sql += " ORDER BY or_date DESC, receipt_id DESC";

            return _db.QueryList("loading the receipts", sql, arguments, MapReceipt);
        }

        public OfficialReceipt GetReceipt(int receiptId)
        {
            const string sql = "SELECT " + SqlText.ReceiptColumns +
                               " FROM official_receipts WHERE receipt_id = @id";

            return _db.QuerySingle("loading the receipt", sql,
                new SqlArguments().Add("@id", receiptId), MapReceipt);
        }

        public int InsertReceipt(OfficialReceipt receipt)
        {
            const string sql =
                "INSERT INTO official_receipts (or_number, series_code, control_number, or_date, payer_name, " +
                "amount, method, request_id, collected_by, remarks, is_void, void_reason, voided_on, " +
                "voided_by, created_on) VALUES (" +
                "@or, @series, @control, @date, @payer, @amount, @method, @request, @collector, @remarks, " +
                "@void, @void_reason, @voided_on, @voided_by, @created_on)";

            int id = InsertAndReturnId("saving the receipt", sql, ReceiptArguments(receipt));
            receipt.ReceiptId = id;
            return id;
        }

        public void UpdateReceipt(OfficialReceipt receipt)
        {
            const string sql =
                "UPDATE official_receipts SET or_number = @or, series_code = @series, " +
                "control_number = @control, or_date = @date, payer_name = @payer, amount = @amount, " +
                "method = @method, request_id = @request, collected_by = @collector, remarks = @remarks, " +
                "is_void = @void, void_reason = @void_reason, voided_on = @voided_on, voided_by = @voided_by " +
                "WHERE receipt_id = @id";

            SqlArguments arguments = ReceiptArguments(receipt);
            arguments.Add("@id", receipt.ReceiptId);

            _db.ExecuteNonQuery("saving the receipt", sql, arguments);
        }

        /// <summary>
        /// Is that OR number already used in that series?
        ///
        /// The database also has a unique key on the two columns, so this check
        /// is the friendly version ("that receipt number is already recorded")
        /// and the key is the guarantee.
        /// </summary>
        public bool ReceiptNumberExists(string seriesCode, string orNumber, int exceptReceiptId)
        {
            const string sql = "SELECT COUNT(*) FROM official_receipts " +
                               "WHERE series_code = @series AND or_number = @or AND receipt_id <> @id";

            object value = _db.ExecuteScalar("checking the receipt number", sql, new SqlArguments()
                .Add("@series", (seriesCode ?? string.Empty).Trim())
                .Add("@or", (orNumber ?? string.Empty).Trim())
                .Add("@id", exceptReceiptId));

            return value != null && Convert.ToInt32(value) > 0;
        }

        public IList<ReceiptSeries> GetReceiptSeries(bool activeOnly)
        {
            string sql = "SELECT " + SqlText.SeriesColumns + " FROM receipt_series";
            if (activeOnly) sql += " WHERE is_active = @active";

            SqlArguments arguments = new SqlArguments();
            if (activeOnly) arguments.Add("@active", true);

            sql += " ORDER BY series_code";

            return _db.QueryList("loading the receipt booklets", sql, arguments, MapSeries);
        }

        public int InsertReceiptSeries(ReceiptSeries series)
        {
            const string sql =
                "INSERT INTO receipt_series (series_code, control_from, control_to, issued_to, issued_on, " +
                "is_active, remarks) VALUES (@code, @from, @to, @issued_to, @issued_on, @active, @remarks)";

            int id = InsertAndReturnId("saving the receipt booklet", sql,
                new SqlArguments()
                    .Add("@code", (series.SeriesCode ?? string.Empty).Trim())
                    .Add("@from", (series.ControlFrom ?? string.Empty).Trim())
                    .Add("@to", (series.ControlTo ?? string.Empty).Trim())
                    .Add("@issued_to", series.IssuedTo ?? string.Empty)
                    .Add("@issued_on", series.IssuedOn.Date)
                    .Add("@active", series.IsActive)
                    .Add("@remarks", series.Remarks ?? string.Empty));

            series.SeriesId = id;
            return id;
        }

        public void UpdateReceiptSeries(ReceiptSeries series)
        {
            const string sql =
                "UPDATE receipt_series SET series_code = @code, control_from = @from, control_to = @to, " +
                "issued_to = @issued_to, issued_on = @issued_on, is_active = @active, remarks = @remarks " +
                "WHERE series_id = @id";

            _db.ExecuteNonQuery("saving the receipt booklet", sql, new SqlArguments()
                .Add("@code", (series.SeriesCode ?? string.Empty).Trim())
                .Add("@from", (series.ControlFrom ?? string.Empty).Trim())
                .Add("@to", (series.ControlTo ?? string.Empty).Trim())
                .Add("@issued_to", series.IssuedTo ?? string.Empty)
                .Add("@issued_on", series.IssuedOn.Date)
                .Add("@active", series.IsActive)
                .Add("@remarks", series.Remarks ?? string.Empty)
                .Add("@id", series.SeriesId));
        }

        public ReceiptSeries FindSeriesForControlNumber(string controlNumber)
        {
            if (string.IsNullOrWhiteSpace(controlNumber)) return null;

            // The control numbers are text with leading zeros, so comparing
            // them inside SQL would need padding rules that differ between the
            // two engines. The booklets are few - a dozen a year at most - so I
            // read the active ones and let the ReceiptSeries class do the
            // comparison, where I can also explain it in one place.
            IList<ReceiptSeries> series = GetReceiptSeries(false);
            foreach (ReceiptSeries item in series)
                if (item.IsActive && item.ContainsControlNumber(controlNumber)) return item;

            return null;
        }

        // ==================================================================
        //  User accounts
        // ==================================================================

        public IList<UserAccount> GetUsers()
        {
            const string sql = "SELECT " + SqlText.UserColumns + " FROM user_accounts " +
                               "ORDER BY full_name";

            return _db.QueryList("loading the user accounts", sql, null, MapUser);
        }

        public UserAccount GetUser(int userId)
        {
            const string sql = "SELECT " + SqlText.UserColumns + " FROM user_accounts WHERE user_id = @id";

            return _db.QuerySingle("loading the user account", sql,
                new SqlArguments().Add("@id", userId), MapUser);
        }

        public UserAccount GetUserByUsername(string username)
        {
            const string sql = "SELECT " + SqlText.UserColumns + " FROM user_accounts WHERE username = @name";

            return _db.QuerySingle("loading the user account", sql,
                new SqlArguments().Add("@name", (username ?? string.Empty).Trim()), MapUser);
        }

        public bool UsernameExists(string username, int exceptUserId)
        {
            const string sql = "SELECT COUNT(*) FROM user_accounts WHERE username = @name AND user_id <> @id";

            object value = _db.ExecuteScalar("checking the user name", sql, new SqlArguments()
                .Add("@name", (username ?? string.Empty).Trim())
                .Add("@id", exceptUserId));

            return value != null && Convert.ToInt32(value) > 0;
        }

        public int CountActiveAdministrators()
        {
            object value = _db.ExecuteScalar("counting the administrators",
                "SELECT COUNT(*) FROM user_accounts WHERE role = 'Administrator' AND is_active = @active",
                new SqlArguments().Add("@active", true));

            return value == null ? 0 : Convert.ToInt32(value);
        }

        public int InsertUser(UserAccount user)
        {
            const string sql =
                "INSERT INTO user_accounts (username, full_name, role, position, password_hash, " +
                "password_salt, hash_iterations, must_change_password, is_active, failed_attempts, " +
                "locked_until, created_on, created_by) VALUES (" +
                "@username, @full_name, @role, @position, @hash, @salt, @iterations, @must_change, " +
                "@active, 0, NULL, @created_on, @created_by)";

            int id = InsertAndReturnId("saving the user account", sql, new SqlArguments()
                .Add("@username", (user.Username ?? string.Empty).Trim())
                .Add("@full_name", user.FullName ?? string.Empty)
                .Add("@role", user.Role.ToString())
                .Add("@position", user.Position ?? string.Empty)
                .Add("@hash", user.PasswordHash ?? string.Empty)
                .Add("@salt", user.PasswordSalt ?? string.Empty)
                .Add("@iterations", user.HashIterations)
                .Add("@must_change", user.MustChangePassword)
                .Add("@active", user.IsActive)
                .Add("@created_on", user.CreatedOn == default(DateTime) ? DateTime.Now : user.CreatedOn)
                .Add("@created_by", user.CreatedBy ?? string.Empty));

            user.UserId = id;
            return id;
        }

        public void UpdateUser(UserAccount user)
        {
            const string sql =
                "UPDATE user_accounts SET username = @username, full_name = @full_name, role = @role, " +
                "position = @position, must_change_password = @must_change, is_active = @active, " +
                "updated_on = @updated_on, updated_by = @updated_by WHERE user_id = @id";

            _db.ExecuteNonQuery("saving the user account", sql, new SqlArguments()
                .Add("@username", (user.Username ?? string.Empty).Trim())
                .Add("@full_name", user.FullName ?? string.Empty)
                .Add("@role", user.Role.ToString())
                .Add("@position", user.Position ?? string.Empty)
                .Add("@must_change", user.MustChangePassword)
                .Add("@active", user.IsActive)
                .Add("@updated_on", user.UpdatedOn.HasValue ? (object)user.UpdatedOn.Value : DateTime.Now)
                .Add("@updated_by", user.UpdatedBy ?? string.Empty)
                .Add("@id", user.UserId));
        }

        public void UpdatePassword(int userId, string hash, string salt, int iterations,
                                  bool mustChange, string changedBy, DateTime when)
        {
            const string sql =
                "UPDATE user_accounts SET password_hash = @hash, password_salt = @salt, " +
                "hash_iterations = @iterations, must_change_password = @must_change, " +
                "failed_attempts = 0, locked_until = NULL, updated_on = @when, updated_by = @by " +
                "WHERE user_id = @id";

            _db.ExecuteNonQuery("saving the new password", sql, new SqlArguments()
                .Add("@hash", hash ?? string.Empty)
                .Add("@salt", salt ?? string.Empty)
                .Add("@iterations", iterations)
                .Add("@must_change", mustChange)
                .Add("@when", when)
                .Add("@by", changedBy ?? string.Empty)
                .Add("@id", userId));
        }

        public void UpdateLoginState(UserAccount user)
        {
            const string sql =
                "UPDATE user_accounts SET failed_attempts = @attempts, locked_until = @locked, " +
                "last_login_on = @last_login, last_login_machine = @machine WHERE user_id = @id";

            _db.ExecuteNonQuery("recording the sign-in", sql, new SqlArguments()
                .Add("@attempts", user.FailedAttempts)
                .Add("@locked", (object)user.LockedUntil ?? DBNull.Value)
                .Add("@last_login", (object)user.LastLoginOn ?? DBNull.Value)
                .Add("@machine", user.LastLoginMachine ?? string.Empty)
                .Add("@id", user.UserId));
        }

        // ==================================================================
        //  Activity log
        // ==================================================================

        public void AppendActivityLog(ActivityLogEntry entry)
        {
            const string sql =
                "INSERT INTO activity_log (occurred_on, username, role, module, action, target_type, " +
                "target_reference, details, machine_name) VALUES (" +
                "@on, @username, @role, @module, @action, @target_type, @target_reference, @details, @machine)";

            _db.ExecuteNonQuery("writing the activity log", sql, LogArguments(entry));
        }

        public IList<ActivityLogEntry> GetActivityLog(ActivityLogQuery query)
        {
            if (query == null) query = new ActivityLogQuery();

            string sql = "SELECT " + SqlText.ActivityColumns + " FROM activity_log WHERE 1 = 1";
            SqlArguments arguments = new SqlArguments();

            if (query.From.HasValue)
            {
                sql += " AND occurred_on >= @from";
                arguments.Add("@from", query.From.Value.Date);
            }

            if (query.To.HasValue)
            {
                sql += " AND occurred_on <= @to";
                arguments.Add("@to", query.To.Value.Date.AddDays(1).AddSeconds(-1));
            }

            if (!string.IsNullOrWhiteSpace(query.Username))
            {
                sql += " AND username = @username";
                arguments.Add("@username", query.Username.Trim());
            }

            if (query.Module.HasValue)
            {
                sql += " AND module = @module";
                arguments.Add("@module", query.Module.Value.ToString());
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                sql += " AND (action LIKE @keyword OR details LIKE @keyword " +
                       "OR target_reference LIKE @keyword OR username LIKE @keyword)";
                arguments.Add("@keyword", "%" + query.Keyword.Trim() + "%");
            }

            sql += " ORDER BY occurred_on DESC, log_id DESC";

            // I cap the rows so a log with a year of history cannot fill the
            // screen's memory. The admin can narrow the dates instead.
            if (query.MaximumRows > 0)
                sql += " " + LimitClause(query.MaximumRows);

            return _db.QueryList("loading the activity log", sql, arguments, MapActivity);
        }

        public int CountActivityForDay(DateTime day)
        {
            object value = _db.ExecuteScalar("counting today's activity",
                "SELECT COUNT(*) FROM activity_log WHERE occurred_on >= @from AND occurred_on <= @to",
                new SqlArguments()
                    .Add("@from", day.Date)
                    .Add("@to", day.Date.AddDays(1).AddSeconds(-1)));

            return value == null ? 0 : Convert.ToInt32(value);
        }

        /// <summary>The activity log as a table, which is what the report and
        /// the export use. The stored procedure does the filtering when it is
        /// installed; the plain query is the fallback.</summary>
        public DataTable GetActivityLogTable(ActivityLogQuery query)
        {
            if (query == null) query = new ActivityLogQuery();

            SqlArguments arguments = new SqlArguments()
                .Add("@from", (query.From.HasValue ? query.From.Value.Date : DateTime.Today.AddMonths(-1)))
                .Add("@to", (query.To.HasValue ? query.To.Value.Date.AddDays(1).AddSeconds(-1) : DateTime.Now))
                .Add("@username", query.Username ?? string.Empty)
                .Add("@module", query.Module.HasValue ? query.Module.Value.ToString() : string.Empty)
                .Add("@keyword", string.IsNullOrWhiteSpace(query.Keyword) ? string.Empty : query.Keyword.Trim())
                .Add("@max_rows", query.MaximumRows <= 0 ? 500 : query.MaximumRows);

            string fallback =
                "SELECT occurred_on, username, role, module, action, target_type, target_reference, details " +
                "FROM activity_log WHERE occurred_on >= @from AND occurred_on <= @to " +
                "AND (@username = '' OR username = @username) " +
                "AND (@module = '' OR module = @module) " +
                "AND (@keyword = '' OR details LIKE @keyword OR action LIKE @keyword) " +
                "ORDER BY occurred_on DESC";

            return ReportTable("loading the activity log report",
                StoredProcedures.ActivityLogSearch, arguments, fallback);
        }

        // ==================================================================
        //  Mapping and arguments
        // ==================================================================

        private static SqlArguments ReceiptArguments(OfficialReceipt receipt)
        {
            return new SqlArguments()
                .Add("@or", (receipt.OrNumber ?? string.Empty).Trim())
                .Add("@series", (receipt.SeriesCode ?? string.Empty).Trim())
                .Add("@control", (receipt.ControlNumber ?? string.Empty).Trim())
                .Add("@date", receipt.OrDate.Date)
                .Add("@payer", receipt.PayerName ?? string.Empty)
                .Add("@amount", receipt.Amount)
                .Add("@method", receipt.Method.ToString())
                .Add("@request", receipt.RequestId.HasValue ? (object)receipt.RequestId.Value : DBNull.Value)
                .Add("@collector", receipt.CollectedBy ?? string.Empty)
                .Add("@remarks", receipt.Remarks ?? string.Empty)
                .Add("@void", receipt.IsVoid)
                .Add("@void_reason", receipt.VoidReason ?? string.Empty)
                .Add("@voided_on", (object)receipt.VoidedOn ?? DBNull.Value)
                .Add("@voided_by", receipt.VoidedBy ?? string.Empty)
                .Add("@created_on", receipt.CreatedOn == default(DateTime) ? DateTime.Now : receipt.CreatedOn);
        }

        private static SqlArguments LogArguments(ActivityLogEntry entry)
        {
            return new SqlArguments()
                .Add("@on", entry.OccurredOn == default(DateTime) ? DateTime.Now : entry.OccurredOn)
                .Add("@username", entry.Username ?? string.Empty)
                .Add("@role", entry.Role.ToString())
                .Add("@module", entry.Module.ToString())
                .Add("@action", entry.Action ?? string.Empty)
                .Add("@target_type", entry.TargetType ?? string.Empty)
                .Add("@target_reference", entry.TargetReference ?? string.Empty)
                .Add("@details", Trim(entry.Details, 500))
                .Add("@machine", entry.MachineName ?? string.Empty);
        }

        private static string Trim(string text, int maximum)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length <= maximum ? text : text.Substring(0, maximum);
        }

        /// <summary>Keeps a long log sentence from breaking the insert. A log
        /// line that cannot be written is worse than a shortened one.</summary>
        private static OfficialReceipt MapReceipt(DbDataReader reader)
        {
            OfficialReceipt receipt = new OfficialReceipt();
            receipt.ReceiptId = RowReader.GetInt(reader, "receipt_id");
            receipt.OrNumber = RowReader.GetString(reader, "or_number");
            receipt.SeriesCode = RowReader.GetString(reader, "series_code");
            receipt.ControlNumber = RowReader.GetString(reader, "control_number");
            receipt.OrDate = RowReader.GetDate(reader, "or_date");
            receipt.PayerName = RowReader.GetString(reader, "payer_name");
            receipt.Amount = RowReader.GetDecimal(reader, "amount");
            receipt.Method = RowReader.GetEnum(reader, "method", PaymentMethod.Cash);
            receipt.RequestId = RowReader.GetNullableInt(reader, "request_id");
            receipt.CollectedBy = RowReader.GetString(reader, "collected_by");
            receipt.Remarks = RowReader.GetString(reader, "remarks");
            receipt.IsVoid = RowReader.GetBool(reader, "is_void");
            receipt.VoidReason = RowReader.GetString(reader, "void_reason");
            receipt.VoidedOn = RowReader.GetNullableDate(reader, "voided_on");
            receipt.VoidedBy = RowReader.GetString(reader, "voided_by");
            receipt.CreatedOn = RowReader.GetDate(reader, "created_on");
            return receipt;
        }

        private static ReceiptSeries MapSeries(DbDataReader reader)
        {
            ReceiptSeries series = new ReceiptSeries();
            series.SeriesId = RowReader.GetInt(reader, "series_id");
            series.SeriesCode = RowReader.GetString(reader, "series_code");
            series.ControlFrom = RowReader.GetString(reader, "control_from");
            series.ControlTo = RowReader.GetString(reader, "control_to");
            series.IssuedTo = RowReader.GetString(reader, "issued_to");
            series.IssuedOn = RowReader.GetDate(reader, "issued_on");
            series.IsActive = RowReader.GetBool(reader, "is_active");
            series.Remarks = RowReader.GetString(reader, "remarks");
            return series;
        }

        private static UserAccount MapUser(DbDataReader reader)
        {
            UserAccount user = new UserAccount();
            user.UserId = RowReader.GetInt(reader, "user_id");
            user.Username = RowReader.GetString(reader, "username");
            user.FullName = RowReader.GetString(reader, "full_name");
            user.Role = RowReader.GetEnum(reader, "role", UserRole.Clerk);
            user.Position = RowReader.GetString(reader, "position");
            user.PasswordHash = RowReader.GetString(reader, "password_hash");
            user.PasswordSalt = RowReader.GetString(reader, "password_salt");
            user.HashIterations = RowReader.GetInt(reader, "hash_iterations");
            user.MustChangePassword = RowReader.GetBool(reader, "must_change_password");
            user.IsActive = RowReader.GetBool(reader, "is_active");
            user.FailedAttempts = RowReader.GetInt(reader, "failed_attempts");
            user.LockedUntil = RowReader.GetNullableDate(reader, "locked_until");
            user.LastLoginOn = RowReader.GetNullableDate(reader, "last_login_on");
            user.LastLoginMachine = RowReader.GetString(reader, "last_login_machine");
            user.CreatedOn = RowReader.GetDate(reader, "created_on");
            user.CreatedBy = RowReader.GetString(reader, "created_by");
            user.UpdatedOn = RowReader.GetNullableDate(reader, "updated_on");
            user.UpdatedBy = RowReader.GetString(reader, "updated_by");
            return user;
        }

        private static ActivityLogEntry MapActivity(DbDataReader reader)
        {
            ActivityLogEntry entry = new ActivityLogEntry();
            entry.LogId = RowReader.GetLong(reader, "log_id");
            entry.OccurredOn = RowReader.GetDate(reader, "occurred_on");
            entry.Username = RowReader.GetString(reader, "username");
            entry.Role = RowReader.GetEnum(reader, "role", UserRole.Clerk);
            entry.Module = RowReader.GetEnum(reader, "module", ActivityModule.Security);
            entry.Action = RowReader.GetString(reader, "action");
            entry.TargetType = RowReader.GetString(reader, "target_type");
            entry.TargetReference = RowReader.GetString(reader, "target_reference");
            entry.Details = RowReader.GetString(reader, "details");
            entry.MachineName = RowReader.GetString(reader, "machine_name");
            return entry;
        }
    }
}
