# Project Overview & Architecture Scan — Thai OpenUtau (v3.9.5)

## 1. Executive Summary
- **Project Name:** Thai OpenUtau (Cyber-Thai Vocal Synthesizer & Editor)
- **Version:** `v3.9.5` (Last Scanned: `2026-11-10`)
- **Author/Architect:** Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
- **Original Foundation:** OpenUtau Contributors (stakira)
- **Target Runtimes:** .NET 8.0 - 12.0+ (`RollForward=LatestMajor`, C# 12/13, Avalonia UI 11.2.4, ReactiveUI 19.5.41)
- **Audio Synthesis Stack:** C++ Worldline DSP (`worldline.dll`), NAudio / MiniAudio, Moresampler (`moresampler.exe`), Wavtool-Simple, DiffSinger ONNX (DirectML GPU)
- **Zero-Defect Guarantee:** 0 Errors, 0 Warnings on `dotnet build`, 300+ xUnit tests passing

## 2. Solution Structure
- **Solution File:** `Thai OpenUtau.sln` (sole solution file at root for clean single-command compilation)
- **Projects:**
  1. `OpenUtau.Core` (Class Library, `net8.0`) — Central audio engine, format parsers (UST, USTX, MIDI, VSQx), singer management (`DeltaSingerProfileResolver`), resampling (`WorldlineRenderer`), package/dependency management (`PackageManager`), DSP bridge.
  2. `OpenUtau.Plugin.Builtin` (Class Library, `net8.0`) — Builtin phonemizers (`ThaiVCCVPhonemizer`, `ThaiArpasingPhonemizer`, `ArpasingPhonemizer`, etc.) and acoustic model integrations (EnunuOnnx, DiffSinger).
  3. `OpenUtau` (Executable, `net8.0-windows`/`net8.0`) — Avalonia UI layer, custom Cyber-Thai dark theme, waveform visualizers, piano roll, updater (`UpdaterViewModel`, `UpdaterDialog`), package manager UI (`PackageManagerViewModel`), Singer Hub (`SingerHubViewModel`).
  4. `OpenUtau.Test` (xUnit Test, `net8.0`) — Comprehensive unit tests covering phonemizers, rendering, security hardening, version comparison, formats, and signal chains.

## 3. Technology & Dependency Stack
- **Avalonia UI:** `11.2.4` (Cross-platform XAML GUI)
- **ReactiveUI:** `19.5.41` (MVVM reactive architecture)
- **Auto-Update:** `NetSparkleUpdater.SparkleUpdater` `2.2.3` with `DowngradableFilter` pre-download version verification
- **Audio Output:** NAudio (DirectSound, WASAPI, ASIO), MiniAudio (`SDL3.dll`)
- **Security:** `System.Security.Cryptography.Xml` (`10.0.13`), `System.Security.Cryptography.Pkcs` (`10.0.13`), Zip-Slip path traversal defenses
- **Installers & Automation:** `Install-File.bat` (`v3.3`), `Start-The-Program.bat`, `Compile_Full_Program.bat` (`v3.5.0`), `Release_Cycle.bat`, `OpenUtau.nsi` (`v1.5`), `BumpVersion.ps1` (`v1.6`)

## 4. Key Architectural Patterns
- **Preserve Before Replace:** Surgical edits only; no wholesale rewriting of established systems.
- **Pre-Install / Pre-Download Version Verification:** All installers (`Install-File.bat`, `OpenUtau.nsi`, `PrepareDirectML.ps1`, `run_dashboard.bat`), app updaters (`UpdaterViewModel`, `UpdaterDialog`), and package managers (`PackageManager`, `PackageManagerViewModel`, `SingerHubViewModel`) inspect the version already installed on the machine first and never prompt or force redundant downloads when an equal or newer version is already present.
- **Fast Plugin Deduplication:** `DocManager.SearchAllPlugins()` groups plugin candidates by `AssemblyName.Name` and skips duplicate builtin plugin copies immediately without expensive `MetadataLoadContext` reflection.
- **Humanized Smooth Envelope & Timing Safety:** Equal-Power Cosine S-Curve transitions and `Math.Max(originalPos, prevPos + 10)` minimum-safe phoneme spacing.

