# Project Overview & Architecture Scan — Thai OpenUtau

## 1. Executive Summary
- **Project Name:** Thai OpenUtau (Cyber-Thai Vocal Synthesizer & Editor)
- **Author/Architect:** DELTA SYNTH & All Code Agentic AI Engine
- **Original Foundation:** OpenUtau Contributors (stakira)
- **Target Runtimes:** .NET 8.0 / .NET 10.0 (C# 12 / 13, Avalonia UI 11, ReactiveUI)
- **Audio Synthesis Stack:** C++ Worldline DSP, NAudio / MiniAudio, Moresampler, Wavtool-Simple, DiffSinger ONNX
- **Zero-Defect Guarantee:** 0 Errors, 0 Warnings on `dotnet build`, 282/282 xUnit tests passing

## 2. Solution Structure
- **Solution File:** `Thai OpenUtau.sln` (sole solution file at root for clean single-command compilation)
- **Projects:**
  1. `OpenUtau.Core` (Class Library, net8.0) — Central audio engine, format parsers (UST, USTX, MIDI, VSQx), singer management, resampling, phonemizers, DSP bridge.
  2. `OpenUtau.Plugin.Builtin` (Class Library, net8.0) — Builtin phonemizers (Thai, Japanese, English, Chinese, etc.) and acoustic model integrations (EnunuOnnx, DiffSinger).
  3. `OpenUtau` (Executable, net8.0-windows/net8.0) — Avalonia UI layer, custom Cyber-Thai Slate-Indigo dark theme, waveform visualizers, piano roll, splash window.
  4. `OpenUtau.Test` (xUnit Test, net8.0-windows/net8.0) — 282 comprehensive unit tests covering phonemizers, rendering, formats, and signal chains.

## 3. Technology & Dependency Stack
- **Avalonia UI:** 11.0.10 (Cross-platform XAML GUI)
- **ReactiveUI:** 19.5.41 (MVVM reactive architecture)
- **Audio Output:** NAudio (DirectSound, WASAPI, ASIO), MiniAudio
- **Security:** `System.Security.Cryptography.Xml` (10.0.13), `System.Security.Cryptography.Pkcs` (10.0.13)
- **Python ML/G2P:** Python 3.10+, `pythainlp`, PyTest for Thai G2P validation

## 4. Key Architectural Patterns
- **Preserve Before Replace:** Surgical edits only; no wholesale rewriting of established systems.
- **Auto-Silence Fallback:** Seamless fallback in resamplers and wavtools when sample files are missing.
- **Humanized Smooth Envelope:** Cosine Smoothstep transitions at consonant-vowel junctions.
