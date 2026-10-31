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
assert not findings, '\n'.join(findings)
print(f'Static source scan: {len(files)} C# files; no selected network, logging, placeholder or foreground-forcing APIs found.')
print('Project XML parsed; native host PE is x64/Windows GUI, targets SlipShot.dll, contains PerMonitorV2 manifest.')
print('These bounded static checks do not prove absence of all network activity, native correctness or application launch.')
