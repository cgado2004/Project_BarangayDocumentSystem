-- ============================================================================
--  02-procedures.sql  ( MySQL / MariaDB )
--  The stored procedures behind the dashboard, the reports and the queue.
--
--  Why they are here instead of only in C#: the summing and grouping happens
--  next to the data, so the program pulls back a dozen rows instead of the
--  whole table, and the barangay's own IT person can adjust a report in
--  phpMyAdmin without a rebuild.
--
--  ONE RULE WHEN ADDING A PROCEDURE: the parameters must be in the same order
--  as the arguments the repository passes, because MySQL is called with
--  "CALL procedure(@a, @b, ...)" (see DBHelper.BuildProcedure).
-- ============================================================================

DELIMITER $$

CREATE PROCEDURE sp_report_per_day_transactions(
    IN p_from DATETIME, IN p_to DATETIME, IN p_status VARCHAR(30), IN p_purok VARCHAR(100))
BEGIN
    SELECT DATE(r.date_requested) AS txn_date,
           COUNT(*) AS filed,
           SUM(CASE WHEN r.status = 'Released' THEN 1 ELSE 0 END) AS released,
           SUM(CASE WHEN r.status = 'Rejected' THEN 1 ELSE 0 END) AS rejected,
           SUM(CASE WHEN r.status = 'Pending' THEN 1 ELSE 0 END) AS waiting,
           SUM(CASE WHEN r.fee = 0 THEN 1 ELSE 0 END) AS free_issued,
           SUM(CASE WHEN r.is_paid = 1 THEN r.fee ELSE 0 END) AS collected
      FROM document_requests r
      JOIN residents res ON res.resident_id = r.resident_id
     WHERE r.date_requested >= p_from AND r.date_requested <= p_to
       AND (p_status = '' OR r.status = p_status)
       AND (p_purok = '' OR res.purok = p_purok)
     GROUP BY DATE(r.date_requested)
     ORDER BY txn_date;
END$$

CREATE PROCEDURE sp_report_document_register(
    IN p_from DATETIME, IN p_to DATETIME, IN p_status VARCHAR(30))
BEGIN
    SELECT r.reference_number, r.date_requested,
           res.first_name, res.last_name, res.purok,
           r.document_type, r.purpose, r.status,
           r.fee, r.is_paid, r.official_receipt_no, r.date_released
      FROM document_requests r
      JOIN residents res ON res.resident_id = r.resident_id
     WHERE r.date_requested >= p_from AND r.date_requested <= p_to
       AND (p_status = '' OR r.status = p_status)
     ORDER BY r.date_requested;
END$$

CREATE PROCEDURE sp_report_collections(IN p_from DATETIME, IN p_to DATETIME)
BEGIN
    SELECT or_date, or_number, series_code, control_number, payer_name,
           amount, method, collected_by, is_void, void_reason
      FROM official_receipts
     WHERE or_date >= p_from AND or_date <= p_to
     ORDER BY or_date, or_number;
END$$

CREATE PROCEDURE sp_report_business_clearances(IN p_from DATETIME, IN p_to DATETIME)
BEGIN
    SELECT r.reference_number, r.date_requested, r.business_name, r.business_nature,
           r.business_purok, r.business_ownership, r.business_employees,
           r.status, r.fee, r.is_paid, r.official_receipt_no,
           res.first_name, res.last_name
      FROM document_requests r
      JOIN residents res ON res.resident_id = r.resident_id
     WHERE r.document_type = 'BarangayBusinessClearance'
       AND r.date_requested >= p_from AND r.date_requested <= p_to
     ORDER BY r.date_requested;
END$$

CREATE PROCEDURE sp_report_census_summary(IN p_purok VARCHAR(100))
BEGIN
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
      FROM residents
     WHERE record_state = 'Active' AND (p_purok = '' OR purok = p_purok)
     GROUP BY purok
     ORDER BY purok;
END$$

CREATE PROCEDURE sp_report_population_by_purok(IN p_active TINYINT)
BEGIN
    SELECT purok,
           COUNT(*) AS residents,
           SUM(CASE WHEN is_head_of_family = 1 THEN 1 ELSE 0 END) AS households,
           SUM(CASE WHEN gender = 'Male' THEN 1 ELSE 0 END) AS male,
           SUM(CASE WHEN gender = 'Female' THEN 1 ELSE 0 END) AS female
      FROM residents
     WHERE (p_active = 0 OR record_state = 'Active')
     GROUP BY purok
     ORDER BY residents DESC;
END$$

CREATE PROCEDURE sp_report_population_by_age(
    IN p_purok VARCHAR(100), IN p_active TINYINT,
    IN p_b0 DATE, IN p_b1 DATE, IN p_b2 DATE, IN p_b3 DATE, IN p_b4 DATE, IN p_b5 DATE)
BEGIN
    SELECT purok,
           SUM(CASE WHEN date_of_birth > p_b0 THEN 1 ELSE 0 END) AS under_1,
           SUM(CASE WHEN date_of_birth <= p_b0 AND date_of_birth > p_b1 THEN 1 ELSE 0 END) AS age_1_5,
           SUM(CASE WHEN date_of_birth <= p_b1 AND date_of_birth > p_b2 THEN 1 ELSE 0 END) AS age_6_13,
           SUM(CASE WHEN date_of_birth <= p_b2 AND date_of_birth > p_b3 THEN 1 ELSE 0 END) AS age_14_18,
           SUM(CASE WHEN date_of_birth <= p_b3 AND date_of_birth > p_b4 THEN 1 ELSE 0 END) AS age_19_30,
           SUM(CASE WHEN date_of_birth <= p_b4 AND date_of_birth > p_b5 THEN 1 ELSE 0 END) AS age_31_59,
           SUM(CASE WHEN date_of_birth <= p_b5 THEN 1 ELSE 0 END) AS age_60_up
      FROM residents
     WHERE (p_purok = '' OR purok = p_purok)
       AND (p_active = 0 OR record_state = 'Active')
     GROUP BY purok
     ORDER BY purok;
END$$

CREATE PROCEDURE sp_report_resident_master(
    IN p_purok VARCHAR(100), IN p_state VARCHAR(20), IN p_residency VARCHAR(20))
BEGIN
    SELECT last_name, first_name, middle_name, suffix, purok, date_of_birth, gender,
           civil_status, contact_number, occupation, classification,
           is_student_fee_category, is_business_owner, is_head_of_family,
           residency_status, record_state, date_of_residency
      FROM residents
     WHERE (p_purok = '' OR purok = p_purok)
       AND (p_state = '' OR record_state = p_state)
       AND (p_residency = '' OR residency_status = p_residency)
     ORDER BY purok, last_name, first_name;
END$$

CREATE PROCEDURE sp_activity_log_search(
    IN p_from DATETIME, IN p_to DATETIME, IN p_username VARCHAR(50),
    IN p_module VARCHAR(30), IN p_keyword VARCHAR(100), IN p_max_rows INT)
BEGIN
    SELECT occurred_on, username, role, module, action, target_type, target_reference, details
      FROM activity_log
     WHERE occurred_on >= p_from AND occurred_on <= p_to
       AND (p_username = '' OR username = p_username)
       AND (p_module = '' OR module = p_module)
       AND (p_keyword = '' OR details LIKE CONCAT('%', p_keyword, '%')
            OR action LIKE CONCAT('%', p_keyword, '%'))
     ORDER BY occurred_on DESC
     LIMIT 500;
END$$

CREATE PROCEDURE sp_dashboard_summary(IN p_from DATETIME, IN p_to DATETIME)
BEGIN
    SELECT
        (SELECT COUNT(*) FROM residents WHERE record_state = 'Active')                              AS active_residents,
        (SELECT COUNT(*) FROM residents WHERE record_state = 'Inactive')                            AS inactive_residents,
        (SELECT COUNT(*) FROM residents WHERE record_state = 'Archived')                            AS archived_records,
        (SELECT COUNT(*) FROM residents WHERE is_head_of_family = 1 AND record_state = 'Active')     AS households,
        (SELECT COUNT(*) FROM dependents)                                                          AS dependents,
        (SELECT COUNT(*) FROM document_requests WHERE date_requested >= p_from AND date_requested <= p_to) AS filed_in_period,
        (SELECT COUNT(*) FROM document_requests WHERE status = 'Pending')                           AS waiting,
        (SELECT COUNT(*) FROM document_requests WHERE status = 'Processing')                        AS processing,
        (SELECT COUNT(*) FROM document_requests WHERE status = 'Cleared')                           AS cleared,
        (SELECT COUNT(*) FROM document_requests WHERE status = 'ReadyForRelease')                   AS ready,
        (SELECT COUNT(*) FROM document_requests WHERE date_released >= p_from AND date_released <= p_to)   AS released_in_period,
        (SELECT COALESCE(SUM(amount), 0) FROM official_receipts
          WHERE is_void = 0 AND or_date >= p_from AND or_date <= p_to)                             AS collected_in_period,
        (SELECT COUNT(*) FROM document_requests
          WHERE fee = 0 AND status = 'Released'
            AND date_released >= p_from AND date_released <= p_to)                                 AS free_issued,
        (SELECT COUNT(*) FROM activity_log WHERE occurred_on >= p_from AND occurred_on <= p_to)     AS activity_entries;
END$$

CREATE PROCEDURE sp_clear_waiting_requests(IN p_now DATETIME, IN p_changed_by VARCHAR(100))
BEGIN
    -- The 8:00 AM to 4:00 PM rule, expressed as one statement: every request
    -- that was filed outside office hours and needs no validation becomes
    -- Cleared once the office window has opened again. The program runs this
    -- on startup, so the queue is honest on a Monday morning.
    UPDATE document_requests
       SET status = 'Cleared',
           last_status_on = p_now,
           last_status_by = p_changed_by
     WHERE status = 'Pending'
       AND requires_validation = 0
       AND filed_during_office_window = 0
       AND date_requested < p_now;
END$$

DELIMITER ;
