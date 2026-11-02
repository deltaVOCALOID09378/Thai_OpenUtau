// CanonAutoPitchBridge.cpp
// Strict bridge for Canon AutoPitchwriter Ver.1.7.0.
// This program never generates its own pitch, never applies a fallback,
// and never reads or modifies voicebank oto.ini.

#define UNICODE
#define _UNICODE
#include <windows.h>
#include <shellapi.h>

#include <filesystem>
#include <iostream>
#include <string>
#include <vector>

namespace fs = std::filesystem;

namespace {

std::wstring QuoteWindowsArgument(const std::wstring& value) {
    std::wstring quoted = L"\"";
    std::size_t backslashes = 0;

    for (wchar_t ch : value) {
        if (ch == L'\\') {
            ++backslashes;
            continue;
        }
        if (ch == L'\"') {
            quoted.append(backslashes * 2 + 1, L'\\');
            quoted.push_back(L'\"');
            backslashes = 0;
            continue;
        }
        quoted.append(backslashes, L'\\');
        backslashes = 0;
        quoted.push_back(ch);
    }

    quoted.append(backslashes * 2, L'\\');
    quoted.push_back(L'\"');
    return quoted;
}

fs::path CurrentExecutableDirectory() {
    std::wstring buffer(32768, L'\0');
    const DWORD length = GetModuleFileNameW(
        nullptr, buffer.data(), static_cast<DWORD>(buffer.size()));
    if (length == 0 || length >= buffer.size()) {
        throw std::runtime_error("Unable to locate CanonAutoPitchBridge.exe");
    }
    buffer.resize(length);
    return fs::path(buffer).parent_path();
}

int ShowError(const std::wstring& message, int exitCode) {
    MessageBoxW(nullptr, message.c_str(), L"Canon AutoPitch Bridge",
                MB_OK | MB_ICONERROR);
    std::wcerr << message << L'\n';
    return exitCode;
}

}  // namespace

int WINAPI wWinMain(HINSTANCE, HINSTANCE, PWSTR, int) {
    // UTAU/OpenUtau plug-ins receive the selected temporary UST as argv[1].
    int argc = 0;
    LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (argv == nullptr || argc != 2 || argv[1] == nullptr ||
        std::wstring(argv[1]).empty()) {
        if (argv != nullptr) LocalFree(argv);
        return ShowError(
            L"No UST input was supplied. Run this bridge from UTAU/OpenUtau.", 2);
    }

    const fs::path ustPath = fs::absolute(fs::path(argv[1]));
    LocalFree(argv);
    if (!fs::exists(ustPath) || !fs::is_regular_file(ustPath)) {
        return ShowError(L"UST input file was not found:\n" + ustPath.wstring(), 3);
    }

    fs::path bridgeDirectory;
    try {
        bridgeDirectory = CurrentExecutableDirectory();
    } catch (const std::exception&) {
        return ShowError(L"Unable to determine the plug-in directory.", 4);
    }

    const fs::path canonExecutable = bridgeDirectory / L"AutoPitchwriter.exe";
    if (!fs::exists(canonExecutable) || !fs::is_regular_file(canonExecutable)) {
        return ShowError(
            L"Canon AutoPitchwriter.exe was not found beside the bridge.\n"
            L"No substitute pitch will be generated.", 5);
    }

    // Canon's original executable is the only tuning engine. Keeping the
    // working directory beside it also preserves its original settings file.
    std::wstring commandLine = QuoteWindowsArgument(canonExecutable.wstring())
                             + L" " + QuoteWindowsArgument(ustPath.wstring());
    std::vector<wchar_t> mutableCommand(commandLine.begin(), commandLine.end());
    mutableCommand.push_back(L'\0');

    STARTUPINFOW startup{};
    startup.cb = sizeof(startup);
    PROCESS_INFORMATION process{};

    const BOOL started = CreateProcessW(
        canonExecutable.c_str(),
        mutableCommand.data(),
        nullptr,
        nullptr,
        FALSE,
        0,
        nullptr,
        bridgeDirectory.c_str(),
        &startup,
        &process);

    if (!started) {
        return ShowError(
            L"Canon AutoPitchwriter could not start. Windows error: "
                + std::to_wstring(GetLastError()), 6);
    }

    CloseHandle(process.hThread);
    const DWORD waitResult = WaitForSingleObject(process.hProcess, INFINITE);
    if (waitResult != WAIT_OBJECT_0) {
        const DWORD error = GetLastError();
        CloseHandle(process.hProcess);
        return ShowError(
            L"Waiting for Canon AutoPitchwriter failed. Windows error: "
                + std::to_wstring(error), 7);
    }

    DWORD canonExitCode = 1;
    if (!GetExitCodeProcess(process.hProcess, &canonExitCode)) {
        const DWORD error = GetLastError();
        CloseHandle(process.hProcess);
        return ShowError(
            L"Canon AutoPitchwriter finished, but its exit code was unavailable. "
            L"Windows error: " + std::to_wstring(error), 8);
    }
    CloseHandle(process.hProcess);

    // Return Canon's own status unchanged. The bridge does not post-process
    // PBS/PBW/PBY/PBM/VBR and never accesses oto.ini.
    return static_cast<int>(canonExitCode);
}
