# Pseudocode — integrating the drafts on the Fdraft baseline

*Written by Clint Wood Gado. This is a planning artifact: I write the plan
in pseudocode first, implement it in C#, and then delete this file. It stays
in the Git history so the professor can see the steps in order.*

## 0. The decisions this pseudocode implements

```text
BASELINE     = Fdraft (Frent): one Framework 4.8 WinForms project, the folder
               layout BusinessRules / CustomControls / Database / Forms /
               Interfaces / Models / UIHelpers / Views, and his working MySQL
               persistence (DatabaseSettings, DatabaseInitializer,
               MySqlBarangayRepository, embedded schema.sql).
RULES        = mine (leader_draft): FeeSchedule, the 24 document types, the
               Citizen's Charter rates, the waivers, RA 11032 aging,
               docs/07. Frent's 7-document classroom rates are NOT used.
UI           = Draft (Jonathan): flat sidebar, page title, status strip,
               six summary cards, three breakdown tables - drawn in MY
               palette (navy / blue / gold / red from the seal) with MY logo.
STORAGE      = MySQL first. If MySQL cannot be reached: explain and exit
               (Frent's rule). Storage=Memory stays for the RuleChecks
               harness and for a demo without XAMPP.
SCHEMA       = Frent's flat schema, extended with the columns my request
               model needs. My 01-schema.sql / 02-seed-data.sql retire.
COMMENTS     = first person, "I" = Clint; every file carries a header that
               names the part, the teammate/branch it came from, and my edits.
```

## 1. Startup (Program.cs)

```text
PROCEDURE Main
    enable visual styles, resolve fonts (AppTheme.Resolve)
    profile  <- BarangayProfile from App.config            (my rules)
    fees     <- FeeSchedule from App.config                (my rules)

    IF AppSettings.Storage = "Memory" THEN
        repository <- InMemoryBarangayRepository(fees)     (seeded)
    ELSE
        TRY
            settings <- DatabaseSettings.Load()            (Frent, via AppSettings)
            DatabaseInitializer.EnsureCreated(settings.ConnectionString)
            repository <- MySqlBarangayRepository(settings.ConnectionString, fees)
            IF settings.SeedSampleData AND repository.Residents is empty THEN
                SampleData.Seed(repository)
        CATCH RepositoryException ex
            show Frent's "Cannot connect to the database" message with ex.Message
            RETURN                                          (exit, no silent fallback)
    END IF

    run MainShell(repository, fees)
END
```

## 2. One repository skeleton, two storages (Database/RepositoryBase)

```text
ABSTRACT CLASS RepositoryBase IMPLEMENTS IBarangayRepository
    working set: residents[], requests[]          (both storages keep one)
    Fees: FeeSchedule

    -- queries: written ONCE for both storages
    FindResident(id)        -> first resident with that id or null
    SearchResidents(term)   -> blank term: everyone; else case-insensitive
                               match on full name, sortable name, purok,
                               contact, occupation
    ResidentsOfPurok(purok) -> residents whose purok equals (ignore case)
    GetRequestsByStatus(s)  -> all, or those with status s
    GetStatistics()         -> counts, collections, by-document, by-purok

    -- commands: skeleton here, persistence hook in the subclass
    AddResident(details)
        id <- InsertResident(details)              (hook)
        resident <- new Resident(id, first, last); Apply(resident, details)
        residents.add(resident); RETURN resident

    UpdateResident(resident, details)
        UpdateResidentRow(resident, details)       (hook)
        Apply(resident, details)

    RemoveResident(resident)
        DeleteResidentRow(resident)                (hook)
        requests.removeAll(belonging to resident); residents.remove(resident)

    CreateRequest(resident, type, purpose, input)
        RETURN File(resident, type, purpose, input, filedOn = now)

    File(resident, type, purpose, input, filedOn)          (internal)
        assessment <- Fees.Assess(resident, type, input)
        IF assessment.IsBlocked THEN THROW InvalidOperation(assessment.BlockReason)
        filedOn <- filedOn truncated to whole seconds (MySQL DATETIME precision)
        id <- InsertRequest(resident, type, purpose, input, assessment, filedOn)   (hook)
        request <- new DocumentRequest(id, resident, type, purpose, input, filedOn)
        request.ApplyAssessment(assessment)
        requests.add(request); resident.AddRequest(request); RETURN request

    SaveRequest(request)    -> UpdateRequestRow(request)   (hook)
    Reload()                -> virtual, default does nothing
    StorageDescription      -> abstract, shown in the status bar

    Apply(resident, details) -> copy the fourteen editable fields (ONE copy)
END

CLASS InMemoryBarangayRepository : RepositoryBase
    hooks: InsertResident/InsertRequest return next counter; the rest do nothing
    constructor(fees, seed = true): IF seed THEN SampleData.Seed(this)
    StorageDescription = "In-memory demo - nothing is saved"

CLASS MySqlBarangayRepository : RepositoryBase                (Frent's code)
    constructor(connectionString, fees): Reload()
    Reload(): read residents then requests in one connection; rebuild objects
              with DocumentRequest.Rehydrate; swap the working set only after
              BOTH reads succeeded
    InsertResident   -> parameterised INSERT, LAST_INSERT_ID()
    UpdateResidentRow-> parameterised UPDATE ... WHERE resident_id
    DeleteResidentRow-> DELETE (requests cascade in the schema)
    InsertRequest    -> INSERT with type name, status, fee, basis AND the
                        extended columns (scope, amount, hours, income,
                        detail, waiver flag, availed flag)
    UpdateRequestRow -> one transaction: UPDATE request row + UPDATE the
                        resident's has_availed_jobseeker flag
    every MySqlException -> RepositoryException with a sentence a clerk can act on
    StorageDescription = "MySQL - server/database"
```

## 3. Schema and first-run upgrade (Database/DatabaseInitializer + schema.sql)

```text
PROCEDURE EnsureCreated(connectionString)
    connect to the SERVER (no database) -> CREATE DATABASE IF NOT EXISTS
    connect to the database             -> run every statement in schema.sql
                                           (CREATE TABLE IF NOT EXISTS ...)
    FOR EACH column my request model added since Frent's first schema
        IF information_schema says document_requests lacks it THEN
            ALTER TABLE document_requests ADD COLUMN ...
    (so a database Frent already created keeps working)
END
```

## 4. Sample data, one copy (Database/SampleData)

```text
PROCEDURE Seed(repository : RepositoryBase)
    add my seven residents (real puroks, Peña, Santos-Reyes, ...)
    file the demo requests through repository.CreateRequest / File(backdated)
    FOR EACH request walked through the workflow (process, ready, pay, release)
        repository.SaveRequest(request)     (no-op in memory, UPDATE in MySQL)
END
```

## 5. Screens never crash on a database error (Views/ViewBase)

```text
CLASS ViewBase
    Repository : IBarangayRepository        (every view already needed it)

    FUNCTION Persist(work, whatFailed) : bool
        TRY work(); RETURN true
        CATCH RepositoryException ex
            Dialog.Error("<whatFailed> was not saved. " + ex.Message)
            Repository.Reload()             (screen never shows what the DB lacks)
            RETURN false
END

ResidentsView: Add / Edit / Delete / New request go through Persist, then reload the grid
RequestsView : Step(move) = move(request) in memory -> Persist(SaveRequest)
               RecordPayment = dialog OK -> Step(r => r.RecordPayment(receiptNo))   (bug fix)
```

## 6. Draft's dashboard in my palette (CustomControls/SummaryCard + Views/DashboardView)

```text
CLASS SummaryCard : Button                  (a Button so Tab / Enter / Space work)
    Heading (uppercase, small, muted)  Value (large, primary colour)
    paints a white card with a thin border, shows the hand cursor

DashboardView
    3 x 2 table of SummaryCards: Residents, Requests, Pending,
        Ready for release, Issued free, Collected
    3 tables underneath: by status, by document, by purok
    clicking a card or a status/purok row navigates with that filter
    OnShown(): read GetStatistics() once and fill every figure from it
```

## 7. Attribution headers (every source file)

```text
FOR EACH .cs / .sql file
    prepend
        PART    <what this file is for>
        ORIGIN  <branch - teammate whose work it is>
        EDITS   <what I changed>
        VOICE   every comment is mine (Clint), in the first person
```
