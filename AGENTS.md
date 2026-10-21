# DELTA SYNTH — AGENTS.md (Thai OpenUtau Standard)

## AI Development, Architecture & Code Guardian Guidelines

> **Project:** Thai OpenUtau (Cyber-Thai Vocal Synthesizer & Editor)  
> **Master Directive:** Preserve → Strengthen → Optimize → Verify  
> **Credit Header Format:**  
> `Made And Checked By DELTA SYNTH & All Code Agentic AI Engine`  
> `Original by OpenUtau Contributors`

---

## 1. Prime Directive & Code Standards
1. **Preserve Before Replace:**
   - Always preserve working functionality. Never replace or rewrite existing working architectures without empirical necessity.
   - Use surgical edits to target root causes directly.
2. **Zero Known Defects:**
   - Always verify that `dotnet build` succeeds with **0 Error(s), 0 Warning(s)**.
   - Ensure all 282 unit tests in `OpenUtau.Test` pass before finishing tasks.
3. **Single Solution Standard:**
   - `Thai OpenUtau.sln` is the sole active solution file at root.
   - Any auxiliary or experimental solutions must reside in `AI_Agent_Commands_And_Archive/Unused_Files/` to prevent MSBuild MSB1011 errors.
4. **No Duplicate Files:**
   - Google Drive sync conflict files (e.g. `* (1).*`) must never be added to compilation.
   - All duplicate files must be merged into canonical source files.

## 2. Directory Layout & Boundaries
- `OpenUtau/` : Avalonia UI presentation layer (XAML, ViewModels, Custom Themes).
- `OpenUtau.Core/` : Core synthesis engine, render pipelines, phonemizers, file parsers.
- `OpenUtau.Plugin.Builtin/` : Default phonemizers and acoustic model engines.
- `OpenUtau.Test/` : xUnit test suite (282 test cases).
- `AI_Agent_Commands_And_Archive/` : Archival directory for unused solutions, large text dumps, and agent tooling.
- `Tools/` : Diagnostic and analytical tools, including Voicebank and phoneme dashboards.

## 3. UI Guidelines
- Default font: `Leelawadee UI`
- Core Palette: Red `#CC2200`, Black `#1A1A1A`, White `#F0F0F0` (with Cyber-Thai Slate-Indigo dark theme accents).
- Language support: Thai and English only.
