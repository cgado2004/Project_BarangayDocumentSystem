-- ============================================================================
--  01-schema.sql  ( Microsoft SQL Server / LocalDB / Express )
--  The same tables as the MySQL script, written the way SQL Server wants them.
--
--  The program creates the database and runs this by itself on the first
--  start, so nothing has to be run by hand. Everything here is inside an
--  "if it is not there yet" check, which is why running it twice is harmless.
--
--  The rules I follow in both engines are the same:
--   * enum columns store the NAME, never the number;
--   * nobody deletes a resident or a receipt - they are deactivated;
--   * every value that comes from the screen travels as a parameter.
-- ============================================================================

IF OBJECT_ID('dbo.residents', 'U') IS NULL
CREATE TABLE dbo.residents (
    resident_id             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    first_name              NVARCHAR(100)  NOT NULL,
    middle_name             NVARCHAR(100)  NOT NULL DEFAULT '',
    last_name               NVARCHAR(100)  NOT NULL,
    suffix                  NVARCHAR(20)   NOT NULL DEFAULT '',
    date_of_birth           DATE           NOT NULL,
    gender                  NVARCHAR(20)   NOT NULL,
    civil_status            NVARCHAR(20)   NOT NULL,
    purok                   NVARCHAR(100)  NOT NULL DEFAULT '',
    contact_number          NVARCHAR(30)   NOT NULL DEFAULT '',
    occupation              NVARCHAR(100)  NOT NULL DEFAULT '',
    date_of_residency       DATE           NOT NULL,
    is_registered_voter     BIT            NOT NULL DEFAULT 0,
    classification          INT            NOT NULL DEFAULT 0,
    is_student_fee_category BIT            NOT NULL DEFAULT 0,
    is_business_owner       BIT            NOT NULL DEFAULT 0,
    is_head_of_family       BIT            NOT NULL DEFAULT 0,
    residency_status        NVARCHAR(20)   NOT NULL DEFAULT 'Newcomer',
    record_state            NVARCHAR(20)   NOT NULL DEFAULT 'Active',
    state_reason            NVARCHAR(255)  NOT NULL DEFAULT '',
    state_changed_on        DATETIME2(0)   NULL,
    state_changed_by        NVARCHAR(100)  NOT NULL DEFAULT '',
    has_availed_jobseeker   BIT            NOT NULL DEFAULT 0,
    created_on              DATETIME2(0)   NOT NULL,
    created_by              NVARCHAR(100)  NOT NULL DEFAULT '',
    updated_on              DATETIME2(0)   NULL,
    updated_by              NVARCHAR(100)  NOT NULL DEFAULT ''
);
GO

IF OBJECT_ID('dbo.dependents', 'U') IS NULL
CREATE TABLE dbo.dependents (
    dependent_id      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    head_resident_id  INT            NOT NULL,
    full_name         NVARCHAR(150)  NOT NULL,
    relation          NVARCHAR(30)   NOT NULL,
    date_of_birth     DATE           NOT NULL,
    is_studying       BIT            NOT NULL DEFAULT 0,
    remarks           NVARCHAR(255)  NOT NULL DEFAULT '',
    created_on        DATETIME2(0)   NOT NULL,
    created_by        NVARCHAR(100)  NOT NULL DEFAULT '',
    CONSTRAINT fk_dependents_head FOREIGN KEY (head_resident_id)
        REFERENCES dbo.residents (resident_id) ON DELETE CASCADE
);
GO

IF OBJECT_ID('dbo.document_requests', 'U') IS NULL
CREATE TABLE dbo.document_requests (
    request_id                  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    reference_number            NVARCHAR(30)   NOT NULL,
    resident_id                 INT            NOT NULL,
    document_type               NVARCHAR(60)   NOT NULL,
    purpose                     NVARCHAR(500)  NOT NULL DEFAULT '',
    date_requested              DATETIME2(0)   NOT NULL,
    status                      NVARCHAR(30)   NOT NULL,
    fee                         DECIMAL(10,2)  NOT NULL DEFAULT 0,
    fee_basis                   NVARCHAR(500)  NOT NULL DEFAULT '',
    is_paid                     BIT            NOT NULL DEFAULT 0,
    official_receipt_no         NVARCHAR(30)   NOT NULL DEFAULT '',
    or_control_number           NVARCHAR(30)   NOT NULL DEFAULT '',
    payment_date                DATETIME2(0)   NULL,
    collected_by                NVARCHAR(100)  NOT NULL DEFAULT '',
    scope                       NVARCHAR(10)   NOT NULL DEFAULT 'Local',
    assessed_amount             DECIMAL(10,2)  NOT NULL DEFAULT 0,
    hours                       DECIMAL(6,2)   NOT NULL DEFAULT 0,
    gross_annual_income         DECIMAL(14,2)  NOT NULL DEFAULT 0,
    detail                      NVARCHAR(255)  NOT NULL DEFAULT '',
    apply_jobseeker_waiver      BIT            NOT NULL DEFAULT 0,
    availed_under_jobseeker_act BIT            NOT NULL DEFAULT 0,
    requires_validation         BIT            NOT NULL DEFAULT 0,
    filed_during_office_window  BIT            NOT NULL DEFAULT 0,
    business_name               NVARCHAR(150)  NOT NULL DEFAULT '',
    business_nature             NVARCHAR(150)  NOT NULL DEFAULT '',
    business_purok              NVARCHAR(100)  NOT NULL DEFAULT '',
    business_location           NVARCHAR(150)  NOT NULL DEFAULT '',
    business_ownership          NVARCHAR(60)   NOT NULL DEFAULT '',
    business_registration       NVARCHAR(50)   NOT NULL DEFAULT '',
    business_previous_permit    NVARCHAR(50)   NOT NULL DEFAULT '',
    business_employees          INT            NOT NULL DEFAULT 0,
    business_is_renewal         BIT            NOT NULL DEFAULT 0,
    date_released               DATETIME2(0)   NULL,
    released_by                 NVARCHAR(100)  NOT NULL DEFAULT '',
    received_by                 NVARCHAR(100)  NOT NULL DEFAULT '',
    rejection_reason            NVARCHAR(500)  NOT NULL DEFAULT '',
    remarks                     NVARCHAR(500)  NOT NULL DEFAULT '',
    last_status_on              DATETIME2(0)   NULL,
    last_status_by              NVARCHAR(100)  NOT NULL DEFAULT '',
    CONSTRAINT fk_requests_resident FOREIGN KEY (resident_id)
        REFERENCES dbo.residents (resident_id),
    CONSTRAINT uq_requests_reference UNIQUE (reference_number)
);
GO

IF OBJECT_ID('dbo.request_status_history', 'U') IS NULL
CREATE TABLE dbo.request_status_history (
    history_id  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    request_id  INT            NOT NULL,
    status      NVARCHAR(30)   NOT NULL,
    changed_on  DATETIME2(0)   NOT NULL,
    changed_by  NVARCHAR(100)  NOT NULL DEFAULT '',
    reason      NVARCHAR(500)  NOT NULL DEFAULT '',
    CONSTRAINT fk_history_request FOREIGN KEY (request_id)
        REFERENCES dbo.document_requests (request_id) ON DELETE CASCADE
);
GO

IF OBJECT_ID('dbo.receipt_series', 'U') IS NULL
CREATE TABLE dbo.receipt_series (
    series_id     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    series_code   NVARCHAR(20)   NOT NULL,
    control_from  NVARCHAR(30)   NOT NULL DEFAULT '',
    control_to    NVARCHAR(30)   NOT NULL DEFAULT '',
    issued_to     NVARCHAR(100)  NOT NULL DEFAULT '',
    issued_on     DATE           NOT NULL,
    is_active     BIT            NOT NULL DEFAULT 1,
    remarks       NVARCHAR(255)  NOT NULL DEFAULT '',
    CONSTRAINT uq_series_code UNIQUE (series_code)
);
GO

IF OBJECT_ID('dbo.official_receipts', 'U') IS NULL
CREATE TABLE dbo.official_receipts (
    receipt_id      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    or_number       NVARCHAR(30)   NOT NULL,
    series_code     NVARCHAR(20)   NOT NULL DEFAULT '',
    control_number  NVARCHAR(30)   NOT NULL DEFAULT '',
    or_date         DATE           NOT NULL,
    payer_name      NVARCHAR(150)  NOT NULL DEFAULT '',
    amount          DECIMAL(10,2)  NOT NULL DEFAULT 0,
    method          NVARCHAR(20)   NOT NULL DEFAULT 'Cash',
    request_id      INT            NULL,
    collected_by    NVARCHAR(100)  NOT NULL DEFAULT '',
    remarks         NVARCHAR(255)  NOT NULL DEFAULT '',
    is_void         BIT            NOT NULL DEFAULT 0,
    void_reason     NVARCHAR(255)  NOT NULL DEFAULT '',
    voided_on       DATETIME2(0)   NULL,
    voided_by       NVARCHAR(100)  NOT NULL DEFAULT '',
    created_on      DATETIME2(0)   NOT NULL,
    CONSTRAINT fk_receipts_request FOREIGN KEY (request_id)
        REFERENCES dbo.document_requests (request_id),
    CONSTRAINT uq_receipt_or UNIQUE (series_code, or_number)
);
GO

IF OBJECT_ID('dbo.user_accounts', 'U') IS NULL
CREATE TABLE dbo.user_accounts (
    user_id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    username             NVARCHAR(50)   NOT NULL,
    full_name            NVARCHAR(150)  NOT NULL DEFAULT '',
    role                 NVARCHAR(30)   NOT NULL,
    position             NVARCHAR(100)  NOT NULL DEFAULT '',
    password_hash        NVARCHAR(255)  NOT NULL DEFAULT '',
    password_salt        NVARCHAR(100)  NOT NULL DEFAULT '',
    hash_iterations      INT            NOT NULL DEFAULT 100000,
    must_change_password BIT            NOT NULL DEFAULT 1,
    is_active            BIT            NOT NULL DEFAULT 1,
    failed_attempts      INT            NOT NULL DEFAULT 0,
    locked_until         DATETIME2(0)   NULL,
    last_login_on        DATETIME2(0)   NULL,
    last_login_machine   NVARCHAR(100)  NOT NULL DEFAULT '',
    created_on           DATETIME2(0)   NOT NULL,
    created_by           NVARCHAR(100)  NOT NULL DEFAULT '',
    updated_on           DATETIME2(0)   NULL,
    updated_by           NVARCHAR(100)  NOT NULL DEFAULT '',
    CONSTRAINT uq_user_username UNIQUE (username)
);
GO

IF OBJECT_ID('dbo.activity_log', 'U') IS NULL
CREATE TABLE dbo.activity_log (
    log_id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    occurred_on       DATETIME2(0)   NOT NULL,
    username          NVARCHAR(50)   NOT NULL DEFAULT '',
    role              NVARCHAR(30)   NOT NULL DEFAULT '',
    module            NVARCHAR(30)   NOT NULL DEFAULT '',
    action            NVARCHAR(60)   NOT NULL DEFAULT '',
    target_type       NVARCHAR(60)   NOT NULL DEFAULT '',
    target_reference  NVARCHAR(60)   NOT NULL DEFAULT '',
    details           NVARCHAR(500)  NOT NULL DEFAULT '',
    machine_name      NVARCHAR(100)  NOT NULL DEFAULT ''
);
GO

IF OBJECT_ID('dbo.reference_counters', 'U') IS NULL
CREATE TABLE dbo.reference_counters (
    counter_year  INT NOT NULL PRIMARY KEY,
    last_number   INT NOT NULL DEFAULT 0
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_requests_status')
    CREATE INDEX idx_requests_status ON dbo.document_requests (status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_requests_filed')
    CREATE INDEX idx_requests_filed ON dbo.document_requests (date_requested);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_receipts_date')
    CREATE INDEX idx_receipts_date ON dbo.official_receipts (or_date);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_log_date')
    CREATE INDEX idx_log_date ON dbo.activity_log (occurred_on);
GO
