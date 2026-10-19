#!/usr/bin/env bash
TARGET_DIR="${1:-$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )}"

echo "==========================================="
echo " DELTA SYNTH - Junk Cleaner (Linux / macOS)"
echo "==========================================="
echo "Target: $TARGET_DIR"

echo "Progress: 20% - Removing build artifacts (bin and obj)..."
find "$TARGET_DIR" -type d \( -name "bin" -o -name "obj" -o -name ".vs" -o -name "UCache" -o -name ".tmp.driveupload" \) -prune -exec rm -rf {} + 2>/dev/null || true

echo "Progress: 60% - Removing log and temp files..."
find "$TARGET_DIR" -type f \( -name "*.log" -o -name "*_log.txt" -o -name "*.tmp" -o -name "build_out.txt" -o -name "compile_log.txt" \) -delete 2>/dev/null || true

echo "Progress: 90% - Removing test results..."
find "$TARGET_DIR" -type d \( -name "TestResults" -o -name "BenchmarkDotNet.Artifacts" \) -prune -exec rm -rf {} + 2>/dev/null || true

echo "Progress: 100% - Cleanup completed successfully!"
