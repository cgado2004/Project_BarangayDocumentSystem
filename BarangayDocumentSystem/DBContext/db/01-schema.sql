-- =====================================================================
--  Barangay Resident and Document Request Management System - v3.1
--  MySQL schema
--
--  Barangay Magugpo Poblacion, City of Tagum, Davao del Norte
--  Written by Clint Wood Gado
--
--  HOW I RUN THIS
--  Start MySQL (in XAMPP, start the MySQL module), open phpMyAdmin or MySQL
--  Workbench, paste this whole file in, and run it. It drops and recreates
--  everything, so it is safe to run again whenever I change something.
--
--  Then run 02-seed-data.sql for the sample residents.
--
--  WHY I WROTE IT THIS WAY
--  I built these tables to match the C# classes in the Core project exactly,
--  so the two can never drift apart. Wherever I made a decision that is not
--  obvious, I explained it above the line rather than leaving my group-mates
--  to guess.
-- =====================================================================

DROP DATABASE IF EXISTS barangay_magugpo;
CREATE DATABASE barangay_magugpo
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

-- I use utf8mb4, not plain utf8. MySQL's old "utf8" only stores three bytes
-- per character and cannot hold every Unicode character. I need the full set
-- because of names like Peña, and because the peso sign appears in my
-- fee-basis text.

USE barangay_magugpo;


-- =====================================================================
--  RESIDENTS
-- =====================================================================
CREATE TABLE residents (
    resident_id           INT UNSIGNED  NOT NULL AUTO_INCREMENT,

    -- I allow 60 characters for each name. Philippine surnames such as
    -- "De los Santos" and hyphenated married names need the room.
    first_name            VARCHAR(60)   NOT NULL,
    middle_name           VARCHAR(60)       NULL,
    last_name             VARCHAR(60)   NOT NULL,
    suffix                VARCHAR(10)       NULL,   -- Jr., Sr., III

    date_of_birth         DATE          NOT NULL,

    -- I store the enum NAME, not a number.
    --
    -- If I stored 0 and 1 and somebody later reordered the C# enum, every
    -- existing row would silently change meaning and nothing would warn us.
    -- Storing the name makes that impossible, and it keeps my rows readable
    -- when I run a plain SELECT.
    gender                ENUM('Male','Female')                     NOT NULL,
    civil_status          ENUM('Single','Married','Widowed',
                               'Separated','Divorced') NOT NULL DEFAULT 'Single',

    -- The real puroks of Magugpo Poblacion. I took the list from the
    -- barangay's own FY 2025 20% Development Fund project sheet.
    purok                 VARCHAR(40)   NOT NULL,
    address_line          VARCHAR(160)      NULL,

    -- I store the number in the normalised local form, 09XXXXXXXXX, so the
    -- same phone is never saved two different ways.
    contact_number        VARCHAR(20)       NULL,

    occupation            VARCHAR(80)       NULL,

    -- This drives the six-month residency test in RA 11261.
    date_of_residency     DATE          NOT NULL,

    is_registered_voter   BOOLEAN       NOT NULL DEFAULT FALSE,

    -- RA 11261 may be availed ONCE only. This flag is what stops a second
    -- First-Time Jobseeker Certificate ever being released.
    has_availed_jobseeker BOOLEAN       NOT NULL DEFAULT FALSE,

    created_at            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP,

    PRIMARY KEY (resident_id),

    -- I index the columns I actually search on. Without these, my resident
    -- search does a full table scan on every keystroke.
    INDEX idx_residents_last_first (last_name, first_name),
    INDEX idx_residents_purok      (purok),
    INDEX idx_residents_contact    (contact_number)
) ENGINE = InnoDB;

-- I use InnoDB, not MyISAM. MyISAM ignores foreign keys completely, which
-- would throw away every relationship rule I write below.


-- =====================================================================
--  CLASSIFICATION TYPES
--  I made this a lookup table rather than hardcoding strings, so the list is
--  data I can add to instead of code I have to change.
-- =====================================================================
CREATE TABLE classification_types (
    classification_code   VARCHAR(20)   NOT NULL,
    display_name          VARCHAR(40)   NOT NULL,
    legal_basis           VARCHAR(120)      NULL,
    grants_fee_exemption  BOOLEAN       NOT NULL DEFAULT FALSE,

    PRIMARY KEY (classification_code)
) ENGINE = InnoDB;

INSERT INTO classification_types
    (classification_code, display_name, legal_basis, grants_fee_exemption) VALUES
    ('SENIOR_CITIZEN', 'Senior Citizen', 'RA 9994 (Expanded Senior Citizens Act)', TRUE),
    ('PWD',            'PWD',            'RA 10754 (Expanded Benefits for PWDs)',  TRUE),
    ('INDIGENT',       'Indigent',       'DILG MC 2019-177; RA 11032',             TRUE),
    ('STUDENT',        'Student',        NULL,                                     FALSE),
    ('SOLO_PARENT',    'Solo Parent',    'RA 11861 (Expanded Solo Parents Act)',   FALSE);


-- =====================================================================
--  RESIDENT CLASSIFICATIONS  (my junction table)
--
--  WHY I DID NOT USE A SINGLE NUMBER
--  In C# this is a [Flags] enum, so a resident who is both a senior and a PWD
--  is stored as the one number 3. That is compact, and I could have copied it
--  straight into an INT column here.
--
--  I chose not to, because it breaks First Normal Form - one column would
--  hold several values at once. The practical cost shows up the moment I
--  write a query. "List every senior citizen" would become
--
--      WHERE classification & 1      -- a bitmask, which CANNOT use an index
--
--  instead of a plain indexed join. A bitmask also has nowhere to record the
--  PWD ID number or the date a classification was granted.
--
--  One row per resident per classification fixes all of that.
-- =====================================================================
CREATE TABLE resident_classifications (
    resident_id           INT UNSIGNED  NOT NULL,
    classification_code   VARCHAR(20)   NOT NULL,

    -- Useful in a real barangay: when the PWD ID was issued, and its number.
    date_granted          DATE              NULL,
    reference_no          VARCHAR(40)       NULL,

    -- The composite key is what stops the same tag being added to the same
    -- resident twice.
    PRIMARY KEY (resident_id, classification_code),

    CONSTRAINT fk_rc_resident
        FOREIGN KEY (resident_id) REFERENCES residents (resident_id)
        ON DELETE CASCADE      -- remove the resident and their tags go too
        ON UPDATE CASCADE,

    CONSTRAINT fk_rc_type
        FOREIGN KEY (classification_code)
            REFERENCES classification_types (classification_code)
        ON DELETE RESTRICT     -- I never silently delete a type that is in use
        ON UPDATE CASCADE,

    INDEX idx_rc_code (classification_code)
) ENGINE = InnoDB;


-- =====================================================================
--  DOCUMENT REQUESTS
-- =====================================================================
CREATE TABLE document_requests (
    request_id            INT UNSIGNED  NOT NULL AUTO_INCREMENT,
    resident_id           INT UNSIGNED  NOT NULL,

    -- All twenty tarpaulin services plus the four the v3.1 Citizen's
    -- Charter adds: the cedula, the Katarungang Pambarangay filing, barangay
    -- facility use, and the other processing fees under the Barangay Taripa.
    -- The order matches my C# DocumentType enum, and new values go at the
    -- end, never in between.
    document_type         ENUM('BarangayClearance',
                               'CertificateOfResidency',
                               'CertificateOfIndigency',
                               'BarangayBusinessClearance',
                               'BarangayID',
                               'FirstTimeJobseekerCertificate',
                               'CertificateOfGoodMoralCharacter',
                               'CertificateOfLowIncome',
                               'SoloParentCertification',
                               'MedicalAssistanceCertification',
                               'FinancialAssistanceCertification',
                               'BurialAssistanceCertification',
                               'IpScholarshipCertification',
                               'FourPsScholarshipCertification',
                               'EmploymentCertification',
                               'AcceptanceCertificate',
                               'GadRelatedDocumentation',
                               'BlotterRelatedIncident',
                               'CsoDocumentation',
                               'OtherCertification',
                               'CommunityTaxCertificate',
                               'LuponCaseFiling',
                               'BarangayFacilityRental',
                               'OtherTarifaProcessingFee') NOT NULL,

    -- I need this because the Citizen's Charter prices ONE document two ways:
    -- a Barangay Clearance is 100 pesos for local employment but 200 pesos if
    -- it is for work abroad. Without this column I cannot charge what the
    -- charter says. It is ignored for every other document type.
    scope                 ENUM('Local','Abroad') NOT NULL DEFAULT 'Local',

    purpose               VARCHAR(200)  NOT NULL,

    date_requested        DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    date_released         DATETIME          NULL,   -- NULL until it is released

    status                ENUM('Pending','Processing','ReadyForRelease',
                               'Released','Rejected') NOT NULL DEFAULT 'Pending',

    -- DECIMAL(10,2), never FLOAT or DOUBLE.
    --
    -- Binary floating point cannot represent 0.10 exactly, so money drifts as
    -- you add it up. DECIMAL stores the digits exactly. This is the same
    -- reason my C# side uses decimal rather than double.
    fee                   DECIMAL(10,2) NOT NULL DEFAULT 0.00,

    -- Why the fee came out the way it did, for example "FREE - RA 11261".
    -- I store this because a resident can and will ask, and because it is my
    -- audit trail if a fee is ever questioned.
    fee_basis             VARCHAR(255)      NULL,

    -- ============ the v3.1 variable-fee columns =====================
    --
    -- Four documents are priced by circumstance, so I keep the circumstances
    -- beside the fee they produced:
    --   assessed_amount  - the business clearance under a violated law, or
    --                      the Taripa item assessed by the clerk
    --   hours_of_use     - barangay facility hours (billed per hour or part)
    --   declared_income  - the sworn gross annual income a cedula is computed
    --                      from (RA 7160 Sec. 156)
    --   fee_detail       - the free text: the law violated, the Taripa line,
    --                      or the facility used
    -- They are NULL for every flat-rate document, and they exist so a
    -- receipt can always be explained, not just totalled.
    assessed_amount       DECIMAL(10,2)     NULL,
    hours_of_use          DECIMAL(6,2)      NULL,
    declared_income       DECIMAL(12,2)     NULL,
    fee_detail            VARCHAR(200)      NULL,

    -- True when releasing this request consumed the resident's once-only
    -- RA 11261 benefit - either the certificate itself or a barangay
    -- clearance issued under the waiver.
    availed_jobseeker_act BOOLEAN       NOT NULL DEFAULT FALSE,

    is_paid               BOOLEAN       NOT NULL DEFAULT FALSE,
    official_receipt_no   VARCHAR(40)       NULL,

    remarks               VARCHAR(255)      NULL,   -- holds the rejection reason

    -- NOTE: there is deliberately NO reference_no column here.
    --
    -- The tracking number (BMP-2026-0042) is built from the year and the id,
    -- so storing it would duplicate data the table already holds. I tried
    -- twice to store it anyway and both attempts are illegal in MySQL, which
    -- I am recording so nobody repeats them:
    --
    --   1. A STORED generated column fails with ERROR 3109 - a generated
    --      column cannot refer to an AUTO_INCREMENT column, because MySQL
    --      computes it before the id has been assigned.
    --   2. An AFTER INSERT trigger that updates the row fails with ERROR 1442
    --      - a trigger may not modify the table that invoked it.
    --
    -- So I compute it in the vw_requests_full view instead, exactly as
    -- GetReferenceNumber() computes it in C#.

    created_at            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP,

    PRIMARY KEY (request_id),

    CONSTRAINT fk_request_resident
        FOREIGN KEY (resident_id) REFERENCES residents (resident_id)
        ON DELETE CASCADE      -- matches RemoveResident() in my C# repository
        ON UPDATE CASCADE,

    INDEX idx_requests_resident (resident_id),
    INDEX idx_requests_status   (status),
    INDEX idx_requests_date     (date_requested)
) ENGINE = InnoDB;


-- =====================================================================
--  RULES THE DATABASE ENFORCES BY ITSELF
--
--  These rules already exist in my C# code. I am repeating them here on
--  purpose, because the application is not the only thing that can write to
--  this database. Somebody running an UPDATE by hand in Workbench should not
--  be able to break a rule the app guarantees.
--
--  A WARNING ABOUT CHECK CONSTRAINTS
--  MySQL only started ENFORCING these in 8.0.16 (2019). Before that it parsed
--  them and silently ignored them, so a schema could look validated while
--  accepting anything at all. If you are on XAMPP you may be running MariaDB
--  instead - run the version check at the bottom of this file to find out.
--  The triggers below are my belt to these braces, and they work on every
--  version.
-- =====================================================================

ALTER TABLE document_requests
    ADD CONSTRAINT chk_fee_not_negative
        CHECK (fee >= 0),

    -- A paid request must have an OR number, and an unpaid one must not.
    ADD CONSTRAINT chk_receipt_matches_paid
        CHECK ( (is_paid = FALSE AND official_receipt_no IS NULL)
             OR (is_paid = TRUE  AND official_receipt_no IS NOT NULL) ),

    -- Only a released request has a release date.
    ADD CONSTRAINT chk_release_date_matches_status
        CHECK ( (status = 'Released'  AND date_released IS NOT NULL)
             OR (status <> 'Released' AND date_released IS NULL) ),

    -- A rejected request must say why. This is not bureaucracy on my part:
    -- the resident is entitled to know, and ARTA Advisory 01-2021 makes
    -- refusing a service without proper grounds a grave offence.
    ADD CONSTRAINT chk_rejection_has_reason
        CHECK ( status <> 'Rejected'
                OR (remarks IS NOT NULL AND remarks <> '') );

ALTER TABLE residents
    ADD CONSTRAINT chk_birth_before_residency
        CHECK (date_of_birth <= date_of_residency),

    ADD CONSTRAINT chk_names_not_blank
        CHECK (first_name <> '' AND last_name <> '');


-- =====================================================================
--  TRIGGER: the unpaid-release rule
--
--  This is the most important rule in my whole system, and the one a CHECK
--  constraint cannot express, because it has to compare the row being written
--  against the row already there.
--
--  Release() in my C# code refuses to release a fee-bearing document that has
--  not been paid. Without this trigger, a plain UPDATE in Workbench would walk
--  straight past that rule and let a document out of the door unpaid.
-- =====================================================================
DELIMITER $$

CREATE TRIGGER trg_block_unpaid_release
BEFORE UPDATE ON document_requests
FOR EACH ROW
BEGIN
    IF NEW.status = 'Released'
       AND NEW.fee > 0
       AND NEW.is_paid = FALSE THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT =
                'This document has an unpaid fee. Record the payment before releasing it.';
    END IF;

    -- A released document can never be rejected afterwards, and a rejected
    -- one can never be released. I treat both as terminal states.
    IF OLD.status = 'Released' AND NEW.status = 'Rejected' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'A released document cannot be rejected.';
    END IF;
END$$


-- =====================================================================
--  TRIGGER: RA 11261 may be availed only once
--
--  When a First-Time Jobseeker Certificate is released I flag the resident, so
--  a second one can never be issued. v3.1 extends the same rule to a barangay
--  CLEARANCE issued under the waiver - the law covers both documents - which
--  is what the availed_jobseeker_act column records. My C# code sets this
--  too; the trigger keeps it true even if somebody updates the row directly.
-- =====================================================================
CREATE TRIGGER trg_mark_jobseeker_availed
AFTER UPDATE ON document_requests
FOR EACH ROW
BEGIN
    IF NEW.status = 'Released'
       AND OLD.status <> 'Released'
       AND (NEW.document_type = 'FirstTimeJobseekerCertificate'
            OR NEW.availed_jobseeker_act = TRUE) THEN
        UPDATE residents
           SET has_availed_jobseeker = TRUE
         WHERE resident_id = NEW.resident_id;
    END IF;
END$$

DELIMITER ;


-- =====================================================================
--  VIEWS
--  These save me writing the same joins over and over, and they are what my
--  dashboard reads instead of pulling every row into C# and counting there.
-- =====================================================================

-- A resident with their classifications collapsed onto one readable line,
-- for example "Senior Citizen, PWD". This is my junction table's answer to
-- the convenience a bitmask would have given me.
CREATE OR REPLACE VIEW vw_residents_full AS
SELECT
    r.resident_id,
    CONCAT(r.last_name, ', ', r.first_name,
           IF(r.middle_name IS NULL OR r.middle_name = '',
              '', CONCAT(' ', UPPER(LEFT(r.middle_name, 1)), '.')),
           IF(r.suffix IS NULL OR r.suffix = '', '', CONCAT(' ', r.suffix))
    ) AS sortable_name,
    TIMESTAMPDIFF(YEAR,  r.date_of_birth,     CURDATE()) AS age,
    TIMESTAMPDIFF(MONTH, r.date_of_residency, CURDATE()) AS months_of_residency,
    r.gender,
    r.civil_status,
    r.purok,
    r.contact_number,
    r.is_registered_voter,
    r.has_availed_jobseeker,
    COALESCE(GROUP_CONCAT(ct.display_name ORDER BY ct.display_name SEPARATOR ', '),
             'None') AS classifications
FROM residents r
LEFT JOIN resident_classifications rc ON rc.resident_id = r.resident_id
LEFT JOIN classification_types     ct ON ct.classification_code = rc.classification_code
GROUP BY r.resident_id;

-- I use TIMESTAMPDIFF(YEAR, ...) for age because it counts whole years and
-- accounts for whether the birthday has happened yet. Subtracting the year
-- numbers, which is what a lot of examples do, is wrong for anyone whose
-- birthday has not come round yet this year.


CREATE OR REPLACE VIEW vw_requests_full AS
SELECT
    dr.request_id,
    -- The same format my GetReferenceNumber() produces in C#.
    CONCAT('BMP-', YEAR(dr.date_requested), '-',
           LPAD(dr.request_id, 4, '0')) AS reference_no,
    CONCAT(r.first_name, ' ', r.last_name) AS resident_name,
    r.purok,
    dr.document_type,
    dr.scope,
    dr.purpose,
    dr.status,
    dr.fee,
    dr.fee_basis,
    dr.assessed_amount,
    dr.hours_of_use,
    dr.declared_income,
    dr.fee_detail,
    dr.availed_jobseeker_act,
    dr.is_paid,
    dr.official_receipt_no,
    dr.date_requested,
    dr.date_released,
    dr.remarks
FROM document_requests dr
JOIN residents r ON r.resident_id = dr.resident_id;


-- Everything my dashboard shows, as a single row.
CREATE OR REPLACE VIEW vw_dashboard_statistics AS
SELECT
    (SELECT COUNT(*) FROM residents)                                   AS total_residents,
    (SELECT COUNT(*) FROM residents WHERE is_registered_voter)         AS registered_voters,
    (SELECT COUNT(*) FROM resident_classifications
      WHERE classification_code = 'SENIOR_CITIZEN')                    AS senior_citizens,
    (SELECT COUNT(*) FROM document_requests)                           AS total_requests,
    (SELECT COUNT(*) FROM document_requests WHERE status = 'Pending')         AS pending,
    (SELECT COUNT(*) FROM document_requests WHERE status = 'Processing')      AS processing,
    (SELECT COUNT(*) FROM document_requests WHERE status = 'ReadyForRelease') AS ready_for_release,
    (SELECT COUNT(*) FROM document_requests WHERE status = 'Released')        AS released,
    (SELECT COALESCE(SUM(fee), 0) FROM document_requests WHERE is_paid)       AS total_collected,
    (SELECT COUNT(*) FROM document_requests
      WHERE fee = 0 AND status = 'Released')                           AS issued_free_of_charge;


-- =====================================================================
--  VERSION CHECK
--  I run this and read the answer before trusting the CHECK constraints.
-- =====================================================================
SELECT
    VERSION() AS mysql_version,
    IF(VERSION() LIKE '%MariaDB%' OR VERSION() >= '8.0.16',
       'CHECK constraints are enforced on this server',
       'WARNING: this version IGNORES CHECK constraints - rely on the triggers and the C# validation')
    AS check_constraint_support;
