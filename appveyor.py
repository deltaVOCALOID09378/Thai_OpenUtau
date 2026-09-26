# Version 0.1
import os
import sys
import shutil
import zipfile
import subprocess
import urllib.request
from pathlib import Path
from datetime import datetime, timezone

appcast_ver = os.environ.get('APPVEYOR_BUILD_VERSION', '0.0.0')

def run_cmd(cmd: str) -> None:
    """Helper function to execute system commands safely."""
    print(f"[Command] {cmd}")
    subprocess.run(cmd, shell=True, check=True)

def cleanup_files(*patterns) -> None:
    """Helper function to remove files matching specific patterns."""
    for pattern in patterns:
        for file in Path('.').glob(pattern):
            try:
                file.unlink()
            except Exception as e:
                print(f"[Warning] Could not delete {file}: {e}")

def write_appcast(appcast_os: str, appcast_rid: str, appcast_file: str) -> None:
    """Generates the Appcast XML file for OpenUtau updates."""
    pub_date = datetime.now(timezone.utc).astimezone().strftime("%a, %d %b %Y %H:%M:%S %z")
    
    xml = f"""<?xml version="1.0" encoding="utf-8"?>
<rss version="2.0" xmlns:sparkle="http://www.andymatuschak.org/xml-namespaces/sparkle">
<channel>
    <title>OpenUtau</title>
    <language>en</language>
    <item>
    <title>OpenUtau {appcast_ver}</title>
    <pubDate>{pub_date}</pubDate>
    <enclosure url="https://github.com/stakira/OpenUtau/releases/download/build%2F{appcast_ver}/{appcast_file}"
                sparkle:version="{appcast_ver}"
                sparkle:shortVersionString="{appcast_ver}"
                sparkle:os="{appcast_os}"
                type="application/octet-stream"
                sparkle:signature="" />
    </item>
</channel>
</rss>"""

    output_path = Path(f"appcast.{appcast_rid}.xml")
    output_path.write_text(xml, encoding='utf-8')
    print(f"[Success] Generated {output_path.name}")

def build_windows():
    if appcast_ver != '0.0.0':
        run_cmd(f"git tag build/{appcast_ver}")
        run_cmd(f"git push origin build/{appcast_ver}")

    cleanup_files('*.xml')

    # Download and extract DirectML
    nupkg_path = "Microsoft.AI.DirectML.nupkg"
    directml_dir = Path("Microsoft.AI.DirectML")
    print(f"[Download] Fetching DirectML package...")
    urllib.request.urlretrieve("https://www.nuget.org/api/v2/package/Microsoft.AI.DirectML/1.12.0", nupkg_path)
    
    directml_dir.mkdir(exist_ok=True)
    with zipfile.ZipFile(nupkg_path, 'r') as zip_ref:
        zip_ref.extractall(directml_dir)

    # Build for win-x86
    run_cmd("dotnet restore OpenUtau -r win-x86")
    run_cmd("dotnet publish OpenUtau -c Release -r win-x86 --self-contained true -o bin/win-x86")
    plugin_src = Path("OpenUtau.Plugin.Builtin/bin/Release/netstandard2.1/OpenUtau.Plugin.Builtin.dll")
    shutil.copy2(plugin_src, Path("bin/win-x86/OpenUtau.Plugin.Builtin.dll"))
    write_appcast("windows", "win-x86", "OpenUtau-win-x86.zip")

    # Build for win-x64
    run_cmd("dotnet restore OpenUtau -r win-x64")
    run_cmd("dotnet publish OpenUtau -c Release -r win-x64 --self-contained true -o bin/win-x64")
    shutil.copy2(plugin_src, Path("bin/win-x64/OpenUtau.Plugin.Builtin.dll"))
    write_appcast("windows", "win-x64", "OpenUtau-win-x64.zip")

    # Installer
    run_cmd(f"makensis -DPRODUCT_VERSION={appcast_ver} OpenUtau.nsi")
    write_appcast("windows", "win-x64-installer", "OpenUtau-win-x64.exe")

def build_macos():
    cleanup_files('*.dmg', '*.xml')

    run_cmd("git checkout OpenUtau/OpenUtau.csproj")
    if Path("LICENSE.txt").exists():
        Path("LICENSE.txt").unlink()

    # Update version in csproj using native Python (safer than sed)
    csproj_path = Path("OpenUtau/OpenUtau.csproj")
    csproj_content = csproj_path.read_text(encoding='utf-8')
    csproj_content = csproj_content.replace("0.0.0", appcast_ver)
    csproj_path.write_text(csproj_content, encoding='utf-8')

    run_cmd("dotnet restore OpenUtau -r osx-x64")
    run_cmd("dotnet msbuild OpenUtau -t:BundleApp -p:Configuration=Release -p:RuntimeIdentifier=osx-x64 -p:UseAppHost=true -p:OutputPath=../bin/osx-x64/")
    
    icon_dest = Path("bin/osx-x64/publish/OpenUtau.app/Contents/Resources/OpenUtau.icns")
    icon_dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2("OpenUtau/Assets/OpenUtau.icns", icon_dest)
    
    cleanup_files('*.dmg')
    run_cmd("npm install -g create-dmg")
    run_cmd("create-dmg bin/osx-x64/publish/OpenUtau.app")
    
    # Rename generated dmg
    for dmg_file in Path('.').glob('*.dmg'):
        dmg_file.rename("OpenUtau-osx-x64.dmg")
        break
        
    run_cmd("codesign -fvs - OpenUtau-osx-x64.dmg")
    run_cmd("git checkout OpenUtau/OpenUtau.csproj")
    run_cmd("git checkout LICENSE.txt")

    write_appcast("macos", "osx-x64", "OpenUtau-osx-x64.dmg")

def build_linux():
    cleanup_files('*.xml')

    run_cmd("dotnet restore OpenUtau -r linux-x64")
    run_cmd("dotnet publish OpenUtau -c Release -r linux-x64 --self-contained true -o bin/linux-x64")
    
    # chmod +x
    linux_bin = Path("bin/linux-x64/OpenUtau")
    if linux_bin.exists():
        linux_bin.chmod(linux_bin.stat().st_mode | 0o111)

    run_cmd("tar -C bin/linux-x64 -czvf OpenUtau-linux-x64.tar.gz .")
    write_appcast("linux", "linux-x64", "OpenUtau-linux-x64.tar.gz")

if __name__ == '__main__':
    if sys.platform == 'win32':
        build_windows()
    elif sys.platform == 'darwin':
        build_macos()
    else:
        build_linux()
