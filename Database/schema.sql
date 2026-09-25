-- ============================================================================
-- BarangayDocumentSystem — MySQL schema (the revamp's database).
--
-- This is Jonathan F. Del Rosario's LocalDB schema design (Data/Schema.sql on
-- the Draft branch: AppState, Version columns, the resident snapshot table,
-- the payment/release/rejection CHECKs, the unique receipt index) translated
-- to the MySQL dialect by Frent Raborar's Database layer, with one addition
-- the Charter required: the clearance `scope` column (local PHP 100 / abroad
-- PHP 200).
--
-- NOTES FOR THE PARSER (DatabaseInitializer strips whole-line -- comments
--   first, then splits statements on the semicolon):
--   keep comments on their own lines, never after code, and never put a
--   semicolon inside a string.
--
-- MySQL notes kept from the translation:
--   * CHECK constraints are ENFORCED on MySQL 8.0.16+ (run SELECT VERSION()
--     first on older servers; the app enforces the same rules in C# anyway).
--   * MySQL has no filtered indexes. The receipt uniqueness travels on the
--     column itself: unpaid rows keep official_receipt_no NULL (the app maps
--     "" to NULL), and a unique index allows any number of NULLs.
--   * The once-only jobseeker rule uses a generated column: it repeats the
--     resident_id while a jobseeker request is active (DocumentType 5 and
--     not Rejected) and is NULL otherwise, so a unique index on it allows
--     exactly one live jobseeker request per resident — no trigger needed.
-- ============================================================================

CREATE TABLE IF NOT EXISTS residents (
    resident_id            INT           NOT NULL AUTO_INCREMENT,
    version                INT           NOT NULL DEFAULT 1,
    first_name             VARCHAR(80)   NOT NULL,
    middle_name            VARCHAR(80)   NOT NULL DEFAULT '',
    last_name              VARCHAR(80)   NOT NULL,
    suffix                 VARCHAR(20)   NOT NULL DEFAULT '',
    date_of_birth          DATE          NOT NULL,
    gender                 INT           NOT NULL,
    civil_status           INT           NOT NULL,
    purok                  VARCHAR(60)   NOT NULL DEFAULT '',
    address                VARCHAR(250)  NOT NULL DEFAULT '',
    contact_number         VARCHAR(15)   NOT NULL DEFAULT '',
    occupation             VARCHAR(100)  NOT NULL DEFAULT '',
    date_of_residency      DATE          NOT NULL,
    is_registered_voter    TINYINT(1)    NOT NULL DEFAULT 0,
    is_senior_citizen      TINYINT(1)    NOT NULL DEFAULT 0,
    is_person_with_disability TINYINT(1) NOT NULL DEFAULT 0,
    is_indigent            TINYINT(1)    NOT NULL DEFAULT 0,
    is_student             TINYINT(1)    NOT NULL DEFAULT 0,
    is_solo_parent         TINYINT(1)    NOT NULL DEFAULT 0,
    has_used_jobseeker_benefit TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (resident_id),
    CONSTRAINT ck_residents_dates CHECK (date_of_residency >= date_of_birth),
    CONSTRAINT ck_residents_gender CHECK (gender BETWEEN 0 AND 1),
    CONSTRAINT ck_residents_civil_status CHECK (civil_status BETWEEN 0 AND 4),
    INDEX idx_residents_name (last_name, first_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS document_requests (
    request_id             INT           NOT NULL AUTO_INCREMENT,
    version                INT           NOT NULL DEFAULT 1,
    resident_id            INT           NOT NULL,
    document_type          INT           NOT NULL,
    scope                  TINYINT(1)    NOT NULL DEFAULT 0,
    document_name          VARCHAR(120)  NOT NULL DEFAULT '',
    purpose                VARCHAR(300)  NOT NULL DEFAULT '',
    business_name          VARCHAR(120)  NOT NULL DEFAULT '',
    business_address       VARCHAR(250)  NOT NULL DEFAULT '',
    business_nature        VARCHAR(150)  NOT NULL DEFAULT '',
    date_requested         DATETIME(6)   NOT NULL,
    date_released          DATETIME(6)   NULL,
    status                 INT           NOT NULL,
    fee                    DECIMAL(12,2) NOT NULL DEFAULT 0,
    fee_basis              VARCHAR(1000) NOT NULL DEFAULT '',
    is_paid                TINYINT(1)    NOT NULL DEFAULT 0,
    official_receipt_no    VARCHAR(50)   NULL,
    date_paid              DATETIME(6)   NULL,
    rejection_reason       VARCHAR(300)  NOT NULL DEFAULT '',
    released_document_text MEDIUMTEXT    NOT NULL,
    -- Once-only RA 11261 guard: repeats resident_id while a jobseeker
    -- request (DocumentType 5) is not Rejected, else NULL.
    jobseeker_guard        INT           GENERATED ALWAYS AS
        (CASE WHEN document_type = 5 AND status <> 4 THEN resident_id END) STORED,
    PRIMARY KEY (request_id),
    CONSTRAINT ck_requests_type CHECK (document_type BETWEEN 0 AND 6),
    CONSTRAINT ck_requests_scope CHECK (scope BETWEEN 0 AND 1),
    CONSTRAINT ck_requests_status CHECK (status BETWEEN 0 AND 4),
    CONSTRAINT ck_requests_fee CHECK (fee >= 0),
    CONSTRAINT ck_requests_payment CHECK (
        (is_paid = 0 AND official_receipt_no IS NULL AND date_paid IS NULL) OR
        (is_paid = 1 AND fee > 0 AND official_receipt_no IS NOT NULL
            AND CHAR_LENGTH(TRIM(official_receipt_no)) > 0 AND date_paid IS NOT NULL)),
    CONSTRAINT ck_requests_release CHECK (
        status <> 3 OR (date_released IS NOT NULL
            AND CHAR_LENGTH(released_document_text) > 0 AND (fee = 0 OR is_paid = 1))),
    CONSTRAINT ck_requests_rejection CHECK (
        status <> 4 OR CHAR_LENGTH(TRIM(rejection_reason)) > 0),
    CONSTRAINT fk_requests_resident FOREIGN KEY (resident_id)
        REFERENCES residents (resident_id),
    UNIQUE INDEX ux_requests_receipt (official_receipt_no),
    UNIQUE INDEX ux_requests_jobseeker (jobseeker_guard),
    INDEX idx_requests_resident (resident_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


CREATE TABLE IF NOT EXISTS fee_schedule (
    fee_code               VARCHAR(40)   NOT NULL,
    service_name           VARCHAR(120)  NOT NULL,
    amount                 DECIMAL(12,2) NOT NULL,
    legal_basis            VARCHAR(300)  NOT NULL DEFAULT '',
    notes                  VARCHAR(300)  NOT NULL DEFAULT '',
    sort_order             INT           NOT NULL DEFAULT 0,
    PRIMARY KEY (fee_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- The Citizen's Charter rates live here (docs/07-fee-schedule-and-legal-basis.md).
-- Every row is idempotent: an existing database is topped up on the next start.
INSERT INTO fee_schedule (fee_code, service_name, amount, legal_basis, notes, sort_order)
    SELECT 'clearance_local', 'Barangay Clearance - local employment', 100.00,
    'RA 7160 Sec. 152, Citizen''s Charter', 'charged to the applicant', 1 FROM DUAL
    WHERE NOT EXISTS (SELECT 1 FROM fee_schedule WHERE fee_code = 'clearance_local');
INSERT INTO fee_schedule (fee_code, service_name, amount, legal_basis, notes, sort_order)
    SELECT 'clearance_abroad', 'Barangay Clearance - work abroad', 200.00,
    'RA 7160 Sec. 152, Citizen''s Charter', 'charged to the applicant', 2 FROM DUAL
    WHERE NOT EXISTS (SELECT 1 FROM fee_schedule WHERE fee_code = 'clearance_abroad');
INSERT INTO fee_schedule (fee_code, service_name, amount, legal_basis, notes, sort_order)
    SELECT 'certification', 'Certification (residency, good moral, other)', 100.00,
    'Citizen''s Charter, RA 7160 Sec. 152', 'anything the Charter does not price separately', 3 FROM DUAL
    WHERE NOT EXISTS (SELECT 1 FROM fee_schedule WHERE fee_code = 'certification');
INSERT INTO fee_schedule (fee_code, service_name, amount, legal_basis, notes, sort_order)
    SELECT 'indigency', 'Certificate of Indigency', 0.00,
    'Citizen''s Charter', 'free of charge', 4 FROM DUAL
    WHERE NOT EXISTS (SELECT 1 FROM fee_schedule WHERE fee_code = 'indigency');
INSERT INTO fee_schedule (fee_code, service_name, amount, legal_basis, notes, sort_order)
    SELECT 'low_income', 'Certificate of Low Income', 0.00,
    'Citizen''s Charter', 'free of charge', 5 FROM DUAL
    WHERE NOT EXISTS (SELECT 1 FROM fee_schedule WHERE fee_code = 'low_income');
INSERT INTO fee_schedule (fee_code, service_name, amount, legal_basis, notes, sort_order)
    SELECT 'business_clearance', 'Business Clearance', 200.00,
    'RA 7160 Sec. 152, Citizen''s Charter', 'standard rate - varies with the law violated', 6 FROM DUAL
    WHERE NOT EXISTS (SELECT 1 FROM fee_schedule WHERE fee_code = 'business_clearance');
INSERT INTO fee_schedule (fee_code, service_name, amount, legal_basis, notes, sort_order)
    SELECT 'barangay_id', 'Barangay ID', 100.00,
    'classroom schedule', 'not priced in the Charter excerpt', 7 FROM DUAL
    WHERE NOT EXISTS (SELECT 1 FROM fee_schedule WHERE fee_code = 'barangay_id');
INSERT INTO fee_schedule (fee_code, service_name, amount, legal_basis, notes, sort_order)
    SELECT 'first_time_jobseeker', 'First-Time Jobseeker Certificate', 0.00,
    'RA 11261', 'free once per resident', 8 FROM DUAL
    WHERE NOT EXISTS (SELECT 1 FROM fee_schedule WHERE fee_code = 'first_time_jobseeker');
INSERT INTO fee_schedule (fee_code, service_name, amount, legal_basis, notes, sort_order)
    SELECT 'cedula_basic', 'Community Tax (cedula), basic', 5.00,
    'RA 7160 Sec. 156', 'plus 1.00 per 1,000 of income, additional capped at 5,000', 9 FROM DUAL
    WHERE NOT EXISTS (SELECT 1 FROM fee_schedule WHERE fee_code = 'cedula_basic');
INSERT INTO fee_schedule (fee_code, service_name, amount, legal_basis, notes, sort_order)
    SELECT 'lupon_filing', 'Filing a case (Katarungang Pambarangay)', 150.00,
    'RA 7160, Citizen''s Charter', 'per case filed', 10 FROM DUAL
    WHERE NOT EXISTS (SELECT 1 FROM fee_schedule WHERE fee_code = 'lupon_filing');
INSERT INTO fee_schedule (fee_code, service_name, amount, legal_basis, notes, sort_order)
    SELECT 'facility_hour', 'Barangay facility rental', 200.00,
    'RA 7160 Sec. 152, Citizen''s Charter', 'per hour or part thereof', 11 FROM DUAL
    WHERE NOT EXISTS (SELECT 1 FROM fee_schedule WHERE fee_code = 'facility_hour');

CREATE TABLE IF NOT EXISTS resident_snapshots (
    request_id             INT           NOT NULL,
    first_name             VARCHAR(80)   NOT NULL,
    middle_name            VARCHAR(80)   NOT NULL DEFAULT '',
    last_name              VARCHAR(80)   NOT NULL,
    suffix                 VARCHAR(20)   NOT NULL DEFAULT '',
    date_of_birth          DATE          NOT NULL,
    gender                 INT           NOT NULL,
    civil_status           INT           NOT NULL,
    purok                  VARCHAR(60)   NOT NULL DEFAULT '',
    address                VARCHAR(250)  NOT NULL DEFAULT '',
    contact_number         VARCHAR(15)   NOT NULL DEFAULT '',
    occupation             VARCHAR(100)  NOT NULL DEFAULT '',
    date_of_residency      DATE          NOT NULL,
    is_registered_voter    TINYINT(1)    NOT NULL DEFAULT 0,
    is_senior_citizen      TINYINT(1)    NOT NULL DEFAULT 0,
    is_person_with_disability TINYINT(1) NOT NULL DEFAULT 0,
    is_indigent            TINYINT(1)    NOT NULL DEFAULT 0,
    is_student             TINYINT(1)    NOT NULL DEFAULT 0,
    is_solo_parent         TINYINT(1)    NOT NULL DEFAULT 0,
    has_used_jobseeker_benefit TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (request_id),
    CONSTRAINT fk_snapshots_requests FOREIGN KEY (request_id)
        REFERENCES document_requests (request_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
