/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: CanonAutoPitchBridge.cpp
 * เวอร์ชั่น: 1.1
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & Staff
 * Original by: OpenUtau Contributors
 * รายละเอียดการแก้ไข: ปิดช่องโหว่ Memory/Handle Leak ด้วย RAII (Smart Pointers), ปรับ String Reservation
 */

#define UNICODE
#define _UNICODE
#include <windows.h>
#include <shellapi.h>

#include <filesystem>
#include <iostream>
#include <string>
#include <vector>
#include <memory>

namespace fs = std::filesystem;

namespace {

// RAII Deleters สำหรับ Windows API ป้องกัน Leak แบบ 100%
struct LocalFreeDeleter {
    void operator()(void* p) const { if (p) LocalFree(p); }
};

struct HandleDeleter {
    void operator()(HANDLE h) const { 
        if (h && h != INVALID_HANDLE_VALUE) CloseHandle(h); 
    }
};
using ScopedHandle = std::unique_ptr<std::remove_pointer_t<HANDLE>, HandleDeleter>;

std::wstring QuoteWindowsArgument(const std::wstring& value) {
    std::wstring quoted;
    quoted.reserve(value.size() + 10); // Optimization: ลดการ Reallocate
    quoted.push_back(L'\"');
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
    MessageBoxW(nullptr, message.c_str(), L"DELTA SYNTH: Canon AutoPitch Bridge",
                MB_OK | MB_ICONERROR);
    std::wcerr << message << L'\n';
    return exitCode;
}

}  // namespace

int WINAPI wWinMain(HINSTANCE, HINSTANCE, PWSTR, int) {
    int argc = 0;
    // ใช้ Smart Pointer คุม LocalFree
    std::unique_ptr<wchar_t*, LocalFreeDeleter> argv(CommandLineToArgvW(GetCommandLineW(), &argc));
    
    if (!argv || argc != 2 || argv.get()[1] == nullptr || std::wstring(argv.get()[1]).empty()) {
        return ShowError(L"No UST input was supplied. Run this bridge from UTAU/OpenUtau.", 2);
    }

    const fs::path ustPath = fs::absolute(fs::path(argv.get()[1]));
    
    // คืนค่า Memory ทันทีที่ไม่ได้ใช้
    argv.reset(); 
    
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

    // ใช้ ScopedHandle ปิด Handle อัตโนมัติเมื่อจบขอบเขตฟังก์ชัน
    ScopedHandle hThread(process.hThread);
    ScopedHandle hProcess(process.hProcess);

    const DWORD waitResult = WaitForSingleObject(process.hProcess, INFINITE);
    if (waitResult != WAIT_OBJECT_0) {
        return ShowError(
            L"Waiting for Canon AutoPitchwriter failed. Windows error: "
                + std::to_wstring(GetLastError()), 7);
    }

    DWORD canonExitCode = 1;
    if (!GetExitCodeProcess(process.hProcess, &canonExitCode)) {
        return ShowError(
            L"Canon AutoPitchwriter finished, but its exit code was unavailable. "
            L"Windows error: " + std::to_wstring(GetLastError()), 8);
    }

    return static_cast<int>(canonExitCode);
}