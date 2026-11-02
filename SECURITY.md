# Security Policy & Security Requirements Specification (SRS)
## Thai OpenUtau (DELTA SYNTH Edition)

> **Date**: 28/10/2026 (B.E. 2569 / A.D. 2026)  
> **Version**: 38.3.11  
> **Author Credit**: Made And Checked By DELTA SYNTH & Gemini AI [ Original by OpenUtau Authors ]  
> **Classification**: Security Policy & Threat-Derived Security Requirements (`/security-requirement-extraction`)

---

## 1. Supported Versions

| Version | Branch / Distribution | Supported | Security Verification |
| ------- | --------------------- | --------- | --------------------- |
| `38.3.x` | `Publish` / `D:\Program Files\Thai_OpenUtau` | :white_check_mark: Active | Ed25519 Strict Update Verification, Zip-Slip Protection, Safe URI Validation |
| `< 38.3.11` | Legacy Builds | :x: Deprecated | Upgrade to `>= 38.3.11` immediately |

---

## 2. Threat-Derived Security Requirements Matrix

### SR-001: Cryptographic Update Signature Verification
- **Source Threat**: T-UPD-001 (Tampering / Man-in-the-Middle during auto-update via `NetSparkleUpdater`).
- **Priority**: Critical
- **Requirement**: The application update client (`UpdaterViewModel`) SHALL enforce `SecurityMode.Strict` on `Ed25519Checker` when initializing `ZipUpdater` and SHALL NOT use `SecurityMode.Unsafe`.
- **Acceptance Criteria**:
  1. Given an appcast or update archive without a valid Ed25519 signature, when `UpdaterViewModel.NewUpdaterAsync()` initializes `ZipUpdater`, then `SecurityMode.Strict` is enforced.
  2. Given an offline or unreachable update endpoint, when update check runs on startup, then the failure is handled gracefully at `Information` log level without crashing or exposing stack traces to the user.

### SR-002: Archive Extraction Path Traversal Prevention (Zip-Slip Defense)
- **Source Threat**: T-ARC-001 (Arbitrary File Write via crafted `.zip`, `.rar`, `.uar`, `.vogeon`, or `.oudep` voicebank/plugin packages containing `../` entries).
- **Priority**: Critical
- **Requirement**: All archive extraction routines (`VoicebankInstaller`, `DependencyInstaller`, `PackageManager`, `VogenSingerInstaller`, `SingerHubClient`) SHALL canonicalize destination paths via `Path.GetFullPath` and verify that the resolved path starts with the intended target directory (including trailing `Path.DirectorySeparatorChar`) before creating directories or writing files (including synthesized metadata files such as `character.txt` / `character.yaml`).
- **Acceptance Criteria**:
  1. Given an archive entry or root folder containing relative traversal sequences (`..\` or `../`) or sibling-prefix paths, when extracted by `VoicebankInstaller`, `DependencyInstaller`, `PackageManager`, `VogenSingerInstaller`, or `SingerHubClient`, then an exception is thrown or the entry is rejected before any filesystem write occurs.

### SR-003: Safe External URI & Network Scheme Validation
- **Source Threat**: T-NET-001 (Arbitrary Command Execution via `OS.OpenWeb`, `SingersDialog`, or untrusted `DownloadUrl` / `PageUrl` in `SingerHubDialog` and `SingerHubRowViewModel`).
- **Priority**: High
- **Requirement**: External URLs opened via `OS.OpenWeb` or fetched via `SingerHubClient` SHALL be validated as absolute URIs with `https` (or `http`/`https` for browser-only links) schemes.
- **Acceptance Criteria**:
  1. Given a `file://`, `javascript:`, or custom protocol URI in `SingerHubRowViewModel.DownloadUrl`, when evaluated by `HasDownloadUrl`, then it returns `false`.
  2. Given a non-HTTP/HTTPS URI or local command passed to `OS.OpenWeb`, when invoked directly or via `SingersDialog` / `SingerHubDialog`, then `OS.OpenWeb` throws `ArgumentException` without starting a process.

### SR-004: Plugin & Native Library Loading Integrity
- **Source Threat**: T-PLG-001 (Untrusted / Unmanaged DLL injection or duplicate core assembly conflict during `DocManager.SearchAllPlugins()`).
- **Priority**: High
- **Requirement**: `DocManager.SearchAllPlugins()` SHALL filter out known native binaries (`worldline.dll`, `DirectML.dll`, `SDL3.dll`, `onnxruntime*.dll`) and core framework assemblies (`OpenUtau.Core.dll`, `OpenUtau.Plugin.Builtin.dll` duplicates in user plugin folders), verify PE CLI metadata via `LibraryLoader.IsManagedAssembly` and `PEReader` prior to loading into `AssemblyLoadContext.Default`, and resolve `worldline.dll` deterministically from the application base directory or `runtimes/<rid>/native`.
- **Acceptance Criteria**:
  1. Given native DLLs or stale `OpenUtau.Core.dll` copies in `Plugins/`, when `SearchAllPlugins()` executes, then they are skipped without throwing `BadImageFormatException` or loading duplicate type identities.
  2. Given `worldline.dll` in either `AppContext.BaseDirectory` or `runtimes/win-x64/native`, when `Worldline` P/Invoke methods execute, then `NativeLibrary.SetDllImportResolver` resolves the binary with zero `DllNotFoundException`.

### SR-005: Single-Instance Isolation & Log Resource Protection
- **Source Threat**: T-IPC-001 (Concurrent process race corrupting `Preferences.json`, audio device locks, or log files).
- **Priority**: Medium
- **Requirement**: `Program.Main` SHALL acquire a deterministic SHA-256 named mutex (`ThaiOpenUtau_SingleInstance_<hash>`) scoped to the user and executable path before initializing Serilog file sinks.
- **Acceptance Criteria**:
  1. Given an already-running instance of Thai OpenUtau, when a second instance is launched, then it exits cleanly with code `0` before opening the log file.

---

## 3. Reporting a Vulnerability

If you discover a security vulnerability in **Thai OpenUtau (DELTA SYNTH Edition)**, please report it directly to the **DELTA SYNTH Security & Architecture Team**:

1. Do **not** open a public issue with exploit payloads.
2. Document the affected component (`OpenUtau`, `OpenUtau.Core`, `OpenUtau.Plugin.Builtin`, or native resamplers/wavtools), reproduction steps, and log excerpt.
3. All verified vulnerabilities are patched at the root cause following the **Preserve → Strengthen → Optimize → Verify** standard.
