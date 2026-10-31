"""Bounded tracked-file/history privacy check; no desktop, clipboard or network access.

This is a guardrail for obvious accidental uploads, not a complete secret detector.
It reports paths/rule names only, never matching secret values.
"""
import pathlib
import re
import struct
import subprocess

ROOT = pathlib.Path(__file__).resolve().parent.parent
ALLOWED_IMAGES = {f'docs/images/{name}.png' for name in ('editor-dark', 'editor-light', 'preview-dark')}
ALLOWED_IMAGES.update(f'docs/branding/concepts/{name}.png' for name in ('frame-s','lifted-slip','open-shutter'))
ALLOWED_ICONS = {'src/SlipShot/Assets/slipshot.ico'}
RULES = {
    'private key': rb'-----BEGIN (?:RSA |EC |OPENSSH |DSA |ENCRYPTED )?PRIVATE KEY-----',
    'GitHub token': rb'\b(?:gh[pousr]_[A-Za-z0-9]{36,}|github_pat_[A-Za-z0-9_]{40,})\b',
    'AWS access key': rb'\b(?:AKIA|ASIA)[A-Z0-9]{16}\b',
    'Slack token': rb'\bxox[baprs]-[A-Za-z0-9-]{20,}\b',
}

def git(*args, data=None):
    return subprocess.run(['git', *args], cwd=ROOT, input=data, capture_output=True, check=True).stdout

def forbidden(path):
    parts = pathlib.PurePosixPath(path).parts
    name = parts[-1].lower() if parts else ''
    suffix = pathlib.PurePosixPath(path).suffix.lower()
    return (any(p.lower() in {'.cache', '.tmp', '.firecrawl', 'artifacts', 'bin', 'obj', 'data', 'testresults'} for p in parts)
            or (name.startswith('.env') and name != '.env.example')
            or name == 'settings.json'
            or suffix in {'.exe', '.dll', '.pdb', '.nupkg', '.pfx', '.p12', '.key', '.zip'}
            or (suffix in {'.png', '.jpg', '.jpeg', '.webp', '.bmp', '.gif', '.tif', '.tiff', '.ico'} and path not in ALLOWED_IMAGES | ALLOWED_ICONS))

findings = []
tracked = git('ls-files', '-z').decode().split('\0')
for path in filter(None, tracked):
    if forbidden(path):
        findings.append(f'Tracked private/generated path: {path}')

objects = {}
for line in git('rev-list', '--objects', '--all').decode().splitlines():
    sha, _, path = line.partition(' ')
    if path:
        objects[sha] = path
        if forbidden(path):
            findings.append(f'History private/generated path: {path}')

metadata = git('cat-file', '--batch-check=%(objectname) %(objecttype) %(objectsize)',
               data=('\n'.join(objects)+'\n').encode()).decode().splitlines()
blobs = []
for line in metadata:
    sha, kind, size = line.split()
    if kind == 'blob':
        if int(size) > 2_000_000:
            findings.append(f'Large history blob needs review: {objects[sha]}')
        else:
            blobs.append(sha)
payload = git('cat-file', '--batch', data=('\n'.join(blobs)+'\n').encode())
offset = 0
for sha in blobs:
    end = payload.index(b'\n', offset)
    size = int(payload[offset:end].split()[-1])
    content = payload[end+1:end+1+size]
    offset = end+size+2
    for label, pattern in RULES.items():
        if re.search(pattern, content):
            findings.append(f'{label} signature in history: {objects[sha]}')

# Also cover uncommitted tracked text, which has not yet entered history.
for path in filter(None, tracked):
    file = ROOT / path
    if file.is_file() and file.stat().st_size <= 2_000_000:
        for label, pattern in RULES.items():
            if re.search(pattern, file.read_bytes()):
                findings.append(f'{label} signature in working file: {path}')

for path in sorted(ALLOWED_IMAGES):
    data = (ROOT/path).read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', path
    pos = 8
    while pos < len(data):
        length = struct.unpack_from('>I', data, pos)[0]
        chunk = data[pos+4:pos+8]
        if chunk in {b'tEXt', b'zTXt', b'iTXt', b'eXIf'}:
            findings.append(f'Image has text/EXIF metadata needing review: {path}')
        pos += length+12
    assert pos == len(data), f'Unexpected trailing PNG data: {path}'

if findings:
    raise SystemExit('\n'.join(sorted(set(findings))))
print(f'PASS: {len(list(filter(None, tracked)))} tracked paths and {len(blobs)} historical blobs checked.')
print('Only explicitly reviewed synthetic UI, generated logo boards and the app ICO are allowed; no prohibited history paths or selected credential signatures found.')
print('PNG text/EXIF metadata absent. Pixel provenance still requires reviewing the generator and the actual images.')
