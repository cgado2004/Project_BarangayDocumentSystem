-- =====================================================================
--  Sample data
--  Written by Clint Wood Gado
--
--  These are the same seven residents my in-memory version creates, so the
--  app behaves identically whichever storage it is pointed at. That matters
--  for our demo: I can switch App.config to MySQL and everything on screen
--  still looks the same.
--
--  Run 01-schema.sql first.
-- =====================================================================

USE barangay_magugpo;

-- I clear out any previous run. The order matters: children before parents,
-- or the foreign keys refuse to let me delete.
DELETE FROM document_requests;
DELETE FROM resident_classifications;
DELETE FROM residents;

ALTER TABLE residents         AUTO_INCREMENT = 1;
ALTER TABLE document_requests AUTO_INCREMENT = 1;


-- =====================================================================
--  RESIDENTS
--  I gave each one a different situation on purpose, so our demo can show
--  every branch of the fee rules without inventing data on the spot.
--  The purok names are the real ones from the barangay's FY 2025 20%
--  Development Fund project list.
-- =====================================================================
INSERT INTO residents
    (first_name, middle_name, last_name, suffix, date_of_birth, gender,
     civil_status, purok, address_line, contact_number, occupation,
     date_of_residency, is_registered_voter)
VALUES
    -- 1. An ordinary resident with no exemptions. He pays full price.
    ('Juan', 'Perez', 'Dela Cruz', NULL, '1985-04-12', 'Male',
     'Married', 'Purok Tandang Sora', '123 Rizal Street', '09171234567',
     'Tricycle Driver', '2010-06-01', TRUE),

    -- 2. A senior citizen, to demonstrate the RA 9994 waiver.
    ('Maria', 'Santos', 'Reyes', NULL, '1955-09-03', 'Female',
     'Widowed', 'Purok Orchids', '45 Bonifacio Avenue', '09181234567',
     'Retired', '1998-01-15', TRUE),

    -- 3. A fresh graduate with 14 months of residency, so he PASSES the
    --    RA 11261 six-month test and can claim a free jobseeker certificate.
    ('Jose', 'Cruz', 'Bautista', 'Jr.', '2004-02-20', 'Male',
     'Single', 'Purok Sampaguita', '78 Mabini Street', '09191234567',
     'Fresh Graduate', DATE_SUB(CURDATE(), INTERVAL 14 MONTH), TRUE),

    -- 4. A solo parent who runs a business. I use her to show that personal
    --    exemptions do NOT apply to a business clearance.
    ('Ana', 'Lopez', 'Villanueva', NULL, '1992-11-08', 'Female',
     'Single', 'Purok Sunflower', '12 Quezon Street', '09201234567',
     'Sari-sari Store Owner', '2015-03-20', TRUE),

    -- 5. An indigent resident.
    ('Pedro', 'Ramos', 'Mendoza', NULL, '1978-07-25', 'Male',
     'Married', 'Purok Cristo Rey', '90 Magsaysay Street', '09211234567',
     'Carpenter', '2005-08-10', FALSE),

    -- 6. A hyphenated surname, and TWO classifications at once. This is the
    --    row that justifies my junction table.
    ('Liza', 'Garcia', 'Santos-Reyes', NULL, '1999-05-30', 'Female',
     'Single', 'Purok Orchids', '56 Del Pilar Street', '09221234567',
     'Student', '2019-06-01', TRUE),

    -- 7. A surname with ñ, and a DITO number (the 089X range, which is a
    --    mobile even though it does not start 09). Only 2 months of
    --    residency, so he FAILS the RA 11261 test.
    ('Carlo', 'Diaz', 'Peña', NULL, '2003-12-05', 'Male',
     'Single', 'Purok Lapu-Lapu', '34 Luna Street', '08951234567',
     'Unemployed', DATE_SUB(CURDATE(), INTERVAL 2 MONTH), FALSE);


-- =====================================================================
--  CLASSIFICATIONS
--  I look the residents up by name rather than hardcoding resident_id = 2,
--  so this still works if the rows ever go in in a different order.
-- =====================================================================
INSERT INTO resident_classifications (resident_id, classification_code)
SELECT resident_id, 'SENIOR_CITIZEN' FROM residents
 WHERE last_name = 'Reyes' AND first_name = 'Maria';

INSERT INTO resident_classifications (resident_id, classification_code)
SELECT resident_id, 'SOLO_PARENT' FROM residents WHERE last_name = 'Villanueva';

INSERT INTO resident_classifications (resident_id, classification_code)
SELECT resident_id, 'INDIGENT' FROM residents WHERE last_name = 'Mendoza';

-- Liza holds two at once. In my C# [Flags] enum that was Student | PWD;
-- here it is simply two rows, which is the whole point of the junction table.
INSERT INTO resident_classifications (resident_id, classification_code)
SELECT resident_id, 'STUDENT' FROM residents WHERE last_name = 'Santos-Reyes';

INSERT INTO resident_classifications (resident_id, classification_code)
SELECT resident_id, 'PWD' FROM residents WHERE last_name = 'Santos-Reyes';


-- =====================================================================
--  DOCUMENT REQUESTS
--
--  The fee amounts below are the REAL rates from the Barangay Citizen's
--  Charter posted at the hall - 100 pesos for a local clearance, 200 for one
--  going abroad, 100 for a certification, and free for indigency and low
--  income. Earlier versions of my project used 50-peso placeholders; those
--  were wrong and I have replaced them.
-- =====================================================================

-- Juan: a clearance that went all the way through, paid and released.
INSERT INTO document_requests
    (resident_id, document_type, scope, purpose, status, fee, fee_basis,
     is_paid, official_receipt_no, date_requested, date_released)
SELECT resident_id, 'BarangayClearance', 'Local', 'Employment Requirement',
       'Released', 100.00,
       'Barangay Clearance - for local employment (Citizen''s Charter)',
       TRUE, 'OR-2026-00101',
       DATE_SUB(NOW(), INTERVAL 10 DAY), DATE_SUB(NOW(), INTERVAL 8 DAY)
FROM residents WHERE last_name = 'Dela Cruz';

-- Maria: a senior citizen, so the fee is waived and no OR is needed.
-- This is the row that proves a zero-fee document can be released without
-- ever touching the payment step.
INSERT INTO document_requests
    (resident_id, document_type, purpose, status, fee, fee_basis,
     is_paid, date_requested, date_released)
SELECT resident_id, 'CertificateOfResidency', 'Pension Claim', 'Released',
       0.00, 'FREE - Senior Citizen (RA 9994)',
       FALSE, DATE_SUB(NOW(), INTERVAL 6 DAY), DATE_SUB(NOW(), INTERVAL 5 DAY)
FROM residents WHERE last_name = 'Reyes' AND first_name = 'Maria';

-- Jose: a first-time jobseeker, free under RA 11261, still being processed.
INSERT INTO document_requests
    (resident_id, document_type, purpose, status, fee, fee_basis, date_requested)
SELECT resident_id, 'FirstTimeJobseekerCertificate',
       'NBI Clearance Application', 'Processing', 0.00,
       'FREE - RA 11261 (First Time Jobseekers Assistance Act)',
       DATE_SUB(NOW(), INTERVAL 3 DAY)
FROM residents WHERE last_name = 'Bautista';

-- Pedro: an indigency certificate, free, ready to collect.
INSERT INTO document_requests
    (resident_id, document_type, purpose, status, fee, fee_basis, date_requested)
SELECT resident_id, 'CertificateOfIndigency',
       'Medical Assistance at Davao Regional Medical Center',
       'ReadyForRelease', 0.00,
       'FREE - Certificate of Indigency (DILG MC 2019-177)',
       DATE_SUB(NOW(), INTERVAL 2 DAY)
FROM residents WHERE last_name = 'Mendoza';

-- Ana: a business clearance. She is a solo parent, but personal exemptions do
-- NOT apply to a business, so the full fee stands. This is the row I would
-- point at if anyone questions that rule.
INSERT INTO document_requests
    (resident_id, document_type, purpose, status, fee, fee_basis, date_requested)
SELECT resident_id, 'BarangayBusinessClearance', 'Sari-sari Store Renewal',
       'Pending', 200.00,
       'Business clearance - personal exemptions do not apply',
       DATE_SUB(NOW(), INTERVAL 1 DAY)
FROM residents WHERE last_name = 'Villanueva';

-- Liza: a PWD, so waived under RA 10754.
INSERT INTO document_requests
    (resident_id, document_type, purpose, status, fee, fee_basis, date_requested)
SELECT resident_id, 'CertificateOfGoodMoralCharacter', 'Scholarship Application',
       'Pending', 0.00, 'FREE - Person With Disability (RA 10754)', NOW()
FROM residents WHERE last_name = 'Santos-Reyes';

-- Carlo: a clearance for work ABROAD, so 200 pesos rather than 100. This is
-- the row that demonstrates why I needed the scope column at all.
INSERT INTO document_requests
    (resident_id, document_type, scope, purpose, status, fee, fee_basis, date_requested)
SELECT resident_id, 'BarangayClearance', 'Abroad',
       'Overseas Employment Requirement', 'Pending', 200.00,
       'Barangay Clearance - for employment abroad (Citizen''s Charter)', NOW()
FROM residents WHERE last_name = 'Peña';


-- =====================================================================
--  I CHECK THAT IT LOADED
-- =====================================================================
SELECT 'Residents'       AS table_name, COUNT(*) AS rows_loaded FROM residents
UNION ALL
SELECT 'Classifications', COUNT(*) FROM resident_classifications
UNION ALL
SELECT 'Requests',        COUNT(*) FROM document_requests;

SELECT sortable_name, age, purok, classifications, months_of_residency
FROM vw_residents_full
ORDER BY sortable_name;

SELECT reference_no, resident_name, document_type, scope, status, fee, is_paid
FROM vw_requests_full
ORDER BY request_id;
