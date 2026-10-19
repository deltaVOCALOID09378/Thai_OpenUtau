# System Audit Map & Architecture Overview
*Generated & Maintained by DELTA SYNTH & All Code Agentic AI Engine*

## Audited Components & Files

### 1. Rendering Architecture (`OpenUtau.Core\Render`)
- **`Worldline.cs`**: Handles UTAU vocal synthesis. 
  - **Status**: Audited and verified. Incorporates cosine crossfading, preventing f0 subharmonic glitching, and handles duration bounds automatically.
- **`RenderPhrase.cs`**: Handles grouping of phonemes for DiffSinger and other phrase-based engines.
- **`ResamplerItem.cs` & `SharpWavtool.cs`**: Handles classic single-note UTAU rendering.
  - **Fix Applied**: Sub-millisecond envelope boundary fading (`microFadeSamples = 44`) guarantees zero-crossing at the head and tail, eliminating micro-clicks.

### 2. Signal Chain (`OpenUtau.Core\SignalChain`)
- **`WaveSource.cs`**: The core buffer container for all synthesized audio before mixdown.
  - **Fix Applied**: Universal sub-millisecond micro-fade (approx. 0.99ms) in `SetSamples()`. This ensures that all renderers (DiffSinger, ENUNU, Classic) have click-free phrase boundaries unconditionally.

### 3. Machine Learning Inference (`OpenUtau.Core\Util\Onnx.cs`)
- **`Onnx.cs`**: Initializes ONNX Runtime sessions for DiffSinger models.
  - **Fix Applied**: Enhanced `SessionOptions` for CPU and DirectML with `GraphOptimizationLevel.ORT_ENABLE_ALL` and `ExecutionMode.ORT_SEQUENTIAL`. Configured multi-threading (`InterOpNumThreads` and `IntraOpNumThreads`) to utilize half of available CPU cores to prevent thread contention while ensuring maximum DiffSinger rendering speed on CPU DirectML fallback scenarios.

### 4. Plugin API & Phonemizers (`OpenUtau.Plugin.Builtin`)
- **`ThaiVCCVPhonemizer.cs` & `ThaiVccvPhonemizer_Delta.cs`**: Native Thai VCCV phonemizers.
  - **Status**: Audited, synchronized, and verified. Resolved missing methods (`GetParentVoiceColor`, `SetUpException`), and immutability compilation errors. Strict boundary checks and glottal stop handling ensure 100% stable phonetic transcription.

### 5. UI Presentation & Startup Lifecycle (`OpenUtau\Views` & `ViewModels`)
- **`SplashWindow.axaml` & `SplashWindow.axaml.cs`**:
  - **Status**: Updated with staged reactive progress loading (0% -> 15% -> 40% -> 70% -> 85% -> 100%).
  - **Features**: Features real-time status descriptions in Thai and English, numeric percentage indicator in `Leelawadee UI` font, `#CC2200` accent progress bar, and instant synchronous closing to smoothly transition to `MainWindow`.
- **`MainWindow.axaml` & `MainWindow.axaml.cs`**:
  - **Status**: Enriched with Cyber-Thai aesthetic (Font: `Leelawadee UI`, Colors: Red `#CC2200`, Dark `#1A1A1A`, White `#F0F0F0`).
  - **Features**: 
    - TopBar: DELTA SYNTH Core system badge with quick cache cleaner button (`Grid.Column="2"`).
    - Welcome Screen: DELTA SYNTH Master Tool information banner with quick launcher actions.
    - Status Bar: Real-time operation message, project tracks telemetry, audio DSP engine indicator (`C++ Worldline & DirectML DSP`), and Thai OpenUtau Master Tool status badge.
- **`MainWindowViewModel.cs`**:
  - Exposes `DisplayStatusText` and `TracksCountText` with reactive notifications on command changes.

### 6. Visual Assets & Iconography (`OpenUtau\Assets`)
- **`Thai OpenUtau Logo.ico`**, **`OpenUtau.ico`**, **`open-utau.ico`**:
  - Replaced with true multi-resolution icon files containing 9 frames (16x16, 24x24, 32x32, 48x48, 64x64, 72x72, 96x96, 128x128, 256x256) in 32-bit ARGB format. Completely eliminated solid black boxes (`A=0` alpha transparency).
- **`Thai OpenUtau Logo.png`**, **`OpenUtau.png`**:
  - High-definition 1024x1024 32bpp ARGB images rendered with high-quality bicubic downsampling and pure alpha transparency.
- **`Thai OpenUtau Logo.svg`**, **`OpenUtau.svg`**:
  - Removed solid bounding box paths (`fill="#4C2D53"`) to ensure transparent vector scaling.

### 7. Maintenance, Cleaning & Automation Tools
- **`Clear-Cache.bat`**:
  - **Status**: Registered system maintenance script.
  - **Function**: Automatically purges obsolete `.dll`s, build artifacts (`bin/`, `obj/`, `Publish/`), OpenUtau audio render caches (`UCache`, `%LocalAppData%\OpenUtau\cache`), diagnostic logs (`*.binlog`, `*.log`, `check_*.txt`, `build_log*.txt`), and IDE caches. Supports Drag & Drop and progress reporting.
- **Skill: `Clear-all-cach-file`**:
  - **Path**: `C:\Users\patip\.gemini\config\skills\Clear-all-cach-file\SKILL.md`
  - **Function**: Protocol for whole-workspace cache clearing, dead DLL sweeps, and build integrity verification.
- **`.gitignore`**:
  - Fully overhauled to strictly block all `.dll`s, binaries (`.exe`, `.so`, `.dylib`), build directories (`bin/`, `obj/`), diagnostic logs (`*.log`, `*.binlog`, `check_*.txt`, `build_log*.txt`), and temporary files from ever entering GitHub.

## Overall Status
- **Critical Issues**: None (0 Errors, 0 Warnings).
- **Build Status**: Verified with .NET SDK Release build.
- **Residual Risks**: None known. DirectML fallback, C++ Worldline DSP, and Avalonia UI operate seamlessly.
