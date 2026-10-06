-- ============================================================================
--  02-procedures.sql  ( Microsoft SQL Server )
--  The same stored procedures as the MySQL script, in T-SQL.
--
--  These are the queries behind the dashboard, the reports and the queue.
--  The parameter names match what the repository sends (@from, @to, @status,
--  @purok, ...) because SQL Server matches them by name.
--
--  "CREATE OR ALTER" needs SQL Server 2016 SP1 or newer, which covers every
--  version a barangay would install today (including LocalDB and Express).
-- ============================================================================

CREATE OR ALTER PROCEDURE dbo.sp_report_per_day_transactions
    @from DATETIME, @to DATETIME, @status NVARCHAR(30), @purok NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(r.date_requested AS date) AS txn_date,
           COUNT(*) AS filed,
           SUM(CASE WHEN r.status = 'Released' THEN 1 ELSE 0 END) AS released,
           SUM(CASE WHEN r.status = 'Rejected' THEN 1 ELSE 0 END) AS rejected,
           SUM(CASE WHEN r.status = 'Pending' THEN 1 ELSE 0 END) AS waiting,
           SUM(CASE WHEN r.fee = 0 THEN 1 ELSE 0 END) AS free_issued,
           SUM(CASE WHEN r.is_paid = 1 THEN r.fee ELSE 0 END) AS collected
      FROM dbo.document_requests r
      JOIN dbo.residents res ON res.resident_id = r.resident_id
     WHERE r.date_requested >= @from AND r.date_requested <= @to
       AND (@status = '' OR r.status = @status)
       AND (@purok = '' OR res.purok = @purok)
     GROUP BY CAST(r.date_requested AS date)
     ORDER BY txn_date;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_report_document_register
    @from DATETIME, @to DATETIME, @status NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.reference_number, r.date_requested,
           res.first_name, res.last_name, res.purok,
           r.document_type, r.purpose, r.status,
           r.fee, r.is_paid, r.official_receipt_no, r.date_released
      FROM dbo.document_requests r
      JOIN dbo.residents res ON res.resident_id = r.resident_id
     WHERE r.date_requested >= @from AND r.date_requested <= @to
       AND (@status = '' OR r.status = @status)
     ORDER BY r.date_requested;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_report_collections
    @from DATETIME, @to DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    SELECT or_date, or_number, series_code, control_number, payer_name,
           amount, method, collected_by, is_void, void_reason
      FROM dbo.official_receipts
     WHERE or_date >= @from AND or_date <= @to
     ORDER BY or_date, or_number;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_report_business_clearances
    @from DATETIME, @to DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.reference_number, r.date_requested, r.business_name, r.business_nature,
           r.business_purok, r.business_ownership, r.business_employees,
           r.status, r.fee, r.is_paid, r.official_receipt_no,
           res.first_name, res.last_name
      FROM dbo.document_requests r
      JOIN dbo.residents res ON res.resident_id = r.resident_id
     WHERE r.document_type = 'BarangayBusinessClearance'
       AND r.date_requested >= @from AND r.date_requested <= @to
     ORDER BY r.date_requested;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_report_census_summary
    @purok NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT purok,
           COUNT(*) AS residents,
           SUM(CASE WHEN is_head_of_family = 1 THEN 1 ELSE 0 END) AS households,
           SUM(CASE WHEN gender = 'Male' THEN 1 ELSE 0 END) AS male,
           SUM(CASE WHEN gender = 'Female' THEN 1 ELSE 0 END) AS female,
           SUM(CASE WHEN (classification & 1) = 1 THEN 1 ELSE 0 END) AS seniors,
           SUM(CASE WHEN (classification & 2) = 2 THEN 1 ELSE 0 END) AS pwd,
           SUM(CASE WHEN (classification & 4) = 4 THEN 1 ELSE 0 END) AS indigent,
           SUM(CASE WHEN (classification & 8) = 8 THEN 1 ELSE 0 END) AS solo_parent,
           SUM(CASE WHEN (classification & 16) = 16 THEN 1 ELSE 0 END) AS four_ps,
           SUM(CASE WHEN is_student_fee_category = 1 THEN 1 ELSE 0 END) AS students,
           SUM(CASE WHEN residency_status = 'Newcomer' THEN 1 ELSE 0 END) AS newcomers,
           SUM(CASE WHEN residency_status = 'Temporary' THEN 1 ELSE 0 END) AS temporary,
           SUM(CASE WHEN residency_status = 'Permanent' THEN 1 ELSE 0 END) AS permanent
      FROM dbo.residents
     WHERE record_state = 'Active' AND (@purok = '' OR purok = @purok)
     GROUP BY purok
     ORDER BY purok;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_report_population_by_purok
    @active BIT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT purok,
           COUNT(*) AS residents,
           SUM(CASE WHEN is_head_of_family = 1 THEN 1 ELSE 0 END) AS households,
           SUM(CASE WHEN gender = 'Male' THEN 1 ELSE 0 END) AS male,
           SUM(CASE WHEN gender = 'Female' THEN 1 ELSE 0 END) AS female
      FROM dbo.residents
     WHERE (@active = 0 OR record_state = 'Active')
     GROUP BY purok
     ORDER BY residents DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_report_population_by_age
    @purok NVARCHAR(100), @active BIT,
    @b0 DATE, @b1 DATE, @b2 DATE, @b3 DATE, @b4 DATE, @b5 DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT purok,
           SUM(CASE WHEN date_of_birth > @b0 THEN 1 ELSE 0 END) AS under_1,
           SUM(CASE WHEN date_of_birth <= @b0 AND date_of_birth > @b1 THEN 1 ELSE 0 END) AS age_1_5,
           SUM(CASE WHEN date_of_birth <= @b1 AND date_of_birth > @b2 THEN 1 ELSE 0 END) AS age_6_13,
           SUM(CASE WHEN date_of_birth <= @b2 AND date_of_birth > @b3 THEN 1 ELSE 0 END) AS age_14_18,
           SUM(CASE WHEN date_of_birth <= @b3 AND date_of_birth > @b4 THEN 1 ELSE 0 END) AS age_19_30,
           SUM(CASE WHEN date_of_birth <= @b4 AND date_of_birth > @b5 THEN 1 ELSE 0 END) AS age_31_59,
           SUM(CASE WHEN date_of_birth <= @b5 THEN 1 ELSE 0 END) AS age_60_up
      FROM dbo.residents
     WHERE (@purok = '' OR purok = @purok)
       AND (@active = 0 OR record_state = 'Active')
     GROUP BY purok
     ORDER BY purok;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_report_resident_master
    @purok NVARCHAR(100), @state NVARCHAR(20), @residency NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT last_name, first_name, middle_name, suffix, purok, date_of_birth, gender,
           civil_status, contact_number, occupation, classification,
           is_student_fee_category, is_business_owner, is_head_of_family,
           residency_status, record_state, date_of_residency
      FROM dbo.residents
     WHERE (@purok = '' OR purok = @purok)
       AND (@state = '' OR record_state = @state)
       AND (@residency = '' OR residency_status = @residency)
     ORDER BY purok, last_name, first_name;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_activity_log_search
    @from DATETIME, @to DATETIME, @username NVARCHAR(50),
    @module NVARCHAR(30), @keyword NVARCHAR(100), @max_rows INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (500) occurred_on, username, role, module, action, target_type,
           target_reference, details
      FROM dbo.activity_log
     WHERE occurred_on >= @from AND occurred_on <= @to
       AND (@username = '' OR username = @username)
       AND (@module = '' OR module = @module)
       AND (@keyword = '' OR details LIKE '%' + @keyword + '%' OR action LIKE '%' + @keyword + '%')
     ORDER BY occurred_on DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_dashboard_summary
    @from DATETIME, @to DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM dbo.residents WHERE record_state = 'Active')                            AS active_residents,
        (SELECT COUNT(*) FROM dbo.residents WHERE record_state = 'Inactive')                          AS inactive_residents,
        (SELECT COUNT(*) FROM dbo.residents WHERE record_state = 'Archived')                          AS archived_records,
        (SELECT COUNT(*) FROM dbo.residents WHERE is_head_of_family = 1 AND record_state = 'Active')  AS households,
        (SELECT COUNT(*) FROM dbo.dependents)                                                         AS dependents,
        (SELECT COUNT(*) FROM dbo.document_requests WHERE date_requested >= @from AND date_requested <= @to) AS filed_in_period,
        (SELECT COUNT(*) FROM dbo.document_requests WHERE status = 'Pending')                         AS waiting,
        (SELECT COUNT(*) FROM dbo.document_requests WHERE status = 'Processing')                      AS processing,
        (SELECT COUNT(*) FROM dbo.document_requests WHERE status = 'Cleared')                         AS cleared,
        (SELECT COUNT(*) FROM dbo.document_requests WHERE status = 'ReadyForRelease')                 AS ready,
        (SELECT COUNT(*) FROM dbo.document_requests WHERE date_released >= @from AND date_released <= @to) AS released_in_period,
        (SELECT COALESCE(SUM(amount), 0) FROM dbo.official_receipts
          WHERE is_void = 0 AND or_date >= @from AND or_date <= @to)                                  AS collected_in_period,
        (SELECT COUNT(*) FROM dbo.document_requests
          WHERE fee = 0 AND status = 'Released'
            AND date_released >= @from AND date_released <= @to)                                      AS free_issued,
        (SELECT COUNT(*) FROM dbo.activity_log WHERE occurred_on >= @from AND occurred_on <= @to)      AS activity_entries;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_clear_waiting_requests
    @now DATETIME, @changed_by NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- The 8:00 AM to 4:00 PM rule as one statement: a request filed outside
    -- office hours that needs no validation becomes Cleared once the window
    -- opens again.
    UPDATE dbo.document_requests
       SET status = 'Cleared',
           last_status_on = @now,
           last_status_by = @changed_by
     WHERE status = 'Pending'
       AND requires_validation = 0
       AND filed_during_office_window = 0
       AND date_requested < @now;
END
GO
