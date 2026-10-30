"""Small SDK-less validation path. Official NuGet compiler/test packages only, on F:.
The standard supported build remains the .NET SDK project. No application launch.
"""
import base64, hashlib, json, pathlib, urllib.request, zipfile
ROOT = pathlib.Path(__file__).resolve().parent.parent
CACHE = ROOT / '.cache' / 'tooling'
CACHE.mkdir(parents=True, exist_ok=True)
packages = [('microsoft.net.compilers.toolset', '4.11.0'), ('nunit','4.4.0'), ('nunitlite','4.4.0')]
for name, version in packages:
    directory = CACHE / f'{name}.{version}'
    archive = CACHE / f'{name}.{version}.nupkg'
    if directory.exists():
        print(f'Cached: {directory.name}')
        continue
    url = f'https://api.nuget.org/v3-flatcontainer/{name}/{version}/{name}.{version}.nupkg'
    data = urllib.request.urlopen(url, timeout=60).read()
    archive.write_bytes(data)
    with zipfile.ZipFile(archive) as z:
        for entry in z.infolist():
            resolved = (directory / entry.filename).resolve()
            if not resolved.is_relative_to(directory.resolve()):
                raise ValueError('Unsafe archive path')
        z.extractall(directory)
    print(f'{name} {version}: {len(data):,} bytes SHA256 {hashlib.sha256(data).hexdigest()}')
