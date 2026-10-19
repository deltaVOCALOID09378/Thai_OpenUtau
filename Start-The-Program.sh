#!/usr/bin/env bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

# Dynamically search across all net* builds
EXE=$(find "$DIR/OpenUtau/bin/Release" "$DIR/OpenUtau/bin/Debug" "$DIR/All Rendered File" -maxdepth 2 -type f -name "OpenUtau" 2>/dev/null | head -n 1)
DLL=$(find "$DIR/OpenUtau/bin/Release" "$DIR/OpenUtau/bin/Debug" "$DIR/All Rendered File" -maxdepth 2 -type f -name "OpenUtau.dll" 2>/dev/null | head -n 1)

if [ -n "$EXE" ] && [ -x "$EXE" ]; then
    echo "Starting Thai OpenUtau: $EXE"
    cd "$(dirname "$EXE")"
    ./OpenUtau "$@"
elif [ -n "$DLL" ]; then
    echo "Starting Thai OpenUtau via dotnet: $DLL"
    cd "$(dirname "$DLL")"
    dotnet OpenUtau.dll "$@"
else
    echo "[ERROR] OpenUtau executable not found."
    echo "Please run ./Install-File.sh first to build the program."
fi
