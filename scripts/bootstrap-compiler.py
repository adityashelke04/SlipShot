"""Small SDK-less validation path. Official NuGet compiler/test packages only, on F:.
The standard supported build remains the .NET SDK project. No application launch.
"""
import hashlib, pathlib, urllib.request, zipfile
ROOT = pathlib.Path(__file__).resolve().parent.parent
CACHE = ROOT / '.cache' / 'tooling'
CACHE.mkdir(parents=True, exist_ok=True)
packages = [('microsoft.net.compilers.toolset', '4.11.0'), ('nunit','4.4.0'), ('nunitlite','4.4.0'),
            ('microsoft.net.hostmodel','3.1.16'), ('microsoft.netcore.app.host.win-x64','8.0.11')]
hashes = {
    'microsoft.net.compilers.toolset': 'db165c75f0d0386d078d01448f96c93f8222320633e4fe828f5d1bf8c99b7d6f',
    'nunit': 'e60785e50385f97fd692e0848243d5287e0074ac78534a25a54d3b4bcf86de75',
    'nunitlite': 'ab19dbafb884125419211da5664f6d37ac5734f1dfdd7736e8a4accb2e37a5b0',
    'microsoft.net.hostmodel': 'e36705b5a645cccf772348198ee0db7046163393e577e931d8c916bb427aeb0c',
    'microsoft.netcore.app.host.win-x64': '21335fb39cb2870e816cf0fbe8206f8baeda52a703566c1c92e876c1d9b9be45',
}
for name, version in packages:
    directory = CACHE / f'{name}.{version}'
    archive = CACHE / f'{name}.{version}.nupkg'
    url = f'https://api.nuget.org/v3-flatcontainer/{name}/{version}/{name}.{version}.nupkg'
    data = archive.read_bytes() if archive.exists() else urllib.request.urlopen(url, timeout=60).read()
    digest = hashlib.sha256(data).hexdigest()
    if digest != hashes[name]:
        raise ValueError(f'Package checksum mismatch: {name} {version}')
    if not archive.exists():
        archive.write_bytes(data)
    marker = directory / '.verified'
    if marker.exists() and marker.read_text() == digest:
        print(f'Verified cached package: {directory.name}')
        continue
    with zipfile.ZipFile(archive) as z:
        for entry in z.infolist():
            resolved = (directory / entry.filename).resolve()
            if not resolved.is_relative_to(directory.resolve()):
                raise ValueError('Unsafe archive path')
        z.extractall(directory)
    marker.write_text(digest)
    print(f'{name} {version}: {len(data):,} bytes SHA256 {hashlib.sha256(data).hexdigest()}')
