-- ============================================================================
--  Barangay Document System — MySQL schema
--
--  You normally do NOT need to run this by hand: the app runs it on every
--  start (DatabaseInitializer). Every statement is "IF NOT EXISTS", so running
--  it again changes nothing.
--
--  To run it manually (MySQL Workbench / phpMyAdmin / mysql CLI):
--      CREATE DATABASE barangay_db CHARACTER SET utf8mb4;
--      USE barangay_db;
--      -- then run everything below
--
--  Enum columns store the enum NAME (for example 'Female', 'ReadyForRelease')
--  so the data is readable in a table browser.
--  NOTE FOR THE PARSER: do not put a semicolon inside a comment or a string.
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
    -- Bit flags: 1 Senior, 2 PWD, 4 Indigent, 8 Student, 16 Solo Parent (0 = none)
    classification        INT           NOT NULL DEFAULT 0,
    -- RA 11261 may be availed only once. Set when the certificate is released.
    has_availed_jobseeker TINYINT(1)    NOT NULL DEFAULT 0,
    PRIMARY KEY (resident_id),
    INDEX idx_residents_name (last_name, first_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS document_requests (
    request_id            INT           NOT NULL AUTO_INCREMENT,
    resident_id           INT           NOT NULL,
    document_type         VARCHAR(50)   NOT NULL,
    purpose               VARCHAR(500)  NOT NULL DEFAULT '',
    date_requested        DATETIME      NOT NULL,
    date_released         DATETIME      NULL,
    status                VARCHAR(30)   NOT NULL,
    fee                   DECIMAL(10,2) NOT NULL DEFAULT 0,
    fee_basis             VARCHAR(255)  NOT NULL DEFAULT '',
    is_paid               TINYINT(1)    NOT NULL DEFAULT 0,
    official_receipt_no   VARCHAR(50)   NOT NULL DEFAULT '',
    remarks               VARCHAR(500)  NOT NULL DEFAULT '',
    PRIMARY KEY (request_id),
    INDEX idx_requests_status (status),
    -- Deleting a resident deletes their requests (same rule the app showed before).
    CONSTRAINT fk_requests_resident FOREIGN KEY (resident_id)
        REFERENCES residents (resident_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
