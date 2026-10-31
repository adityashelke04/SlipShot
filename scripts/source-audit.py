"""Static release checks only. Does not launch the app or access the desktop."""
import pathlib, re, struct, xml.etree.ElementTree as ET
root = pathlib.Path(__file__).resolve().parent.parent
files = sorted((root / 'src').rglob('*.cs'))
patterns = [r'\bHttpClient\b', r'\bWebRequest\b', r'\bTcpClient\b', r'\bSocket\b',
            r'\bSetForegroundWindow\s*\(', r'\bProcess\.Start\s*\(',
            r'\b(?:Console|Debug|Trace)\.Write', r'\bNotImplementedException\b', r'\bTODO\b']
findings = []
for path in files:
    if any(part in ('bin', 'obj') for part in path.parts):
        continue
    text = path.read_text(encoding='utf-8-sig')
    for pattern in patterns:
        # Explicit user-clicked folder navigation: fixed Windows Explorer executable,
        # one validated directory argument; reviewed/tested in CaptureFolderTests.
        if pattern == r'\bProcess\.Start\s*\(' and path == root / 'src/SlipShot/Services/CaptureFolder.cs':
            continue
        if re.search(pattern, text):
            findings.append(f'{path.relative_to(root)}: {pattern}')
for path in [root / 'Directory.Build.props', *root.glob('**/*.csproj'), root / 'src/SlipShot/app.manifest']:
    if '.cache' not in path.parts:
        ET.parse(path)
binary = (root / 'artifacts/portable/SlipShot.exe').read_bytes()
offset = struct.unpack_from('<I', binary, 0x3c)[0]
assert binary[offset:offset+4] == b'PE\0\0'
assert struct.unpack_from('<H', binary, offset+4)[0] == 0x8664, 'Expected x64 host'
assert struct.unpack_from('<H', binary, offset+24+68)[0] == 2, 'Expected Windows GUI subsystem'
assert b'SlipShot.dll\0' in binary, 'Apphost target missing'
assert b'PerMonitorV2' in binary or 'PerMonitorV2'.encode('utf-16-le') in binary, 'DPI manifest missing'
optional = offset + 24
section_count = struct.unpack_from('<H', binary, offset+6)[0]
optional_size = struct.unpack_from('<H', binary, offset+20)[0]
resource_rva = struct.unpack_from('<I', binary, optional+112+16)[0]
resource_offset = None
for index in range(section_count):
    section = optional+optional_size+40*index
    virtual_size, rva, raw_size, raw_offset = struct.unpack_from('<IIII', binary, section+8)
    if rva <= resource_rva < rva+max(virtual_size, raw_size):
        resource_offset = raw_offset+resource_rva-rva
assert resource_offset is not None, 'Native resources missing'
named, ids = struct.unpack_from('<HH', binary, resource_offset+12)
resource_types = {struct.unpack_from('<I', binary, resource_offset+16+8*i)[0] for i in range(named+ids)}
assert {3,14,24}.issubset(resource_types), 'Native icon, icon-group or manifest resource missing'
assert not findings, '\n'.join(findings)
print(f'Static source scan: {len(files)} C# files; no selected network, logging, placeholder or foreground-forcing APIs found.')
print('Project XML parsed; x64 GUI apphost targets SlipShot.dll with native icon/icon-group resources and PerMonitorV2 manifest.')
print('These bounded static checks do not prove absence of all network activity, native correctness or application launch.')
