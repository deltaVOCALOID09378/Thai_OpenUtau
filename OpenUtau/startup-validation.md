# Thai OpenUtau Startup Validation

## Scope

Validate application startup, single-instance coordination,
font lookup, logging, and shutdown.

These tests do not validate phonemizers, synthesis quality,
project compatibility, or all editor functions.

## Build prerequisites

- The application targets .NET 8 or later.
- Existing Avalonia and Serilog package references are preserved.
- StartupSettings.props is imported by the application project.
- The original project license and attribution are preserved.

## Build checks

Run from the repository root:

    dotnet restore
    dotnet build -c Release

Acceptance criteria:

- Restore succeeds.
- Build succeeds.
- No new startup-related compiler warnings are left unexplained.
- The generated runtime configuration contains the intended rollForward policy.

## Startup tests

| ID | Scenario | Expected result |
|---|---|---|
| START-01 | Launch the normal executable | Main window opens and startup information is logged |
| START-02 | Launch the DLL through dotnet where supported | Main window opens |
| START-03 | Launch a second instance in the same user/session scope | The second instance exits before opening application logging |
| START-04 | Close the first instance and launch again | New instance opens normally |
| START-05 | Terminate the application externally, then relaunch | No stale process permanently prevents a new launch |
| START-06 | Open the Avalonia designer | Application builder does not acquire the application mutex |

## Linux font lookup tests

Perform PATH manipulation only in a disposable test environment.
Do not rename or remove system font utilities.

| ID | Scenario | Expected result |
|---|---|---|
| FONT-01 | fc-match is available and succeeds | A non-empty family can become the default |
| FONT-02 | fc-match is unavailable | A warning is logged and startup continues |
| FONT-03 | A test fc-match executable returns nonzero | A warning is logged and startup continues |
| FONT-04 | A test fc-match executable produces empty output | Normal Avalonia font selection remains available |
| FONT-05 | A test fc-match executable sleeps beyond the timeout | Lookup is cancelled and startup continues after bounded best-effort cleanup |
| FONT-06 | A test fc-match executable writes heavily to stdout and stderr | Both pipes are drained without a wait-before-read deadlock |

The font lookup timeout is not an exact upper bound for total application
startup time. Process creation, best-effort termination, and other startup
work also take time.

## Thai text rendering tests

Suggested text:

    ภาษาไทย
    น้ำ เก้า กุ้ง ปี่ ผู้ใช้
    Thai OpenUtau ภาษาไทย 123

| ID | Scenario | Expected result |
|---|---|---|
| THAI-01 | Inspect menus and dialogs | Thai text is readable and not clipped |
| THAI-02 | Inspect vowels and tone marks | Marks are positioned acceptably for the selected font |
| THAI-03 | Test mixed Thai and Latin text | Both scripts display without missing-glyph boxes |
| THAI-04 | Test a machine without Noto Sans Thai | Available system fallback behavior is documented |
| THAI-05 | Test supported display scaling levels | Text and controls remain usable |

A fallback family declaration does not install or embed a font.
If deterministic rendering is required, font packaging and licensing
must be addressed separately.

## Logging and shutdown tests

| ID | Scenario | Expected result |
|---|---|---|
| LOG-01 | Close the main window normally | Logs are flushed and the process exits with code 0 |
| LOG-02 | Simulate an exception escaping Run in a test build | Failure is reported and the process returns a nonzero code |
| LOG-03 | Simulate a NetMQ cleanup failure in a test build | Logger and mutex cleanup are still attempted |
| LOG-04 | Simulate an unwritable log directory | Startup fails rather than silently proceeding without required logging |
| LOG-05 | Close the app after using synthesis and related services | No remaining foreground worker prevents process exit |

## Platform notes

- NetMQ cleanup is intentionally skipped on macOS, matching the previous implementation.
- Validate macOS shutdown separately before changing that behavior.
- A mutex coordinates application instances; it is not a security boundary.
- On Windows, the current mutex name is normally session-scoped.
- stderr messages may not be visible when launching a GUI application.
- Forced termination cannot guarantee that all logs are flushed.
- Environment.Exit is intentionally not used for normal shutdown.
- If the process remains alive after the window closes, investigate
  service and worker shutdown instead of immediately forcing exit.

## Merge checklist

- Windows startup and shutdown tested.
- Linux startup and font fallback tested.
- macOS behavior tested if the platform is supported.
- Executable and dotnet-based launches tested where applicable.
- Original and updated builds compared with the same project.
- At least one rendering operation completed before shutdown testing.
- No claim of Thai phonemizer correctness is based on startup tests alone.