-- ============================================================================
--  01-schema.sql  ( MySQL / MariaDB )
--  The tables of the Barangay Document System.
--
--  The program runs this itself on the first start (DatabaseInitializer), so
--  nobody has to paste anything into phpMyAdmin. To run it by hand anyway:
--      CREATE DATABASE barangay_db CHARACTER SET utf8mb4;
--      USE barangay_db;
--      -- then everything below
--
--  Two things I decided on purpose:
--
--  1. Enum columns store the NAME ('Female', 'ReadyForRelease', 'Active'),
--     never the number. A table has to be readable in phpMyAdmin, and
--     reordering an enum in C# must never change what an old row means.
--
--  2. Nobody deletes a resident or a receipt. Records are deactivated or
--     archived, because a released certificate and an official receipt point
--     back at them. The only rows that disappear are a resident's dependents
--     and request history, which go with the resident and her requests.
--
--  Note for the script runner in DatabaseInitializer: it drops lines that
--  start with two dashes and then splits on the semicolon, and it understands
--  the DELIMITER line used by the procedure script.
-- ============================================================================

CREATE TABLE IF NOT EXISTS residents (
    resident_id             INT            NOT NULL AUTO_INCREMENT,
    first_name              VARCHAR(100)   NOT NULL,
    middle_name             VARCHAR(100)   NOT NULL DEFAULT '',
    last_name               VARCHAR(100)   NOT NULL,
    suffix                  VARCHAR(20)    NOT NULL DEFAULT '',
    date_of_birth           DATE           NOT NULL,
    gender                  VARCHAR(20)    NOT NULL,
    civil_status            VARCHAR(20)    NOT NULL,
    purok                   VARCHAR(100)   NOT NULL DEFAULT '',
    contact_number          VARCHAR(30)    NOT NULL DEFAULT '',
    occupation              VARCHAR(100)   NOT NULL DEFAULT '',
    date_of_residency       DATE           NOT NULL,
    is_registered_voter     TINYINT(1)     NOT NULL DEFAULT 0,
    classification          INT            NOT NULL DEFAULT 0,
    is_student_fee_category TINYINT(1)     NOT NULL DEFAULT 0,
    is_business_owner       TINYINT(1)     NOT NULL DEFAULT 0,
    is_head_of_family       TINYINT(1)     NOT NULL DEFAULT 0,
    residency_status        VARCHAR(20)    NOT NULL DEFAULT 'Newcomer',
    record_state            VARCHAR(20)    NOT NULL DEFAULT 'Active',
    state_reason            VARCHAR(255)   NOT NULL DEFAULT '',
    state_changed_on        DATETIME       NULL,
    state_changed_by        VARCHAR(100)   NOT NULL DEFAULT '',
    has_availed_jobseeker   TINYINT(1)     NOT NULL DEFAULT 0,
    created_on              DATETIME       NOT NULL,
    created_by              VARCHAR(100)   NOT NULL DEFAULT '',
    updated_on              DATETIME       NULL,
    updated_by              VARCHAR(100)   NOT NULL DEFAULT '',
    PRIMARY KEY (resident_id),
    INDEX idx_residents_name (last_name, first_name),
    INDEX idx_residents_purok (purok),
    INDEX idx_residents_state (record_state)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS dependents (
    dependent_id      INT           NOT NULL AUTO_INCREMENT,
    head_resident_id  INT           NOT NULL,
    full_name         VARCHAR(150)  NOT NULL,
    relation          VARCHAR(30)   NOT NULL,
    date_of_birth     DATE          NOT NULL,
    is_studying       TINYINT(1)    NOT NULL DEFAULT 0,
    remarks           VARCHAR(255)  NOT NULL DEFAULT '',
    created_on        DATETIME      NOT NULL,
    created_by        VARCHAR(100)  NOT NULL DEFAULT '',
    PRIMARY KEY (dependent_id),
    INDEX idx_dependents_head (head_resident_id),
    CONSTRAINT fk_dependents_head FOREIGN KEY (head_resident_id)
        REFERENCES residents (resident_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS document_requests (
    request_id                  INT            NOT NULL AUTO_INCREMENT,
    reference_number            VARCHAR(30)    NOT NULL,
    resident_id                 INT            NOT NULL,
    document_type               VARCHAR(60)    NOT NULL,
    purpose                     VARCHAR(500)   NOT NULL DEFAULT '',
    date_requested              DATETIME       NOT NULL,
    status                      VARCHAR(30)    NOT NULL,
    fee                         DECIMAL(10,2)  NOT NULL DEFAULT 0,
    fee_basis                   VARCHAR(500)   NOT NULL DEFAULT '',
    is_paid                     TINYINT(1)     NOT NULL DEFAULT 0,
    official_receipt_no         VARCHAR(30)    NOT NULL DEFAULT '',
    or_control_number           VARCHAR(30)    NOT NULL DEFAULT '',
    payment_date                DATETIME       NULL,
    collected_by                VARCHAR(100)   NOT NULL DEFAULT '',
    scope                       VARCHAR(10)    NOT NULL DEFAULT 'Local',
    assessed_amount             DECIMAL(10,2)  NOT NULL DEFAULT 0,
    hours                       DECIMAL(6,2)   NOT NULL DEFAULT 0,
    gross_annual_income         DECIMAL(14,2)  NOT NULL DEFAULT 0,
    detail                      VARCHAR(255)   NOT NULL DEFAULT '',
    apply_jobseeker_waiver      TINYINT(1)     NOT NULL DEFAULT 0,
    availed_under_jobseeker_act TINYINT(1)     NOT NULL DEFAULT 0,
    requires_validation         TINYINT(1)     NOT NULL DEFAULT 0,
    filed_during_office_window  TINYINT(1)     NOT NULL DEFAULT 0,
    business_name               VARCHAR(150)   NOT NULL DEFAULT '',
    business_nature             VARCHAR(150)   NOT NULL DEFAULT '',
    business_purok              VARCHAR(100)   NOT NULL DEFAULT '',
    business_location           VARCHAR(150)   NOT NULL DEFAULT '',
    business_ownership          VARCHAR(60)    NOT NULL DEFAULT '',
    business_registration       VARCHAR(50)    NOT NULL DEFAULT '',
    business_previous_permit    VARCHAR(50)    NOT NULL DEFAULT '',
    business_employees          INT            NOT NULL DEFAULT 0,
    business_is_renewal         TINYINT(1)     NOT NULL DEFAULT 0,
    date_released               DATETIME       NULL,
    released_by                 VARCHAR(100)   NOT NULL DEFAULT '',
    received_by                 VARCHAR(100)   NOT NULL DEFAULT '',
    rejection_reason            VARCHAR(500)   NOT NULL DEFAULT '',
    remarks                     VARCHAR(500)   NOT NULL DEFAULT '',
    last_status_on              DATETIME       NULL,
    last_status_by              VARCHAR(100)   NOT NULL DEFAULT '',
    PRIMARY KEY (request_id),
    UNIQUE KEY uq_requests_reference (reference_number),
    INDEX idx_requests_status (status),
    INDEX idx_requests_filed (date_requested),
    CONSTRAINT fk_requests_resident FOREIGN KEY (resident_id)
        REFERENCES residents (resident_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS request_status_history (
    history_id  INT           NOT NULL AUTO_INCREMENT,
    request_id  INT           NOT NULL,
    status      VARCHAR(30)   NOT NULL,
    changed_on  DATETIME      NOT NULL,
    changed_by  VARCHAR(100)  NOT NULL DEFAULT '',
    reason      VARCHAR(500)  NOT NULL DEFAULT '',
    PRIMARY KEY (history_id),
    INDEX idx_history_request (request_id),
    CONSTRAINT fk_history_request FOREIGN KEY (request_id)
        REFERENCES document_requests (request_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS receipt_series (
    series_id     INT           NOT NULL AUTO_INCREMENT,
    series_code   VARCHAR(20)   NOT NULL,
    control_from  VARCHAR(30)   NOT NULL DEFAULT '',
    control_to    VARCHAR(30)   NOT NULL DEFAULT '',
    issued_to     VARCHAR(100)  NOT NULL DEFAULT '',
    issued_on     DATE          NOT NULL,
    is_active     TINYINT(1)    NOT NULL DEFAULT 1,
    remarks       VARCHAR(255)  NOT NULL DEFAULT '',
    PRIMARY KEY (series_id),
    UNIQUE KEY uq_series_code (series_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS official_receipts (
    receipt_id      INT            NOT NULL AUTO_INCREMENT,
    or_number       VARCHAR(30)    NOT NULL,
    series_code     VARCHAR(20)    NOT NULL DEFAULT '',
    control_number  VARCHAR(30)    NOT NULL DEFAULT '',
    or_date         DATE           NOT NULL,
    payer_name      VARCHAR(150)   NOT NULL DEFAULT '',
    amount          DECIMAL(10,2)  NOT NULL DEFAULT 0,
    method          VARCHAR(20)    NOT NULL DEFAULT 'Cash',
    request_id      INT            NULL,
    collected_by    VARCHAR(100)   NOT NULL DEFAULT '',
    remarks         VARCHAR(255)   NOT NULL DEFAULT '',
    is_void         TINYINT(1)     NOT NULL DEFAULT 0,
    void_reason     VARCHAR(255)   NOT NULL DEFAULT '',
    voided_on       DATETIME       NULL,
    voided_by       VARCHAR(100)   NOT NULL DEFAULT '',
    created_on      DATETIME       NOT NULL,
    PRIMARY KEY (receipt_id),
    UNIQUE KEY uq_receipt_or (series_code, or_number),
    INDEX idx_receipts_date (or_date),
    CONSTRAINT fk_receipts_request FOREIGN KEY (request_id)
        REFERENCES document_requests (request_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS user_accounts (
    user_id              INT           NOT NULL AUTO_INCREMENT,
    username             VARCHAR(50)   NOT NULL,
    full_name            VARCHAR(150)  NOT NULL DEFAULT '',
    role                 VARCHAR(30)   NOT NULL,
    position             VARCHAR(100)  NOT NULL DEFAULT '',
    password_hash        VARCHAR(255)  NOT NULL DEFAULT '',
    password_salt        VARCHAR(100)  NOT NULL DEFAULT '',
    hash_iterations      INT           NOT NULL DEFAULT 100000,
    must_change_password TINYINT(1)    NOT NULL DEFAULT 1,
    is_active            TINYINT(1)    NOT NULL DEFAULT 1,
    failed_attempts      INT           NOT NULL DEFAULT 0,
    locked_until         DATETIME      NULL,
    last_login_on        DATETIME      NULL,
    last_login_machine   VARCHAR(100)  NOT NULL DEFAULT '',
    created_on           DATETIME      NOT NULL,
    created_by           VARCHAR(100)  NOT NULL DEFAULT '',
    updated_on           DATETIME      NULL,
    updated_by           VARCHAR(100)  NOT NULL DEFAULT '',
    PRIMARY KEY (user_id),
    UNIQUE KEY uq_user_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS activity_log (
    log_id            BIGINT        NOT NULL AUTO_INCREMENT,
    occurred_on       DATETIME      NOT NULL,
    username          VARCHAR(50)   NOT NULL DEFAULT '',
    role              VARCHAR(30)   NOT NULL DEFAULT '',
    module            VARCHAR(30)   NOT NULL DEFAULT '',
    action            VARCHAR(60)   NOT NULL DEFAULT '',
    target_type       VARCHAR(60)   NOT NULL DEFAULT '',
    target_reference  VARCHAR(60)   NOT NULL DEFAULT '',
    details           VARCHAR(500)  NOT NULL DEFAULT '',
    machine_name      VARCHAR(100)  NOT NULL DEFAULT '',
    PRIMARY KEY (log_id),
    INDEX idx_log_date (occurred_on),
    INDEX idx_log_user (username),
    INDEX idx_log_module (module)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS reference_counters (
    counter_year  INT  NOT NULL,
    last_number   INT  NOT NULL DEFAULT 0,
    PRIMARY KEY (counter_year)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
