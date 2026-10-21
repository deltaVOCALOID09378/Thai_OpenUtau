# ============================================================================
# Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
# File Name    : update_preferences_vm.py
# Version      : 1.0
# Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
# File Location: AI_Agent_Commands_And_Archive/Command_Runners/update_preferences_vm.py
# Description  : Surgical updater for PreferencesViewModel.cs to allow dynamic headphone/speaker selection.
# ============================================================================
import os

file_path = os.path.join("OpenUtau", "ViewModels", "PreferencesViewModel.cs")
with open(file_path, "r", encoding="utf-8") as f:
    text = f.read()

# 1. Header
header = """// ============================================================================
// Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : PreferencesViewModel.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau/ViewModels/PreferencesViewModel.cs
// Description  : Enhanced audio routing enabling immediate Speaker/Headphone selection and reactive switching.
// ============================================================================
"""
if not text.startswith("// =="):
    text = header + text

# 2. Update device initialization
old_init_block = """            var audioOutput = PlaybackManager.Inst.AudioOutput;
            if (audioOutput != null) {
                AudioOutputDevices = audioOutput.GetOutputDevices();
                int deviceNumber = audioOutput.DeviceNumber;
                var device = AudioOutputDevices.FirstOrDefault(d => d.deviceNumber == deviceNumber);
                if (device != null) {
                    AudioOutputDevice = device;
                }
            }"""

new_init_block = """            var audioOutput = PlaybackManager.Inst.AudioOutput;
            if (audioOutput != null) {
                AudioOutputDevices = audioOutput.GetOutputDevices();
                int deviceNumber = audioOutput.DeviceNumber;
                var device = AudioOutputDevices.FirstOrDefault(d => d.deviceNumber == deviceNumber);
                if (device != null) {
                    AudioOutputDevice = device;
                } else if (AudioOutputDevices != null && AudioOutputDevices.Count > 0) {
                    AudioOutputDevice = AudioOutputDevices[0];
                }
            }"""

assert old_init_block in text, "old_init_block not found in PreferencesViewModel.cs"
text = text.replace(old_init_block, new_init_block, 1)

# 3. Update subscriptions for UseSystemDefaultDevice and AudioOutputDevice
old_sub_block = """            this.WhenAnyValue(vm => vm.UseSystemDefaultDevice)
                .Subscribe(useDefault => {
                    Preferences.Default.UseSystemDefaultAudioDevice = useDefault;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.AudioOutputDevice)
                .WhereNotNull()
                .SubscribeOn(RxApp.MainThreadScheduler)
                .Subscribe(device => {
                    if (UseSystemDefaultDevice) {
                        return;
                    }
                    if (PlaybackManager.Inst.AudioOutput != null) {
                        try {
                            PlaybackManager.Inst.AudioOutput.SelectDevice(device.guid, device.deviceNumber);
                        } catch (Exception e) {
                            DocManager.Inst.ExecuteCmd(new ErrorMessageNotification($"Failed to select device {device.name}", e));
                        }
                    }
                });"""

new_sub_block = """            this.WhenAnyValue(vm => vm.UseSystemDefaultDevice)
                .Subscribe(useDefault => {
                    Preferences.Default.UseSystemDefaultAudioDevice = useDefault;
                    Preferences.Save();
                    if (useDefault) {
                        if (PlaybackManager.Inst.AudioOutput != null) {
                            try {
                                PlaybackManager.Inst.AudioOutput.SelectDevice(Guid.Empty, -1);
                            } catch (Exception ex) {
                                Log.Warning(ex, "Failed to switch to default audio device.");
                            }
                        }
                    } else if (AudioOutputDevice != null) {
                        if (PlaybackManager.Inst.AudioOutput != null) {
                            try {
                                PlaybackManager.Inst.AudioOutput.SelectDevice(AudioOutputDevice.guid, AudioOutputDevice.deviceNumber);
                            } catch (Exception ex) {
                                Log.Warning(ex, $"Failed to switch to audio device {AudioOutputDevice.name}.");
                            }
                        }
                    }
                });
            this.WhenAnyValue(vm => vm.AudioOutputDevice)
                .WhereNotNull()
                .SubscribeOn(RxApp.MainThreadScheduler)
                .Subscribe(device => {
                    if (PlaybackManager.Inst.AudioOutput != null) {
                        try {
                            if (UseSystemDefaultDevice) {
                                UseSystemDefaultDevice = false;
                            }
                            PlaybackManager.Inst.AudioOutput.SelectDevice(device.guid, device.deviceNumber);
                        } catch (Exception e) {
                            DocManager.Inst.ExecuteCmd(new ErrorMessageNotification($"Failed to select device {device.name}", e));
                        }
                    }
                });"""

assert old_sub_block in text, "old_sub_block not found in PreferencesViewModel.cs"
text = text.replace(old_sub_block, new_sub_block, 1)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(text)

print("PreferencesViewModel.cs successfully updated!")
