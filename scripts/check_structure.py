"""Source/project checks only; not a substitute for compiling on Windows."""
from pathlib import Path
import xml.etree.ElementTree as ET
import re

root = Path(__file__).resolve().parents[1]
app = root / 'BarangayDocumentSystem'
ns = {'m': 'http://schemas.microsoft.com/developer/msbuild/2003'}
project = ET.parse(app / 'BarangayDocumentSystem.csproj')
assert project.find('.//m:TargetFrameworkVersion', ns).text == 'v4.8'
listed = [x.attrib['Include'].replace('\\', '/') for x in project.findall('.//m:Compile', ns)]
actual = [str(p.relative_to(app)) for p in app.rglob('*.cs') if not {'obj', 'bin'} & set(p.parts)]
assert len(listed) == len(set(listed)), 'Duplicate Compile entries'
assert set(listed) == set(actual), 'Missing or stale Compile entries'
for folder in ['BusinessRules/DocumentTemplates', 'CustomControls', 'Database', 'Forms', 'Interfaces', 'Models', 'UIHelpers', 'Views']:
    assert (app / folder).is_dir(), folder
for name in ['App.config', 'app.manifest', 'MainShell.resx']:
    ET.parse(app / name)
assert not (root / 'docs/dashboard-preview.png').exists()
assert (app / 'Assets/barangay-logo.png').exists()
for file in actual:
    text = (app / file).read_text()
    for old in ['BarangayDocumentSystem.Helper;', 'BarangayDocumentSystem.Service;', 'BarangayDocumentSystem.DBContext;', 'ApplicationConfiguration.Initialize()', 'Math.Clamp(', 'ArgumentNullException.ThrowIfNull(']:
        assert old not in text, (file, old)
    assert not re.search(r'Enum\.(?:GetValues|GetNames|Parse)<', text), file
print(f'PASS: {len(actual)} source files, Framework target, layout, XML, assets, and compatibility scan.')
