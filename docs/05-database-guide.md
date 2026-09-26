# Database guide — for my group-mates

*Written by Clint Wood Gado. The persistence it describes is Frent Dhieniel
Raborar's (`Fdraft`), extended for the v3.1 request model.*

I wrote this because the database is the part most likely to be
misunderstood, and I would rather over-explain it once than have someone
guess. If you only read one thing, read §1.

---

## 1. Setup is: start MySQL, press F5

The app now runs on MySQL by default, and it sets the database up itself:

1. In the XAMPP Control Panel press **Start** next to **MySQL**. You do not
   need Apache.
2. Press **F5** in Visual Studio.

On the first run the program creates the database `barangay_db`, creates the
two tables, and — because the database is empty — loads my seven sample
residents and their requests so the screens are not blank. After that it
only reads and writes.

There is nothing to paste into phpMyAdmin. The `01-schema.sql` and
`02-seed-data.sql` scripts I wrote for v3.1 are gone (§6 says why).

If MySQL is **not** running, the program tells you exactly that and closes:

> Cannot reach the MySQL server. Check that MySQL is running (XAMPP: start
> MySQL in the control panel) …

It does **not** quietly switch to sample data. That was Frent's decision
and I kept it: a clerk must never spend a morning typing into a store that
forgets everything at closing time without being told.

---

## 2. Which storage the app is using

One line in `BarangayDocumentSystem/App.config` decides:

```xml
<add key="Storage" value="MySQL" />
```

| Value | What happens |
|---|---|
| `MySQL` | The real database. **This is the default.** Created on first run. |
| `Memory` | The built-in sample data. Nothing is saved. For a laptop with no XAMPP. |

The status bar at the bottom right always says which one is running:
`MySQL — localhost/barangay_db` or `In-memory demo — nothing is saved`.

The RuleChecks harness (`tests/RuleChecks`) always uses the in-memory store,
so it runs on a build machine without MySQL.

---

## 3. The connection string

```xml
<add name="BarangayDb"
     connectionString="Server=localhost;Port=3306;Database=barangay_db;User ID=root;Password=;CharSet=utf8mb4;" />
```

That is the XAMPP default — user `root`, **no password**. It is fine for the
class demo and not fine anywhere real.

**Never type a real password into this file.** The repository is public.
Set the environment variable `BARANGAY_DB_CONNECTION` instead; when it
exists it overrides the file. In PowerShell, before starting the app:

```powershell
$env:BARANGAY_DB_CONNECTION = "Server=localhost;Port=3306;Database=barangay_db;User ID=barangay;Password=...;CharSet=utf8mb4;"
```

The order of precedence is in `Database/DatabaseSettings.cs`: environment
variable, then `App.config`, then the built-in default.

After a build, the file the program actually reads is
`bin\Debug\BarangayDocumentSystem.exe.config`. Edit `App.config` and
rebuild, or edit that file directly to change a running install.

---

## 4. How start-up works (so you can debug it)

`Program.CreateRepository` does four things, in this order:

| Step | Class | What it does |
|---|---|---|
| 1 | `DatabaseSettings.Load()` | Works out the connection string and the seed switch |
| 2 | `DatabaseInitializer.EnsureCreated()` | Connects to the **server**, `CREATE DATABASE IF NOT EXISTS`; connects to the **database**, runs the embedded `schema.sql` (`CREATE TABLE IF NOT EXISTS` ×2); then `EnsureColumns` adds any column an older table lacks |
| 3 | `new MySqlBarangayRepository(...)` | Loads every resident and request into memory (`Reload`) |
| 4 | `SampleData.Seed(...)` | Only if `SeedSampleData=true` **and** there are zero residents |

Every MySQL error on the way becomes a `RepositoryException` with a sentence
you can act on (`MySqlBarangayRepository.Describe` maps the error numbers).

While the app is running, every write goes: screen → `ViewBase.Persist` →
repository → MySQL. If MySQL refuses, `Persist` shows the reason and asks the
repository to `Reload()`, so the grid returns to what is really on disk.

---

## 5. What the tables look like

Two tables. The diagram is in `docs/09-object-model.md` §5 (Mermaid, so it
is always current).

| Table | What it holds |
|---|---|
| `residents` | One row per person. Classification is the same bit-flag integer as the C# enum (1 Senior, 2 PWD, 4 Indigent, 8 Student, 16 Solo Parent). `has_availed_jobseeker` is the RA 11261 once-only flag. |
| `document_requests` | One row per request: type, purpose, dates, status, **fee and fee basis frozen at filing time**, payment, and the inputs the fee was assessed from (scope, assessed amount, hours, gross income, detail, jobseeker flags). |

Deleting a resident deletes their requests (`ON DELETE CASCADE`), the same
rule the repository applies to its working set.

Enum columns store the enum **name** (`'Female'`, `'ReadyForRelease'`), never
its number. Reordering an enum in C# can therefore never silently change
what a row means, and the table is readable in phpMyAdmin.

---

## 6. Why my v3.1 scripts were retired, and what survived of them

In v3.1 I wrote a four-table schema by hand (`residents`,
`classification_types`, `resident_classifications`, `document_requests`),
with native `ENUM` columns, three views, and two triggers — and a seed script
to go with it. Frent's `Fdraft`, meanwhile, had a two-table schema that the
program creates by itself. When I integrated the branches I had to pick one,
because two schemas for one program is exactly the duplication DRY warns
about, and I picked Frent's. My reasons, so nobody thinks it was arbitrary:

| My v3.1 decision | What happened to it |
|---|---|
| **Native `ENUM` columns** so a bad value is rejected on every MySQL/MariaDB version | Dropped. The program is the only writer, and it only ever writes `enum.ToString()`. Adding a document type is now a C# change, not an `ALTER TABLE`. |
| **Store enum names, never numbers** | **Kept.** It is Frent's convention too. |
| **Classifications in a junction table** (1NF, indexable) | Dropped for now. The C# model is a `[Flags]` enum and every screen and rule reads it as one; a junction table would mean mapping code on every load and save for a query nobody runs yet. Recorded here as the upgrade to make when "list every senior citizen" needs an index. |
| **`fee` is `DECIMAL(10,2)`, never `FLOAT`** | **Kept.** Binary floating point cannot hold 0.10; money must not drift. |
| **Trigger: no `Released` while unpaid** | Moved into the one place it already lived: `DocumentRequest.Release()`. The database no longer duplicates a rule the model enforces. |
| **Views for the dashboard figures** | Replaced by `RepositoryBase.GetStatistics()`, which both storages share. |
| **Manual scripts you paste into phpMyAdmin** | Replaced by the embedded `schema.sql` that `DatabaseInitializer` runs. One copy, applied automatically, cannot get out of step with the code. |

What I added to Frent's schema: the seven `document_requests` columns my
`RequestInput` needs (`scope`, `assessed_amount`, `hours`,
`gross_annual_income`, `detail`, `apply_jobseeker_waiver`,
`availed_under_jobseeker_act`) and a wider `fee_basis`. A database created by
his earlier build is upgraded in place by `EnsureColumns` — nobody has to
drop a database that already has real residents in it.

---

## 7. If something goes wrong

| What you see | What it means | Fix |
|---|---|---|
| "Cannot reach the MySQL server" (2002 / 2003 / 2013 / 1042) | The server is not running, or the port is wrong | Start MySQL in XAMPP; check `Server=` and `Port=` |
| "MySQL refused the user name or password" (1044 / 1045) | Wrong credentials | Check `User ID=` and `Password=`, or set `BARANGAY_DB_CONNECTION` |
| "The database does not exist yet" (1049) | Rare: the database vanished between creation and loading | Start the program again; it recreates it |
| "Column 'x' holds the value 'y' which this version does not recognise" | Somebody edited an enum column by hand | Fix the row in phpMyAdmin to a valid enum name |
| "… was not saved. …" while working | A write failed after start-up | Read the reason; the grid has already been reloaded from the database |
| The status bar says `In-memory demo` | `Storage` is `Memory` | Set it to `MySQL` and restart |

---

## 8. Do not do these

- **Do not edit `fee` or `fee_basis` directly in phpMyAdmin.** They are
  written together by `FeeSchedule` at filing time. Changing one without the
  other means the certificate prints an amount that contradicts its own
  stated reason.
- **Do not set a request to `Released` by hand.** `DocumentRequest.Release()`
  refuses an unpaid fee for a reason: releasing a document without recording
  the payment is exactly the audit problem the system exists to prevent.
- **Do not put a password in `App.config`.** Use the environment variable.
- **Do not add a column by hand.** Add it to `schema.sql` *and* to the
  `RequestColumnUpgrades` list in `DatabaseInitializer`, so a fresh database
  and an existing one end up identical.
