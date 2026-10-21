# ============================================================================
# Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
# File Name    : update_audio_output.py
# Version      : 1.0
# Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
# File Location: AI_Agent_Commands_And_Archive/Command_Runners/update_audio_output.py
# Description  : Surgical updater for MiniAudioOutput.cs to enable independent speaker/headphone routing.
# ============================================================================
import os

file_path = os.path.join("OpenUtau.Core", "Audio", "MiniAudioOutput.cs")
with open(file_path, "r", encoding="utf-8") as f:
    text = f.read()

# 1. Mandatory Header
header = """// ============================================================================
// Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : MiniAudioOutput.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.Core/Audio/MiniAudioOutput.cs
// Description  : Enhanced audio output routing with independent Speaker/Headphone device selection and hot-swap.
// ============================================================================
"""
if not text.startswith("// =="):
    text = header + text

# 2. Update constructor to always enumerate devices
old_ctor_block = """            unsafe {
                var f = (ou_audio_data_callback_t)DataCallback;
                GCHandle.Alloc(f);
                callbackPtr = Marshal.GetFunctionPointerForDelegate(f);
            }
            if (Preferences.Default.UseSystemDefaultAudioDevice) {"""

new_ctor_block = """            unsafe {
                var f = (ou_audio_data_callback_t)DataCallback;
                GCHandle.Alloc(f);
                callbackPtr = Marshal.GetFunctionPointerForDelegate(f);
            }
            try {
                UpdateDeviceList();
            } catch (Exception ex) {
                Log.Warning(ex, "Failed to enumerate audio devices during startup.");
            }
            if (Preferences.Default.UseSystemDefaultAudioDevice) {"""

assert old_ctor_block in text, "old_ctor_block not found in MiniAudioOutput.cs"
text = text.replace(old_ctor_block, new_ctor_block, 1)

# 3. Update SelectDevice
old_select_block = """        public void SelectDevice(Guid guid, int deviceNumber) {
            if (Preferences.Default.UseSystemDefaultAudioDevice) {
                return;
            }
            if (selectedDevice != Guid.Empty && selectedDevice == guid) {
                return;
            }
            if (nativeContext != IntPtr.Zero) {
                CheckError(ou_free_audio_device(nativeContext));
                nativeContext = IntPtr.Zero;
                selectedDevice = Guid.Empty;
            }
            for (int i = 0; i < devices.Count; i++) {
                if (devices[i].guid == guid) {
                    deviceNumber = i;
                    break;
                }
                if (i == devices.Count - 1) {
                    guid = devices[0].guid;
                    deviceNumber = devices[0].deviceNumber;
                }
            }"""

new_select_block = """        public void SelectDevice(Guid guid, int deviceNumber) {
            if (guid == Guid.Empty || deviceNumber < 0) {
                if (nativeContext != IntPtr.Zero) {
                    try {
                        ou_free_audio_device(nativeContext);
                    } catch { }
                    nativeContext = IntPtr.Zero;
                    selectedDevice = Guid.Empty;
                }
                nativeContext = ou_init_audio_device_auto(callbackPtr);
                Preferences.Default.UseSystemDefaultAudioDevice = true;
                Preferences.Save();
                return;
            }

            Preferences.Default.UseSystemDefaultAudioDevice = false;
            if (selectedDevice != Guid.Empty && selectedDevice == guid && nativeContext != IntPtr.Zero) {
                return;
            }
            if (nativeContext != IntPtr.Zero) {
                CheckError(ou_free_audio_device(nativeContext));
                nativeContext = IntPtr.Zero;
                selectedDevice = Guid.Empty;
            }
            if (devices.Count == 0) {
                UpdateDeviceList();
            }
            for (int i = 0; i < devices.Count; i++) {
                if (devices[i].guid == guid) {
                    deviceNumber = i;
                    break;
                }
                if (i == devices.Count - 1 && devices.Count > 0) {
                    guid = devices[0].guid;
                    deviceNumber = devices[0].deviceNumber;
                }
            }"""

assert old_select_block in text, "old_select_block not found in MiniAudioOutput.cs"
text = text.replace(old_select_block, new_select_block, 1)

# 4. Update GetOutputDevices
old_get_block = """        public List<AudioOutputDevice> GetOutputDevices() {
            if (Preferences.Default.UseSystemDefaultAudioDevice) {
                return new List<AudioOutputDevice>();
            }
            return devices;
        }"""

new_get_block = """        public List<AudioOutputDevice> GetOutputDevices() {
            if (devices.Count == 0) {
                try {
                    UpdateDeviceList();
                } catch (Exception ex) {
                    Log.Warning(ex, "Failed to refresh audio device list.");
                }
            }
            return devices;
        }"""

assert old_get_block in text, "old_get_block not found in MiniAudioOutput.cs"
text = text.replace(old_get_block, new_get_block, 1)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(text)

print("MiniAudioOutput.cs successfully updated!")
