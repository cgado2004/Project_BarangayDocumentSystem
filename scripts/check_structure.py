"""Source/project checks only; not a substitute for compiling on Windows.

Run from anywhere:  python scripts/check_structure.py

What it proves: the project file lists exactly the .cs files that exist, the
Framework target and folder layout are intact, the XML files parse, the schema
is embedded, no real password is committed, every source file carries its
attribution header, the MySQL repository reads only columns the schema
defines, and no API that is missing from .NET Framework 4.8 crept in.
"""
from pathlib import Path
import xml.etree.ElementTree as ET
import re

root = Path(__file__).resolve().parents[1]
app = root / 'BarangayDocumentSystem'
ns = {'m': 'http://schemas.microsoft.com/developer/msbuild/2003'}
project = ET.parse(app / 'BarangayDocumentSystem.csproj')
assert project.find('.//m:TargetFrameworkVersion', ns).text == 'v4.8'

# ---- every .cs file is compiled, and nothing stale is listed ------------
listed = [x.attrib['Include'].replace('\\', '/') for x in project.findall('.//m:Compile', ns)]
actual = [str(p.relative_to(app)) for p in app.rglob('*.cs') if not {'obj', 'bin'} & set(p.parts)]
assert len(listed) == len(set(listed)), 'Duplicate Compile entries'
assert set(listed) == set(actual), 'Missing or stale Compile entries: ' + str(set(listed) ^ set(actual))

for folder in ['BusinessRules/DocumentTemplates', 'CustomControls', 'Database', 'Forms', 'Interfaces', 'Models', 'UIHelpers', 'Views']:
    assert (app / folder).is_dir(), folder
for name in ['App.config', 'app.manifest', 'MainShell.resx']:
    ET.parse(app / name)
assert not (root / 'docs/dashboard-preview.png').exists()
assert (app / 'Assets/barangay-logo.png').exists()

# ---- persistence wiring --------------------------------------------------
embedded = [x.attrib['Include'].replace('\\', '/') for x in project.findall('.//m:EmbeddedResource', ns)]
assert 'Database/schema.sql' in embedded, 'schema.sql must be an EmbeddedResource (DatabaseInitializer reads it from the .exe)'
assert (app / 'Database/schema.sql').exists()
packages = {x.attrib['Include']: x.attrib.get('Version') for x in project.findall('.//m:PackageReference', ns)}
assert 'MySql.Data' in packages, 'MySql.Data PackageReference missing'
for retired in ['Database/01-schema.sql', 'Database/02-seed-data.sql']:
    assert not (app / retired).exists(), f'{retired} was retired; schema.sql is the one schema'

# ---- App.config: MySQL default, no committed password ---------------------
config = ET.parse(app / 'App.config').getroot()
settings = {a.attrib['key']: a.attrib['value'] for a in config.find('appSettings').findall('add')}
assert settings.get('Storage', '').lower() == 'mysql', 'Storage should default to MySQL'
assert 'SeedSampleData' in settings
conn = {a.attrib['name']: a.attrib['connectionString'] for a in config.find('connectionStrings').findall('add')}
assert 'BarangayDb' in conn
pwd = re.search(r'(?:Password|Pwd)\s*=\s*([^;]*)', conn['BarangayDb'], re.I)
assert pwd is not None and pwd.group(1).strip() == '', 'Never commit a real MySQL password; use BARANGAY_DB_CONNECTION'

# ---- the schema defines every column the MySQL repository reads/writes -----
schema = (app / 'Database/schema.sql').read_text(encoding='utf-8')
def table_columns(table):
    body = re.search(rf'CREATE TABLE IF NOT EXISTS {table}\s*\((.*?)\)\s*ENGINE', schema, re.S).group(1)
    cols = set()
    for line in body.splitlines():
        line = line.strip()
        if not line or line.startswith('--') or line.upper().startswith(('PRIMARY', 'INDEX', 'CONSTRAINT', 'REFERENCES')):
            continue
        cols.add(line.split()[0])
    return cols
schema_requests = table_columns('document_requests')
schema_residents = table_columns('residents')
repo = (app / 'Database/MySqlBarangayRepository.cs').read_text(encoding='utf-8')
def sql_const(name):
    m = re.search(rf'private const string {name}\s*=\s*(.*?);', repo, re.S)
    return ''.join(re.findall(r'"([^"]*)"', m.group(1)))
select_requests = sql_const('SqlSelectRequests')
used = set(re.findall(r'\b([a-z_]+)\b', select_requests.split('FROM')[0].replace('SELECT', '')))
missing = used - schema_requests
assert not missing, f'MySqlBarangayRepository selects columns schema.sql does not define: {missing}'
insert_requests = sql_const('SqlInsertRequest')
inserted = set(re.search(r'\((.*?)\)\s*VALUES', insert_requests, re.S).group(1).replace(' ', '').split(','))
assert inserted <= schema_requests, f'INSERT uses unknown columns: {inserted - schema_requests}'
initializer = (app / 'Database/DatabaseInitializer.cs').read_text(encoding='utf-8')
upgrades = set(re.findall(r'\(\s*"([a-z_]+)"\s*,\s*"', initializer))
assert upgrades <= schema_requests, f'EnsureColumns upgrades a column schema.sql lacks: {upgrades - schema_requests}'
resident_cols = set(sql_const('ResidentColumns').replace(' ', '').split(','))
assert resident_cols <= schema_residents, f'Resident columns not in schema: {resident_cols - schema_residents}'

# ---- attribution headers and Framework-safe APIs -------------------------
sources = [app / f for f in actual] + [root / 'tests/RuleChecks/Program.cs']
for path in sources:
    text = path.read_text(encoding='utf-8')
    head = '\n'.join(text.splitlines()[:12])
    for tag in ['PART:', 'ORIGIN:', 'EDITS:', 'VOICE:']:
        assert tag in head, (str(path.relative_to(root)), f'missing {tag} in the attribution header')
    for old in ['BarangayDocumentSystem.Helper;', 'BarangayDocumentSystem.Service;', 'BarangayDocumentSystem.DBContext;', 'ApplicationConfiguration.Initialize()', 'Math.Clamp(', 'ArgumentNullException.ThrowIfNull(', 'System.Configuration;']:
        assert old not in text, (str(path.relative_to(root)), old)
    assert not re.search(r'Enum\.(?:GetValues|GetNames|Parse)<', text), path
assert 'PART:' in schema.splitlines()[1]

print(f'PASS: {len(actual)} source files, Framework target, layout, XML, assets, embedded schema, '
      f'no committed password, schema/repository column agreement, attribution headers, compatibility scan.')
