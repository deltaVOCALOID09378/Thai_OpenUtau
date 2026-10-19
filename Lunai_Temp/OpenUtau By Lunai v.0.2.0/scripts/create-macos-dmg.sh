#!/usr/bin/env bash
set -euo pipefail

ARCH_NAME="${1:?usage: create-macos-dmg.sh <arch-name> e.g. osx-x64}"

APP="bin/${ARCH_NAME}/publish/OpenUtau.app"
PLIST="${APP}/Contents/Info.plist"
OUT="OpenUtau-${ARCH_NAME}.dmg"

if [[ ! -d "${APP}" ]]; then
  echo "App bundle not found: ${APP}" >&2
  exit 1
fi

cp OpenUtau/Assets/OpenUtau.icns "${APP}/Contents/Resources/"

# create-dmg reads Info.plist as XML; Dotnet.Bundle may emit binary or BOM-prefixed XML.
plutil -convert xml1 "${PLIST}"
python3 -c "
from pathlib import Path
p = Path('${PLIST}')
data = p.read_bytes()
if data.startswith(b'\\xef\\xbb\\xbf'):
    p.write_bytes(data[3:])
"

rm -f "${OUT}" *.dmg
hdiutil detach "/Volumes/OpenUtau" -force 2>/dev/null || true

npm install -g create-dmg
create-dmg --no-code-sign "${APP}"
mv *.dmg "${OUT}"
# Ad-hoc sign when no Developer ID is available (e.g. GitHub Actions).
codesign -fvs - "${OUT}" || true
