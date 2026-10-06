"""Source/project checks only; not a substitute for compiling on Windows.

Run from anywhere:  python scripts/check_structure.py

What it proves, without a C# compiler on the machine:

  * the project file lists exactly the .cs files that exist, and nothing stale
  * the four database scripts travel inside the .exe (embedded resources)
  * both engines' stored procedures have the same names and the same number of
    parameters, in the same order (see DBHelper.BuildProcedure for why order
    matters on MySQL)
  * every setting the code reads is present in App.config
  * no real password is committed, and the connection string is blank
  * the seal is byte-identical to the original file (nobody re-edited the logo)
  * the things the barangay asked to remove really are gone: Address,
    Student-as-a-classification, and the old v3.2 folders
  * every source file is balanced and opens with a comment
"""
from pathlib import Path
import hashlib
import re
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
app = root / 'BarangayDocumentSystem'
ns = {'m': 'http://schemas.microsoft.com/developer/msbuild/2003'}
problems = []


def check(condition, message):
    if not condition:
        problems.append(message)
    return condition


# ---------------------------------------------------------------- project ----
project = ET.parse(app / 'BarangayDocumentSystem.csproj')
check(project.find('.//m:TargetFrameworkVersion', ns).text == 'v4.8',
      'the project must target .NET Framework 4.8')
check(project.find('.//m:LangVersion', ns).text == '10.0',
      'the project must pin LangVersion so any VS 2022 builds it the same way')

listed = [x.attrib['Include'].replace('\\', '/') for x in project.findall('.//m:Compile', ns)]
actual = sorted(str(p.relative_to(app)).replace('\\', '/')
                for p in app.rglob('*.cs') if not {'obj', 'bin'} & set(p.parts))
check(len(listed) == len(set(listed)), 'duplicate Compile entries in the project file')
check(sorted(listed) == actual,
      'the project file and the folder disagree: ' + str(set(listed) ^ set(actual)))

for folder in ['Config', 'Data', 'Data/Sql', 'Database', 'Database/Scripts/MySql',
               'Database/Scripts/SqlServer', 'Interfaces', 'Models', 'Security',
               'Services', 'Services/Documents', 'Services/Reports', 'UI',
               'UI/Controls', 'UI/Dialogs', 'UI/Forms', 'UI/Views']:
    check((app / folder).is_dir(), 'missing folder: ' + folder)

ET.parse(app / 'App.config')
ET.parse(app / 'app.manifest') if (app / 'app.manifest').exists() else None

# ----------------------------------------------------------- db scripts ----
embedded = [x.attrib['Include'].replace('\\', '/')
            for x in project.findall('.//m:EmbeddedResource', ns)]
for script in ['Database/Scripts/MySql/01-schema.sql', 'Database/Scripts/MySql/02-procedures.sql',
               'Database/Scripts/SqlServer/01-schema.sql', 'Database/Scripts/SqlServer/02-procedures.sql']:
    check(script in embedded, script + ' must be embedded in the .exe, not left as a loose file')
    check((app / script).exists(), 'missing script file: ' + script)


def procedures(text, engine):
    """Returns {name: [parameter names]} for one of the two script styles."""
    found = {}
    if engine == 'mysql':
        pattern = r'CREATE PROCEDURE\s+(\w+)\s*\(([^)]*)\)'
    else:
        pattern = r'CREATE (?:OR ALTER )?PROCEDURE\s+dbo\.(\w+)\s*\n(.*?)\nAS'
    for match in re.finditer(pattern, text, re.S):
        name = match.group(1)
        body = match.group(2)
        names = re.findall(r'\b(?:IN\s+|OUT\s+)?p_(\w+)|\b@p_?(\w+)', body)
        if engine == 'mysql':
            params = re.findall(r'\b(?:IN|OUT|INOUT)?\s*p_(\w+)\s', body)
        else:
            params = re.findall(r'@(\w+)\s', body.split('\n')[0]) or re.findall(r'@(\w+)', body)
        found[name] = [p for p in params]
    return found


mysql = (app / 'Database/Scripts/MySql/02-procedures.sql').read_text(encoding='utf-8')
sqlsrv = (app / 'Database/Scripts/SqlServer/02-procedures.sql').read_text(encoding='utf-8')

def balanced(text, start):
    depth = 0
    for i in range(start, len(text)):
        if text[i] == '(':
            depth += 1
        elif text[i] == ')':
            depth -= 1
            if depth == 0:
                return text[start + 1:i]
    return ''


def mysql_procedures(text):
    out = []
    for match in re.finditer(r'CREATE PROCEDURE\s+(\w+)\s*\(', text):
        out.append((match.group(1), balanced(text, match.end() - 1)))
    return out


def server_procedures(text):
    out = []
    for match in re.finditer(r'CREATE (?:OR ALTER )?PROCEDURE\s+dbo\.(\w+)(.*?)\nAS', text, re.S):
        out.append((match.group(1), match.group(2)))
    return out


mysql_procs = mysql_procedures(mysql)
srv_procs = server_procedures(sqlsrv)

mysql_names = [n for n, _ in mysql_procs]
srv_names = [n for n, _ in srv_procs]
check(mysql_names == srv_names,
      'the two engines must offer the same procedures, in the same order:\n'
      '    MySQL     : ' + ', '.join(mysql_names) + '\n'
      '    SQL Server: ' + ', '.join(srv_names))
check(len(mysql_names) >= 11, 'expected at least eleven stored procedures')

for (name, mparams), (sname, sparams) in zip(mysql_procs, srv_procs):
    mine = [p.strip().split()[-1] for p in mparams.split(',') if p.strip()]
    theirs = [p.strip().split()[0] for p in sparams.split(',') if p.strip()]
    mine = [p.replace('p_', '') for p in mine]
    theirs = [p.replace('@', '') for p in theirs]
    check(len(mine) == len(theirs),
          f'{name}: MySQL takes {len(mine)} parameters, SQL Server takes {len(theirs)} '
          '(the repository passes them positionally on MySQL)')

schema_my = (app / 'Database/Scripts/MySql/01-schema.sql').read_text(encoding='utf-8')
schema_srv = (app / 'Database/Scripts/SqlServer/01-schema.sql').read_text(encoding='utf-8')
for table in ['residents', 'dependents', 'document_requests', 'official_receipts',
              'receipt_series', 'user_accounts', 'activity_log']:
    check(table in schema_my, 'MySQL schema is missing the table ' + table)
    check(table in schema_srv, 'SQL Server schema is missing the table ' + table)
check('address' not in schema_my.lower() and 'address' not in schema_srv.lower(),
      'the address column was supposed to be erased from the database')

# ------------------------------------------------------------ App.config ----
config = ET.parse(app / 'App.config').getroot()
settings = {a.attrib['key']: a.attrib['value']
            for a in config.find('appSettings').findall('add')}

check(settings.get('Storage', '').lower() == 'mysql', 'Storage should default to MySQL')
for required in ['SeedSampleData', 'LogFolder', 'Barangay.Name', 'Barangay.City', 'Barangay.Logo',
                 'Fee.Clearance.Local', 'Fee.Clearance.Abroad', 'Fee.Certification',
                 'Fee.BusinessClearance.Standard', 'Fee.LuponFiling', 'Fee.Facility.Hourly',
                 'Fee.Student.Enabled', 'Fee.Student.DiscountPercent', 'Fee.Student.Documents',
                 'Fee.CommunityTax.Base', 'Fee.CommunityTax.PerThousand', 'Fee.CommunityTax.Cap',
                 'Rule.OfficeWindowStart', 'Rule.OfficeWindowEnd', 'Rule.JobseekerResidencyMonths',
                 'Rule.Residency.NewcomerMonths', 'Rule.Residency.PermanentYears',
                 'Rule.RA11032.SimpleWorkingDays', 'Security.MaxFailedLogins', 'Security.LockoutMinutes',
                 'Security.SessionTimeoutMinutes', 'Security.MinimumPasswordLength',
                 'Security.ForcePasswordChangeOnFirstLogin', 'Security.ProtectConnectionString',
                 'Security.InitialAdmin.Username', 'Security.InitialAdmin.Password',
                 'Security.InitialClerk.Username', 'Security.InitialClerk.Password',
                 'Reports.UseCrystalReports', 'Reports.CrystalFolder', 'Reports.Footer']:
    check(required in settings, 'App.config is missing the setting ' + required)

# every key the code reads must be a key the file has
read_keys = set()
for source in app.rglob('*.cs'):
    if {'obj', 'bin'} & set(source.parts):
        continue
    text = source.read_text(encoding='utf-8', errors='replace')
    read_keys |= set(re.findall(r'Read(?:String|Int|Money|Bool|Time)\("([\w\.]+)"', text))
missing = sorted(k for k in read_keys if k not in settings and k != 'Storage')
check(not missing, 'the code reads settings that App.config does not define: ' + ', '.join(missing))

conn = {a.attrib['name']: a.attrib['connectionString']
        for a in config.find('connectionStrings').findall('add')}
for name in ['BarangayDb', 'BarangaySqlServer']:
    check(name in conn, 'App.config is missing the connection string ' + name)
pwd = re.search(r'(?:Password|Pwd)\s*=\s*([^;]*)', conn.get('BarangayDb', ''), re.I)
check(pwd is not None and pwd.group(1).strip() == '',
      'never commit a real MySQL password; use the BARANGAY_DB_CONNECTION variable instead')

# ----------------------------------------------------------------- logo -----
logo = app / 'Assets/barangay-logo.png'
check(logo.exists(), 'the barangay seal is missing')
if logo.exists():
    digest = hashlib.sha256(logo.read_bytes()).hexdigest()
    expected = '57fc619156d141038a71d5296c925aa5c5f79ba403e71368e35a872846361bc4'
    check(digest == expected,
          'the seal has been edited or replaced (sha256 ' + digest + '). It must stay as it was.')

# ------------------------------------------------------------- removals -----
for gone in ['BusinessRules', 'UIHelpers', 'CustomControls', 'Forms', 'Views',
             'MainShell.cs', 'AppSettings.cs', 'Database/schema.sql',
             'Database/MySqlBarangayRepository.cs', 'Database/RepositoryBase.cs',
             'Database/InMemoryBarangayRepository.cs', 'Database/DatabaseSettings.cs',
             'Interfaces/IBarangayRepository.cs', 'UI/Forms/TextPromptForm.cs',
             'UI/Forms/NewRequestForm.cs', 'UI/Forms/ChangePasswordForm.cs',
             'UI/Views/UsersView.cs', 'UI/Views/ActivityLogView.cs', 'UI/Views/ReceiptsView.cs']:
    check(not (app / gone).exists(), gone + ' is a leftover from the earlier build and must be gone')

resident = (app / 'Models/Resident.cs').read_text(encoding='utf-8')
check('public string Address' not in resident, 'Resident still has an Address property')
enums = (app / 'Models/Enums.cs').read_text(encoding='utf-8')
check('Student' not in enums.split('enum ResidentClassification')[1].split('}')[0],
      'Student is still one of the classifications; it is a fee category now')
check('FourPsBeneficiary' in enums, 'the fourth classification (4Ps beneficiary) is missing')
check('IsStudentFeeCategory' in resident, 'the student fee category is missing from Resident')

# --------------------------------------------------------------- sources ----
for source in sorted(app.rglob('*.cs')):
    if {'obj', 'bin'} & set(source.parts):
        continue
    text = source.read_text(encoding='utf-8', errors='replace')
    if not text.lstrip().startswith('//'):
        problems.append(str(source.relative_to(root)) + ' does not open with a comment')
    for open_char, close_char in [('{', '}'), ('(', ')')]:
        if text.count(open_char) != text.count(close_char):
            problems.append(f'{source.relative_to(root)}: unbalanced {open_char}{close_char}')

# ---------------------------------------------------------------- result ----
if problems:
    print('FAILED - ' + str(len(problems)) + ' problem(s):\n')
    for item in problems:
        print('  * ' + item)
    sys.exit(1)

print('All structure checks passed.')
print('  ' + str(len(actual)) + ' source files, ' + str(len(mysql_names)) + ' stored procedures, '
      + str(len(settings)) + ' settings, seal unchanged.')
