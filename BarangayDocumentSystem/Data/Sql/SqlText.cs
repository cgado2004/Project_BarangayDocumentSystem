// ---------------------------------------------------------------------------
//  SqlText.cs - the SQL I write once and use everywhere.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;

namespace BarangayDocumentSystem.Data.Sql
{
    /// <summary>
    /// One place for the SQL text that would otherwise be typed again in every
    /// method: the column lists and the little bits where MySQL and SQL Server
    /// disagree.
    ///
    /// Keeping the column lists here means a column name is spelled correctly
    /// in exactly one place. I have already lost an afternoon to a query that
    /// said "official_reciept_no" in one method and "official_receipt_no" in
    /// the next.
    /// </summary>
    internal static class SqlText
    {
        // ==================================================================
        //  Column lists
        // ==================================================================

        public const string ResidentColumns =
            "resident_id, first_name, middle_name, last_name, suffix, " +
            "date_of_birth, gender, civil_status, purok, contact_number, occupation, " +
            "date_of_residency, is_registered_voter, classification, " +
            "is_student_fee_category, is_business_owner, is_head_of_family, " +
            "residency_status, record_state, state_reason, state_changed_on, state_changed_by, " +
            "has_availed_jobseeker, created_on, created_by, updated_on, updated_by";

        public const string DependentColumns =
            "dependent_id, head_resident_id, full_name, relation, date_of_birth, " +
            "is_studying, remarks, created_on, created_by";

        public const string RequestColumns =
            "request_id, reference_number, resident_id, document_type, purpose, date_requested, status, " +
            "fee, fee_basis, is_paid, official_receipt_no, or_control_number, payment_date, collected_by, " +
            "scope, assessed_amount, hours, gross_annual_income, detail, " +
            "apply_jobseeker_waiver, availed_under_jobseeker_act, " +
            "requires_validation, filed_during_office_window, " +
            "business_name, business_nature, business_purok, business_location, business_ownership, " +
            "business_registration, business_previous_permit, business_employees, business_is_renewal, " +
            "date_released, released_by, received_by, rejection_reason, remarks, " +
            "last_status_on, last_status_by";

        public const string ReceiptColumns =
            "receipt_id, or_number, series_code, control_number, or_date, payer_name, amount, method, " +
            "request_id, collected_by, remarks, is_void, void_reason, voided_on, voided_by, created_on";

        public const string SeriesColumns =
            "series_id, series_code, control_from, control_to, issued_to, issued_on, is_active, remarks";

        public const string UserColumns =
            "user_id, username, full_name, role, position, password_hash, password_salt, hash_iterations, " +
            "must_change_password, is_active, failed_attempts, locked_until, last_login_on, last_login_machine, " +
            "created_on, created_by, updated_on, updated_by";

        public const string ActivityColumns =
            "log_id, occurred_on, username, role, module, action, target_type, target_reference, " +
            "details, machine_name";

        // ==================================================================
        //  Resident select with the household head's name for the grid
        // ==================================================================

        /// <summary>The requests grid shows the resident's name next to the
        /// document, so the join lives here rather than being retyped in the
        /// list query, the print query and the report query.</summary>
        public const string RequestSelect =
            "SELECT r." + "request_id, r.reference_number, r.resident_id, r.document_type, r.purpose, " +
            "r.date_requested, r.status, r.fee, r.fee_basis, r.is_paid, r.official_receipt_no, " +
            "r.or_control_number, r.payment_date, r.collected_by, r.scope, r.assessed_amount, r.hours, " +
            "r.gross_annual_income, r.detail, r.apply_jobseeker_waiver, r.availed_under_jobseeker_act, " +
            "r.requires_validation, r.filed_during_office_window, r.business_name, r.business_nature, " +
            "r.business_purok, r.business_location, r.business_ownership, r.business_registration, " +
            "r.business_previous_permit, r.business_employees, r.business_is_renewal, " +
            "r.date_released, r.released_by, r.received_by, r.rejection_reason, r.remarks, " +
            "r.last_status_on, r.last_status_by, " +
            "res.first_name, res.middle_name, res.last_name, res.suffix, res.purok " +
            "FROM document_requests r INNER JOIN residents res ON res.resident_id = r.resident_id";

        // ==================================================================
        //  Dialect bits
        // ==================================================================

        /// <summary>
        /// The expression that returns just the date part of a timestamp.
        ///
        /// MySQL and SQL Server spell this differently and both refuse the
        /// other's version, which is the only place in my hand-written SQL
        /// where I need to know which engine is under me. The stored
        /// procedures do their own grouping, so this is only used by the
        /// fallback queries when a procedure is missing.
        /// </summary>
        public static string DateOnly(string providerKey, string columnExpression)
        {
            if (string.Equals(providerKey, "SqlServer", StringComparison.OrdinalIgnoreCase))
                return "CAST(" + columnExpression + " AS date)";

            return "DATE(" + columnExpression + ")";
        }

        /// <summary>How each engine writes "pick the first row". I try not to
        /// need it, but the duplicate check does.</summary>
        public static string TopOne(string providerKey)
        {
            return string.Equals(providerKey, "SqlServer", StringComparison.OrdinalIgnoreCase)
                ? "SELECT TOP 1 "
                : "SELECT ";
        }

        public static string LimitOne(string providerKey)
        {
            return string.Equals(providerKey, "SqlServer", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : " LIMIT 1";
        }

        /// <summary>The "is this true" comparison. Both engines accept the
        /// parameter directly, so this simply keeps the intent readable.</summary>
        public static string BooleanColumn(string providerKey, string column)
        {
            return column + " = @value";
        }
    }
}
