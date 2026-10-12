# Database guide — for my group-mates

*Written by Clint Wood Gado. The persistence underneath is Frent Dhieniel
Raborar's idea (`Fdraft`), rebuilt for the re-designed system.*

I wrote this because the database is the part most likely to be
misunderstood, and I would rather over-explain it once than have someone
guess. If you only read one thing, read §1.

---

## 1. Setup is: start MySQL, press F5

The app runs on MySQL by default, and it sets the database up itself:

1. In the XAMPP Control Panel press **Start** next to **MySQL**. You do not
   need Apache.
2. Press **F5** in Visual Studio.

On the first run the program creates the database
`barangay_document_system`, creates the eight tables, installs the eleven
stored procedures, creates the first two accounts from `App.config`, and —
because the database is empty — loads the sample residents so the screens
are not blank. After that it only reads and writes.

There is nothing to paste into phpMyAdmin. The scripts live inside the
program as embedded resources (`Database/Scripts/MySql/01-schema.sql` and
`02-procedures.sql`), so the program always installs the schema that matches
its own code.

If MySQL is **not** running, the program tells you exactly that and closes.
It does **not** quietly switch to sample data — a clerk must never spend a
morning typing into a store that forgets everything at closing time without
being told.

---

## 2. Which storage the app is using

One line in `BarangayDocumentSystem/App.config` decides:

```xml
<add key="Storage" value="MySQL" />
```

| Value | What happens |
|---|---|
| `MySQL` | The real database. **This is the default.** Created on first run. |
| `SqlServer` | The same program on SQL Server (Express or LocalDB is fine). The second connection string is used, and the SQL Server scripts are installed the same way. |
| `Memory` | The built-in sample data. Nothing is saved. For a laptop with no database server at all. |

The status bar at the bottom right always says which one is running and
which database it is pointing at.

The RuleChecks harness (`tests/RuleChecks`) always uses the in-memory store,
so it runs on a build machine without MySQL.

---

## 3. The connection string

```xml
<add name="BarangayDb"
     connectionString="Server=localhost;Port=3306;Database=barangay_document_system;Uid=root;Pwd=;CharSet=utf8mb4;SslMode=None;AllowUserVariables=True;" />
```

That is the XAMPP default — user `root`, **no password**. It is fine for
the class demo and not fine anywhere real.

**Never type a real password into this file.** The repository is public.
Set the environment variable `BARANGAY_DB_CONNECTION` instead; when it
exists it overrides the file. In PowerShell, before starting the app:

```powershell
$env:BARANGAY_DB_CONNECTION = "Server=localhost;Port=3306;Database=barangay_document_system;Uid=barangay;Pwd=...;CharSet=utf8mb4;"
```

(For SQL Server the variable is `BARANGAY_DB_CONNECTION_SQLSERVER`.)

After a build, the file the program actually reads is
`bin\Debug\BarangayDocumentSystem.exe.config`. Edit `App.config` and
rebuild, or edit that file directly to change a running install. There is
also an option to seal the connection string with Windows DPAPI —
`Security.ProtectConnectionString` in `App.config`.

---

## 4. How start-up works (so you can debug it)

`Program.BuildRepository` does four things, in this order:

| Step | Class | What it does |
|---|---|---|
| 1 | `AppConfig.Load()` | Reads every setting — fees, rules, the storage choice |
| 2 | `DBContext.FromConfiguration()` | Builds the provider named by `Storage` (MySQL, SQL Server, or Memory) |
| 3 | `DatabaseInitializer.Run()` | Creates the database if missing, runs the schema script, upgrades an older database's missing columns **without dropping anything**, installs the stored procedures, seeds the first accounts and (if asked) the sample data |
| 4 | `SqlBarangayRepository` | The repository every screen talks to through `IBarangayRepository` |

Every database error on the way becomes a `RepositoryException` with a
sentence you can act on, and `AppLog` writes it to the `logs` folder next
to the program — passwords are scrubbed before anything is written.

While the app is running, every write goes: screen → service → repository →
database, all parameterised. If the database refuses, the screen shows the
reason in plain words.

---

## 5. What the tables look like

Eight tables (see `Database/Scripts/MySql/01-schema.sql`):

| Table | What it holds |
|---|---|
| `residents` | One row per person. Classifications are a bit-flag integer (1 senior, 2 PWD, 4 indigent, 8 solo parent, 16 4Ps). `record_state` is Active / Inactive / Archived — never deleted. |
| `dependents` | The head of family's dependents; goes away with the resident (`ON DELETE CASCADE`). |
| `document_requests` | One row per request: type, purpose, dates, status, **fee and fee basis frozen at filing time**, payment, business details, and the flags the office-window rule works from. |
| `request_status_history` | Every status change — including the automatic next-morning clearances — with who and why. |
| `receipt_series` | The official-receipt booklets: series code and control-number range. |
| `official_receipts` | Every receipt issued. A voided receipt stays in the register with its reason but stops counting as income. |
| `user_accounts` | Staff accounts: PBKDF2 password hash and salt, lock-out counter, must-change-password flag. |
| `activity_log` | Who did what, when, in which module. Append-only by design. |
| `reference_counters` | The running number behind each year's request reference numbers. |

Enum columns store the enum **name** (`'Female'`, `'ReadyForRelease'`),
never its number, so reordering an enum in C# can never silently change
what a row means — and the tables stay readable in phpMyAdmin.

---

## 6. The stored procedures

Eleven of them, with the same names and parameter order in both the MySQL
and SQL Server scripts: the dashboard summary, the eight report queries,
the activity-log search, and `sp_clear_waiting_requests` — the one that
clears yesterday's after-hours requests the next morning and writes the
clearance into each request's history.

The program tries the procedure first and falls back to the same query in
plain SQL if a procedure is missing, so a report always comes out.

**Important:** the initializer installs the procedures only when they are
missing. If you change a procedure in the script, drop it in phpMyAdmin
(`DROP PROCEDURE name;`) and restart the program, or run the new script by
hand — an existing database does not get the new version automatically.

---

## 7. If something goes wrong

| What you see | What it means | Fix |
|---|---|---|
| "Could not open the database connection" | The server is not running, or the port is wrong | Start MySQL in XAMPP; check `Server=` and `Port=` |
| Access denied for user | Wrong credentials | Check `Uid=` and `Pwd=`, or set `BARANGAY_DB_CONNECTION` |
| Unknown database | Rare: the database vanished between creation and use | Start the program again; it recreates it |
| "… was not saved …" while working | A write failed after start-up | Read the reason; the log folder has the detail |
| The status bar says `In-memory` | `Storage` is `Memory` | Set it to `MySQL` and restart |

---

## 8. Do not do these

- **Do not edit `fee` or `fee_basis` directly in phpMyAdmin.** They are
  written together by `FeeSchedule` at filing time. Changing one without
  the other means the certificate prints an amount that contradicts its
  own stated reason.
- **Do not set a request to `Released` by hand.** The model refuses to
  release an unpaid request for a reason: releasing a document without
  recording the payment is exactly the audit problem the system exists to
  prevent.
- **Do not delete rows.** Deactivate, archive, or void instead — a released
  certificate and an official receipt point back at their records.
- **Do not put a password in `App.config`.** Use the environment variable.
- **Do not add a column by hand.** Add it to the schema script *and* to the
  upgrade list in `DatabaseInitializer`, so a fresh database and an
  existing one end up identical.
