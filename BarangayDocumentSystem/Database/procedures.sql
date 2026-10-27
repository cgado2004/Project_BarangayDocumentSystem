-- ============================================================================
--  Barangay Document System — stored procedures
--
--  Every query the app runs lives here. The C# code only CALLs these by name,
--  so SQL is kept in one place, in the database, and runs as a unit.
--
--  The app re-creates all of them each time it starts (DROP then CREATE), so
--  editing a procedure here and restarting the app is enough to update it.
--
--  Statements are separated by a line holding only $$ (a semicolon cannot be
--  the separator, because procedure bodies contain semicolons).
--  Parameter names start with p_ so they never clash with column names.
--  Parameters are listed in the order the C# code passes them.
-- ============================================================================

-- ---------------------------------------------------------------- residents

DROP PROCEDURE IF EXISTS sp_residents_get_all
$$
CREATE PROCEDURE sp_residents_get_all()
BEGIN
    SELECT resident_id, first_name, middle_name, last_name, suffix, date_of_birth, gender,
           civil_status, purok, house_number, address_line, contact_number, occupation, date_of_residency,
           is_registered_voter, classification, has_availed_jobseeker,
           is_archived, archived_on, transfer_destination, transfer_reason
    FROM residents
    ORDER BY resident_id;
END
$$

DROP PROCEDURE IF EXISTS sp_resident_add
$$
CREATE PROCEDURE sp_resident_add(
    IN p_first_name          VARCHAR(100),
    IN p_middle_name         VARCHAR(100),
    IN p_last_name           VARCHAR(100),
    IN p_suffix              VARCHAR(20),
    IN p_date_of_birth       DATE,
    IN p_gender              VARCHAR(20),
    IN p_civil_status        VARCHAR(20),
    IN p_purok               VARCHAR(100),
    IN p_house_number        VARCHAR(30),
    IN p_contact_number      VARCHAR(30),
    IN p_occupation          VARCHAR(100),
    IN p_date_of_residency   DATE,
    IN p_is_registered_voter TINYINT(1),
    IN p_classification      INT)
BEGIN
    INSERT INTO residents (first_name, middle_name, last_name, suffix, date_of_birth, gender,
                           civil_status, purok, house_number, contact_number, occupation,
                           date_of_residency, is_registered_voter, classification)
    VALUES (p_first_name, p_middle_name, p_last_name, p_suffix, p_date_of_birth, p_gender,
            p_civil_status, p_purok, p_house_number, p_contact_number, p_occupation,
            p_date_of_residency, p_is_registered_voter, p_classification);

    SELECT LAST_INSERT_ID() AS resident_id;
END
$$

DROP PROCEDURE IF EXISTS sp_resident_update
$$
CREATE PROCEDURE sp_resident_update(
    IN p_first_name          VARCHAR(100),
    IN p_middle_name         VARCHAR(100),
    IN p_last_name           VARCHAR(100),
    IN p_suffix              VARCHAR(20),
    IN p_date_of_birth       DATE,
    IN p_gender              VARCHAR(20),
    IN p_civil_status        VARCHAR(20),
    IN p_purok               VARCHAR(100),
    IN p_house_number        VARCHAR(30),
    IN p_contact_number      VARCHAR(30),
    IN p_occupation          VARCHAR(100),
    IN p_date_of_residency   DATE,
    IN p_is_registered_voter TINYINT(1),
    IN p_classification      INT,
    IN p_resident_id         INT)
BEGIN
    UPDATE residents
    SET first_name = p_first_name,
        middle_name = p_middle_name,
        last_name = p_last_name,
        suffix = p_suffix,
        date_of_birth = p_date_of_birth,
        gender = p_gender,
        civil_status = p_civil_status,
        purok = p_purok,
        house_number = p_house_number,
        contact_number = p_contact_number,
        occupation = p_occupation,
        date_of_residency = p_date_of_residency,
        is_registered_voter = p_is_registered_voter,
        classification = p_classification
    WHERE resident_id = p_resident_id;
END
$$

DROP PROCEDURE IF EXISTS sp_resident_archive
$$
CREATE PROCEDURE sp_resident_archive(
    IN p_archived_on  DATETIME,
    IN p_destination  VARCHAR(255),
    IN p_reason       VARCHAR(255),
    IN p_resident_id  INT)
BEGIN
    UPDATE residents
    SET is_archived = 1,
        archived_on = p_archived_on,
        transfer_destination = p_destination,
        transfer_reason = p_reason
    WHERE resident_id = p_resident_id;
END
$$

DROP PROCEDURE IF EXISTS sp_resident_restore
$$
CREATE PROCEDURE sp_resident_restore(IN p_resident_id INT)
BEGIN
    UPDATE residents
    SET is_archived = 0,
        archived_on = NULL,
        transfer_destination = '',
        transfer_reason = ''
    WHERE resident_id = p_resident_id;
END
$$

-- ----------------------------------------------------------------- requests

DROP PROCEDURE IF EXISTS sp_requests_get_all
$$
CREATE PROCEDURE sp_requests_get_all()
BEGIN
    SELECT request_id, resident_id, document_type, purpose, date_requested, date_released,
           status, fee, fee_basis, is_paid, official_receipt_no, date_paid, received_by, remarks
    FROM document_requests
    ORDER BY request_id;
END
$$

DROP PROCEDURE IF EXISTS sp_request_add
$$
CREATE PROCEDURE sp_request_add(
    IN p_resident_id    INT,
    IN p_document_type  VARCHAR(50),
    IN p_purpose        VARCHAR(500),
    IN p_date_requested DATETIME,
    IN p_status         VARCHAR(30),
    IN p_fee            DECIMAL(10,2),
    IN p_fee_basis      VARCHAR(255))
BEGIN
    INSERT INTO document_requests (resident_id, document_type, purpose, date_requested, status,
                                   fee, fee_basis, is_paid, official_receipt_no, remarks)
    VALUES (p_resident_id, p_document_type, p_purpose, p_date_requested, p_status,
            p_fee, p_fee_basis, 0, '', '');

    SELECT LAST_INSERT_ID() AS request_id;
END
$$

-- Saves a request AND the resident's RA 11261 flag in one transaction:
-- either both change or neither does.
DROP PROCEDURE IF EXISTS sp_request_save
$$
CREATE PROCEDURE sp_request_save(
    IN p_purpose              VARCHAR(500),
    IN p_status               VARCHAR(30),
    IN p_date_released        DATETIME,
    IN p_is_paid              TINYINT(1),
    IN p_official_receipt_no  VARCHAR(50),
    IN p_date_paid            DATETIME,
    IN p_received_by          VARCHAR(100),
    IN p_remarks              VARCHAR(500),
    IN p_request_id           INT,
    IN p_has_availed          TINYINT(1),
    IN p_resident_id          INT)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    UPDATE document_requests
    SET purpose = p_purpose,
        status = p_status,
        date_released = p_date_released,
        is_paid = p_is_paid,
        official_receipt_no = p_official_receipt_no,
        date_paid = p_date_paid,
        received_by = p_received_by,
        remarks = p_remarks
    WHERE request_id = p_request_id;

    UPDATE residents
    SET has_availed_jobseeker = p_has_availed
    WHERE resident_id = p_resident_id;

    COMMIT;
END
$$

-- -------------------------------------------------------------------- users

DROP PROCEDURE IF EXISTS sp_user_count
$$
CREATE PROCEDURE sp_user_count()
BEGIN
    SELECT COUNT(*) AS total FROM users;
END
$$

DROP PROCEDURE IF EXISTS sp_user_find
$$
-- The COLLATE makes the comparison work whatever collation the users table
-- was created with (MySQL 8 defaults to utf8mb4_0900_ai_ci, older setups to
-- utf8mb4_general_ci), which otherwise raises "Illegal mix of collations".
CREATE PROCEDURE sp_user_find(IN p_username VARCHAR(50) CHARACTER SET utf8mb4)
BEGIN
    SELECT user_id, username, full_name, password_hash, role, is_archived
    FROM users
    WHERE username COLLATE utf8mb4_unicode_ci = p_username COLLATE utf8mb4_unicode_ci;
END
$$

DROP PROCEDURE IF EXISTS sp_user_get
$$
CREATE PROCEDURE sp_user_get(IN p_user_id INT)
BEGIN
    SELECT user_id, username, full_name, password_hash, role, is_archived
    FROM users
    WHERE user_id = p_user_id;
END
$$

DROP PROCEDURE IF EXISTS sp_user_add
$$
CREATE PROCEDURE sp_user_add(
    IN p_username      VARCHAR(50),
    IN p_full_name     VARCHAR(100),
    IN p_password_hash VARCHAR(255),
    IN p_created_on    DATETIME,
    IN p_role          VARCHAR(20))
BEGIN
    INSERT INTO users (username, full_name, password_hash, created_on, role)
    VALUES (p_username, p_full_name, p_password_hash, p_created_on, p_role);
END
$$

DROP PROCEDURE IF EXISTS sp_user_list
$$
CREATE PROCEDURE sp_user_list()
BEGIN
    SELECT user_id, username, full_name, password_hash, role, is_archived
    FROM users
    ORDER BY created_on, user_id;
END
$$

DROP PROCEDURE IF EXISTS sp_user_update
$$
CREATE PROCEDURE sp_user_update(
    IN p_user_id   INT,
    IN p_username  VARCHAR(50),
    IN p_full_name VARCHAR(100),
    IN p_role      VARCHAR(20))
BEGIN
    UPDATE users
    SET username = p_username, full_name = p_full_name, role = p_role
    WHERE user_id = p_user_id;
END
$$

DROP PROCEDURE IF EXISTS sp_user_change_password
$$
CREATE PROCEDURE sp_user_change_password(
    IN p_user_id       INT,
    IN p_password_hash VARCHAR(255))
BEGIN
    UPDATE users SET password_hash = p_password_hash WHERE user_id = p_user_id;
END
$$

DROP PROCEDURE IF EXISTS sp_user_set_archived
$$
CREATE PROCEDURE sp_user_set_archived(IN p_user_id INT, IN p_archived TINYINT)
BEGIN
    UPDATE users SET is_archived = p_archived WHERE user_id = p_user_id;
END
$$

DROP PROCEDURE IF EXISTS sp_user_delete
$$
CREATE PROCEDURE sp_user_delete(IN p_user_id INT)
BEGIN
    DELETE FROM users WHERE user_id = p_user_id;
END
$$

-- ------------------------------------------------------------ login throttling

DROP PROCEDURE IF EXISTS sp_login_state
$$
CREATE PROCEDURE sp_login_state(IN p_username VARCHAR(50))
BEGIN
    SELECT failed_count, locked_until
    FROM login_attempts
    WHERE username COLLATE utf8mb4_unicode_ci = p_username COLLATE utf8mb4_unicode_ci;
END
$$

DROP PROCEDURE IF EXISTS sp_login_fail
$$
CREATE PROCEDURE sp_login_fail(IN p_username VARCHAR(50))
BEGIN
    DECLARE v_count  INT;
    DECLARE v_window DATETIME;

    SELECT failed_count, window_start INTO v_count, v_window
    FROM login_attempts
    WHERE username COLLATE utf8mb4_unicode_ci = p_username COLLATE utf8mb4_unicode_ci;

    IF v_count IS NULL THEN
        INSERT INTO login_attempts (username, failed_count, window_start, locked_until)
        VALUES (p_username, 1, NOW(), NULL);
        SET v_count = 1;
    ELSEIF TIMESTAMPDIFF(MINUTE, v_window, NOW()) > 15 THEN
        UPDATE login_attempts
        SET failed_count = 1, window_start = NOW(), locked_until = NULL
        WHERE username COLLATE utf8mb4_unicode_ci = p_username COLLATE utf8mb4_unicode_ci;
        SET v_count = 1;
    ELSE
        SET v_count = v_count + 1;
        UPDATE login_attempts
        SET failed_count = v_count
        WHERE username COLLATE utf8mb4_unicode_ci = p_username COLLATE utf8mb4_unicode_ci;
        IF v_count >= 5 THEN
            UPDATE login_attempts
            SET locked_until = DATE_ADD(NOW(), INTERVAL 30 SECOND)
            WHERE username COLLATE utf8mb4_unicode_ci = p_username COLLATE utf8mb4_unicode_ci;
        END IF;
    END IF;

    SELECT v_count AS failed_count,
           (SELECT locked_until FROM login_attempts
            WHERE username COLLATE utf8mb4_unicode_ci = p_username COLLATE utf8mb4_unicode_ci) AS locked_until;
END
$$

DROP PROCEDURE IF EXISTS sp_login_clear
$$
CREATE PROCEDURE sp_login_clear(IN p_username VARCHAR(50))
BEGIN
    DELETE FROM login_attempts
    WHERE username COLLATE utf8mb4_unicode_ci = p_username COLLATE utf8mb4_unicode_ci;
END
$$

-- ---------------------------------------------------------------- dependents

DROP PROCEDURE IF EXISTS sp_dependents_get_all
$$
CREATE PROCEDURE sp_dependents_get_all()
BEGIN
    SELECT dependent_id, resident_id, full_name, relationship, date_of_birth
    FROM dependents
    ORDER BY resident_id, dependent_id;
END
$$

DROP PROCEDURE IF EXISTS sp_dependents_clear
$$
CREATE PROCEDURE sp_dependents_clear(IN p_resident_id INT)
BEGIN
    DELETE FROM dependents WHERE resident_id = p_resident_id;
END
$$

DROP PROCEDURE IF EXISTS sp_dependent_add
$$
CREATE PROCEDURE sp_dependent_add(
    IN p_full_name     VARCHAR(150),
    IN p_relationship  VARCHAR(50),
    IN p_date_of_birth DATE,
    IN p_resident_id   INT)
BEGIN
    INSERT INTO dependents (resident_id, full_name, relationship, date_of_birth)
    VALUES (p_resident_id, p_full_name, p_relationship, p_date_of_birth);
END
$$

-- -------------------------------------------------------------- activity log

DROP PROCEDURE IF EXISTS sp_activity_add
$$
CREATE PROCEDURE sp_activity_add(
    IN p_logged_on DATETIME,
    IN p_username  VARCHAR(100),
    IN p_action    VARCHAR(60),
    IN p_details   VARCHAR(500))
BEGIN
    INSERT INTO activity_log (logged_on, username, action, details)
    VALUES (p_logged_on, p_username, p_action, p_details);
END
$$

DROP PROCEDURE IF EXISTS sp_activity_get_by_date
$$
CREATE PROCEDURE sp_activity_get_by_date(IN p_day DATE)
BEGIN
    SELECT logged_on, username, action, details
    FROM activity_log
    WHERE logged_on >= p_day AND logged_on < DATE_ADD(p_day, INTERVAL 1 DAY)
    ORDER BY logged_on DESC, log_id DESC;
END
$$