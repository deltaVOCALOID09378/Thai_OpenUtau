#!/usr/bin/env bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
STATE_DIR="${HOME}/.local/share/Thai_OpenUtau"
PREREQ_MARKER="${STATE_DIR}/.installed_prereqs"

echo "======================================================="
echo " DELTA SYNTH - Thai_OpenUtau System Setup & Builder"
echo " Architecture: Multi-Runtime Support (.NET 8.0 - 12.0+)"
echo "======================================================="

echo "[1/4] Checking .NET SDK / Runtime environment..."
if command -v dotnet >/dev/null 2>&1; then
    DOTNET_VER=$(dotnet --version 2>/dev/null || echo "0")
    DOTNET_MAJOR=$(echo "$DOTNET_VER" | cut -d. -f1)
    if [ "$DOTNET_MAJOR" -ge 8 ]; then
        echo "[OK] Detected .NET SDK version $DOTNET_VER (Supported: 8.0 - 12.0+)"
        mkdir -p "$STATE_DIR" 2>/dev/null || true
        echo "installed_version=$DOTNET_VER" > "$PREREQ_MARKER" 2>/dev/null || true
    else
        echo "[WARNING] Detected .NET version $DOTNET_VER is below 8.0"
    fi
else
    echo "[NOTICE] dotnet command not found. Please install .NET SDK 8.0 or higher."
fi

echo "[2/4] Restoring dependencies..."
export DOTNET_ROLL_FORWARD=LatestMajor
dotnet restore "$DIR/OpenUtau/OpenUtau.csproj"

if [ -f "$DIR/cpp/build_linux.sh" ]; then
    echo "[3/4] Building C++ Worldline Library..."
    chmod +x "$DIR/cpp/build_linux.sh"
    "$DIR/cpp/build_linux.sh"
else
    echo "[3/4] Using built-in native runtime."
fi

echo "[4/4] Building C# project (Release Mode)..."
dotnet build "$DIR/OpenUtau/OpenUtau.csproj" -c Release --no-restore

echo "[SUCCESS] Build completed successfully!"
