-- ============================================================================
--  PART:    Database - the ONE schema of the Barangay Document System
--  ORIGIN:  Fdraft - Frent Dhieniel Raborar (both tables, the cascade rule,
--           the enum-name convention, the embedded-resource approach)
--  EDITS:   Clint Wood Gado - seven request columns for my v3.1 fee model
--           (scope, assessed amount, hours, income, detail, two RA 11261
--           flags) and a wider fee_basis; comments in my voice
--
--  The program runs this itself on start-up (DatabaseInitializer), after
--  creating the database if it is missing. Nothing has to be run by hand.
--  To run it manually anyway (phpMyAdmin / MySQL Workbench / mysql CLI):
--      CREATE DATABASE barangay_db CHARACTER SET utf8mb4;
--      USE barangay_db;
--      -- then everything below
--
--  Enum columns store the enum NAME ('Female', 'ReadyForRelease'), never its
--  number, so the table is readable in phpMyAdmin and reordering an enum in
--  C# can never silently change what a row means.
--
--  NOTE FOR THE PARSER: it drops the lines that start with two dashes, then
--  splits the rest on the semicolon. So: no semicolon inside a string, and
--  no comment on the same line as SQL.
-- ============================================================================

CREATE TABLE IF NOT EXISTS residents (
    resident_id           INT           NOT NULL AUTO_INCREMENT,
    first_name            VARCHAR(100)  NOT NULL,
    middle_name           VARCHAR(100)  NOT NULL DEFAULT '',
    last_name             VARCHAR(100)  NOT NULL,
    suffix                VARCHAR(20)   NOT NULL DEFAULT '',
    date_of_birth         DATE          NOT NULL,
    gender                VARCHAR(20)   NOT NULL,
    civil_status          VARCHAR(20)   NOT NULL,
    purok                 VARCHAR(100)  NOT NULL DEFAULT '',
    address_line          VARCHAR(255)  NOT NULL DEFAULT '',
    contact_number        VARCHAR(30)   NOT NULL DEFAULT '',
    occupation            VARCHAR(100)  NOT NULL DEFAULT '',
    date_of_residency     DATE          NOT NULL,
    is_registered_voter   TINYINT(1)    NOT NULL DEFAULT 0,
    -- Bit flags, exactly as the C# ResidentClassification enum: 1 Senior,
    -- 2 PWD, 4 Indigent, 8 Student, 16 Solo Parent (0 = none). A resident
    -- can be several at once - Liza in the sample data is a PWD student.
    classification        INT           NOT NULL DEFAULT 0,
    -- RA 11261 may be availed only once. Set when the certificate (or a
    -- clearance issued under the waiver) is released.
    has_availed_jobseeker TINYINT(1)    NOT NULL DEFAULT 0,
    PRIMARY KEY (resident_id),
    INDEX idx_residents_name (last_name, first_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS document_requests (
    request_id                  INT           NOT NULL AUTO_INCREMENT,
    resident_id                 INT           NOT NULL,
    document_type               VARCHAR(50)   NOT NULL,
    purpose                     VARCHAR(500)  NOT NULL DEFAULT '',
    date_requested              DATETIME      NOT NULL,
    date_released               DATETIME      NULL,
    status                      VARCHAR(30)   NOT NULL,
    -- What was charged and the sentence of law behind it, frozen at filing
    -- time. The fee schedule may change next year - this row must not.
    fee                         DECIMAL(10,2) NOT NULL DEFAULT 0,
    fee_basis                   VARCHAR(500)  NOT NULL DEFAULT '',
    is_paid                     TINYINT(1)    NOT NULL DEFAULT 0,
    official_receipt_no         VARCHAR(50)   NOT NULL DEFAULT '',
    remarks                     VARCHAR(500)  NOT NULL DEFAULT '',
    -- The inputs the fee was assessed from (the C# RequestInput record), so
    -- a reloaded request prints the same computation it was filed with.
    scope                       VARCHAR(10)   NOT NULL DEFAULT 'Local',
    assessed_amount             DECIMAL(10,2) NOT NULL DEFAULT 0,
    hours                       DECIMAL(6,2)  NOT NULL DEFAULT 0,
    gross_annual_income         DECIMAL(14,2) NOT NULL DEFAULT 0,
    detail                      VARCHAR(255)  NOT NULL DEFAULT '',
    apply_jobseeker_waiver      TINYINT(1)    NOT NULL DEFAULT 0,
    -- True when releasing this request consumes the resident's once-only
    -- RA 11261 benefit.
    availed_under_jobseeker_act TINYINT(1)    NOT NULL DEFAULT 0,
    PRIMARY KEY (request_id),
    INDEX idx_requests_status (status),
    -- Deleting a resident deletes their requests, the same rule the
    -- repository applies to its working set.
    CONSTRAINT fk_requests_resident FOREIGN KEY (resident_id)
        REFERENCES residents (resident_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
