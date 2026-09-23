-- Creates the database tables.
CREATE TABLE dbo.AppState (
    Id int NOT NULL CONSTRAINT PK_AppState PRIMARY KEY CONSTRAINT CK_AppState_Id CHECK (Id = 1),
    SchemaVersion int NOT NULL,
    SampleDataInitialized bit NOT NULL
);
INSERT INTO dbo.AppState (Id, SchemaVersion, SampleDataInitialized) VALUES (1, 1, 0);

CREATE TABLE dbo.Residents (
    ResidentId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Residents PRIMARY KEY,
    Version int NOT NULL CONSTRAINT DF_Residents_Version DEFAULT 1,
    FirstName nvarchar(80) NOT NULL,
    MiddleName nvarchar(80) NOT NULL,
    LastName nvarchar(80) NOT NULL,
    Suffix nvarchar(20) NOT NULL,
    DateOfBirth date NOT NULL,
    Gender int NOT NULL,
    CivilStatus int NOT NULL,
    Purok nvarchar(60) NOT NULL,
    Address nvarchar(250) NOT NULL,
    ContactNumber nvarchar(15) NOT NULL,
    Occupation nvarchar(100) NOT NULL,
    DateOfResidency date NOT NULL,
    IsRegisteredVoter bit NOT NULL,
    IsSeniorCitizen bit NOT NULL,
    IsPersonWithDisability bit NOT NULL,
    IsIndigent bit NOT NULL,
    IsStudent bit NOT NULL,
    IsSoloParent bit NOT NULL,
    HasUsedJobseekerBenefit bit NOT NULL,
    CONSTRAINT CK_Residents_Dates CHECK (DateOfResidency >= DateOfBirth),
    CONSTRAINT CK_Residents_Gender CHECK (Gender BETWEEN 0 AND 1),
    CONSTRAINT CK_Residents_CivilStatus CHECK (CivilStatus BETWEEN 0 AND 4)
);

CREATE TABLE dbo.DocumentRequests (
    RequestId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentRequests PRIMARY KEY,
    Version int NOT NULL CONSTRAINT DF_Requests_Version DEFAULT 1,
    ResidentId int NOT NULL,
    DocumentType int NOT NULL,
    DocumentName nvarchar(120) NOT NULL,
    Purpose nvarchar(300) NOT NULL,
    BusinessName nvarchar(120) NOT NULL,
    BusinessAddress nvarchar(250) NOT NULL,
    BusinessNature nvarchar(150) NOT NULL,
    DateRequested datetime2(7) NOT NULL,
    DateReleased datetime2(7) NULL,
    Status int NOT NULL,
    Fee decimal(12,2) NOT NULL,
    FeeBasis nvarchar(1000) NOT NULL,
    IsPaid bit NOT NULL,
    OfficialReceiptNumber nvarchar(50) COLLATE Latin1_General_100_CI_AS NULL,
    DatePaid datetime2(7) NULL,
    RejectionReason nvarchar(300) NOT NULL,
    ReleasedDocumentText nvarchar(max) NOT NULL,
    CONSTRAINT FK_Requests_Residents FOREIGN KEY (ResidentId) REFERENCES dbo.Residents(ResidentId),
    CONSTRAINT CK_Requests_Type CHECK (DocumentType BETWEEN 0 AND 6),
    CONSTRAINT CK_Requests_Status CHECK (Status BETWEEN 0 AND 4),
    CONSTRAINT CK_Requests_Fee CHECK (Fee >= 0),
    CONSTRAINT CK_Requests_Payment CHECK (
        (IsPaid = 0 AND OfficialReceiptNumber IS NULL AND DatePaid IS NULL) OR
        (IsPaid = 1 AND Fee > 0 AND OfficialReceiptNumber IS NOT NULL AND LEN(LTRIM(RTRIM(OfficialReceiptNumber))) > 0 AND DatePaid IS NOT NULL)),
    CONSTRAINT CK_Requests_Release CHECK (
        Status <> 3 OR (DateReleased IS NOT NULL AND LEN(ReleasedDocumentText) > 0 AND (Fee = 0 OR IsPaid = 1))),
    CONSTRAINT CK_Requests_Rejection CHECK (Status <> 4 OR LEN(LTRIM(RTRIM(RejectionReason))) > 0)
);
CREATE UNIQUE INDEX UX_Requests_Receipt ON dbo.DocumentRequests(OfficialReceiptNumber) WHERE IsPaid = 1;
CREATE UNIQUE INDEX UX_Requests_Jobseeker ON dbo.DocumentRequests(ResidentId) WHERE DocumentType = 5 AND Status <> 4;
CREATE INDEX IX_Requests_Resident ON dbo.DocumentRequests(ResidentId);

-- Keeps the original resident details.
CREATE TABLE dbo.RequestResidentSnapshots (
    RequestId int NOT NULL CONSTRAINT PK_RequestResidentSnapshots PRIMARY KEY,
    FirstName nvarchar(80) NOT NULL,
    MiddleName nvarchar(80) NOT NULL,
    LastName nvarchar(80) NOT NULL,
    Suffix nvarchar(20) NOT NULL,
    DateOfBirth date NOT NULL,
    Gender int NOT NULL,
    CivilStatus int NOT NULL,
    Purok nvarchar(60) NOT NULL,
    Address nvarchar(250) NOT NULL,
    ContactNumber nvarchar(15) NOT NULL,
    Occupation nvarchar(100) NOT NULL,
    DateOfResidency date NOT NULL,
    IsRegisteredVoter bit NOT NULL,
    IsSeniorCitizen bit NOT NULL,
    IsPersonWithDisability bit NOT NULL,
    IsIndigent bit NOT NULL,
    IsStudent bit NOT NULL,
    IsSoloParent bit NOT NULL,
    HasUsedJobseekerBenefit bit NOT NULL,
    CONSTRAINT FK_Snapshots_Requests FOREIGN KEY (RequestId) REFERENCES dbo.DocumentRequests(RequestId)
);
