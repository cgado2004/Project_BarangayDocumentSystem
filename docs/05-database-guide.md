# Database guide — for my group-mates

*Written by Clint Wood Gado.*

I wrote this because the database is the part most likely to be
misunderstood, and I would rather over-explain it once than have someone
guess. If you only read one thing, read §1.

---

## 1. You do NOT need MySQL to run our app

The application starts on built-in sample data. Seven residents and seven
requests are already there when you press F5. **No database, no XAMPP, no
setup.**

That is deliberate. Our professor, or any of you, should be able to clone the
folder and have it working in under a minute.

The database is the *next* step, not a requirement. Only follow §3 if you are
specifically working on the MySQL part.

---

## 2. Which storage the app is using

One line in `BarangayDocumentSystem/App.config` decides:

```xml
<add key="Storage" value="Memory" />
```

| Value | What happens |
|---|---|
| `Memory` | Sample data. Works everywhere. **This is the default.** |
| `MySQL` | Uses the database described below. |

**Please leave it on `Memory` unless you are testing the database.** If you
commit it as `MySQL`, everyone else gets a warning box on startup.

---

## 3. Setting up MySQL

### 3.1 Start the server

In XAMPP, open the Control Panel and press **Start** next to **MySQL**. That
is all — you do not need Apache for this.

### 3.2 Run the two scripts, in order

Open **phpMyAdmin** (`http://localhost/phpmyadmin`) or MySQL Workbench, then:

1. Open the **SQL** tab.
2. Paste the whole of **`db/01-schema.sql`** and run it.
   This creates the database, the four tables, the constraints, the triggers
   and the views. It drops and recreates everything, so it is safe to run
   again any time.
3. Paste the whole of **`db/02-seed-data.sql`** and run it.
   This loads the same seven residents the app shows in Memory mode.

Both scripts print a result at the end so you can confirm they worked.
`02-seed-data.sql` should report **7 residents, 5 classifications,
7 requests**.

### 3.3 Read the version check

The last thing `01-schema.sql` prints is this:

```sql
SELECT VERSION(), ... AS check_constraint_support;
```

**Read that answer.** It tells you whether your server actually enforces
`CHECK` constraints. MySQL silently *ignored* them until version 8.0.16, and
XAMPP often ships MariaDB. On an older server the schema looks validated but
will accept nonsense. That is why the two rules that really matter are
triggers instead — triggers work on every version.

### 3.4 Point the app at it

In `App.config`, set `Storage` to `MySQL` and check the connection string:

```xml
<add name="BarangayDb"
     connectionString="Server=localhost;Port=3306;Database=barangay_magugpo;Uid=root;Pwd=;..." />
```

The default is the XAMPP default — user `root` with **no password**. If you
set a MySQL password, put it after `Pwd=`.

---

## 4. Honest status — please do not misread this

**The C# class that talks to MySQL is not in this version yet.**

The scripts in `db/` are complete and correct. What is missing is
`MySqlBarangayRepository`, the C# side that reads and writes those tables.

If you set `Storage=MySQL` today, the app shows a message saying exactly that
and starts on the sample data instead. It does not pretend, and it does not
crash.

When the repository is added, the only code change is one line in
`Program.cs`:

```csharp
return new MySqlBarangayRepository(connection, fees);
```

Nothing else in the program changes, because every screen only ever sees
`IBarangayRepository`. That is the entire reason the interface exists.

**I also have to be straight about this: I have never run these scripts.**
There was no MySQL server on the machine I wrote them on. Every statement has
been checked against the MySQL dialect by a parser, and the 24 document-type
values were verified to match the C# enum exactly, in the same order — but
"it parses" is not "it runs". **Please run them and tell me what breaks.**

---

## 5. What the tables look like

Four tables. The full diagram is in `docs/02-erd.svg`.

| Table | What it holds |
|---|---|
| `residents` | One row per person in the registry |
| `classification_types` | The five tags (senior, PWD, indigent, student, solo parent) |
| `resident_classifications` | Which residents have which tags — one row each |
| `document_requests` | One row per document requested |

Plus three views that do the joins for you:

| View | Use it for |
|---|---|
| `vw_residents_full` | Residents with age and classifications on one line |
| `vw_requests_full` | Requests with the reference number and resident name |
| `vw_dashboard_statistics` | Every dashboard figure, as a single row |

---

## 6. Four decisions I made, and why

These are the ones most likely to be questioned, so here is my reasoning in
advance.

**1. I used native `ENUM` instead of `VARCHAR` with a `CHECK`.**
The usual advice is the opposite. I went this way because `CHECK` was silently
ignored before MySQL 8.0.16, and XAMPP ships MariaDB — so a `VARCHAR + CHECK`
schema would *look* rigorously validated while accepting `'Pendinggg'` or an
emoji. A safety net drawn on the wall is worse than no net. `ENUM` is enforced
on every version. The cost is that adding a document type needs an
`ALTER TABLE`, which I accept — document types change by ordinance, rarely.

**2. I store enum NAMES, never numbers.**
If I stored `0` and `1` and somebody reordered the C# enum, every existing row
would silently change meaning and nothing would warn us. Storing
`'BarangayClearance'` makes that impossible. **This is why the order of values
in the SQL `ENUM` must stay identical to the C# enum — I check this.**

**3. Classifications are a separate table, not one number.**
In C# it is a `[Flags]` enum, so senior + PWD is the single number 3. Copying
that into an `INT` would break First Normal Form and make "list every senior
citizen" a bitmask scan that cannot use an index. One row per tag fixes both,
and gives somewhere to record the PWD ID number.

**4. `fee` is `DECIMAL(10,2)`, never `FLOAT`.**
Binary floating point cannot hold 0.10 exactly, so money drifts as it adds up.
On fees that is indefensible.

---

## 7. If something goes wrong

| Message | What it means | Fix |
|---|---|---|
| `Error 1045 Access denied` | Wrong username or password | Check `Uid=` and `Pwd=` in App.config |
| `Error 1049 Unknown database` | The schema was never created | Run `db/01-schema.sql` |
| `Error 2002 / 2003 Can't connect` | The server is not running | Start MySQL in XAMPP |
| `Error 1452 Cannot add foreign key` | You ran the seed before the schema | Run `01-schema.sql` first |
| `Error 1364 Field doesn't have a default` | Rows inserted by hand, missing a required column | Use the seed script as your template |
| Triggers rejected on import | Some tools mishandle `DELIMITER` | Run the script in phpMyAdmin's SQL tab or Workbench, not a bulk importer |

---

## 8. Do not do these

- **Do not edit `fee` directly in phpMyAdmin.** The fee and its legal basis
  are written together by `FeeSchedule`. Changing one without the other means
  the certificate prints an amount that contradicts its own stated reason.
- **Do not reorder the `document_type` ENUM.** See decision 2 above.
- **Do not set a request to `Released` by hand.** The trigger will stop you if
  the fee is unpaid, which is correct — that rule exists because releasing a
  document without recording the payment is exactly the audit problem the
  system is meant to prevent.
- **Do not commit `App.config` with `Storage=MySQL`.** It breaks the app for
  everyone who has not set up a database.
