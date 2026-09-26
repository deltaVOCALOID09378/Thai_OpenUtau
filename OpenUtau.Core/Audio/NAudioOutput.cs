using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using OpenUtau.Core.Util;

namespace OpenUtau.Audio {
#if !WINDOWS
    public class NAudioOutput : DummyAudioOutput { }
#else
    public class NAudioOutput : IAudioOutput {
        const int Channels = 2;

        private readonly object lockObj = new object();
        private DirectSoundOut directSoundOut;
        private int deviceNumber;

        public NAudioOutput() {
            if (Guid.TryParse(Preferences.Default.PlaybackDevice, out var guid)) {
                SelectDevice(guid, Preferences.Default.PlaybackDeviceNumber);
            } else {
                SelectDevice(new Guid(), 0);
            }
        }

        public PlaybackState PlaybackState {
            get {
                lock (lockObj) {
                    return directSoundOut == null ? PlaybackState.Stopped : directSoundOut.PlaybackState;
                }
            }
        }

        public int DeviceNumber => deviceNumber;

        public long GetPosition() {
            lock (lockObj) {
                return directSoundOut == null
                    ? 0
                    : directSoundOut.GetPosition() / Channels;
            }
        }

        public void Init(ISampleProvider sampleProvider) {
            lock (lockObj) {
                if (directSoundOut != null) {
                    directSoundOut.Stop();
                    directSoundOut.Dispose();
                }
                var devices = new List<DirectSoundDeviceInfo>(DirectSoundOut.Devices);
                Guid guid = new Guid();
                if (deviceNumber < devices.Count) {
                    guid = devices[deviceNumber].Guid;
                }
                
                directSoundOut = new DirectSoundOut(guid, 100);
                directSoundOut.Init(sampleProvider);
            }
        }

        public void Pause() {
            lock (lockObj) {
                if (directSoundOut != null) {
                    directSoundOut.Pause();
                }
            }
        }

        public void Play() {
            lock (lockObj) {
                if (directSoundOut != null) {
                    directSoundOut.Play();
                }
            }
        }

        public void Stop() {
            lock (lockObj) {
                if (directSoundOut != null) {
                    directSoundOut.Stop();
                    directSoundOut.Dispose();
                    directSoundOut = null;
                }
            }
        }

        public void SelectDevice(Guid guid, int deviceNumber) {
            Preferences.Default.PlaybackDevice = guid.ToString();
            Preferences.Default.PlaybackDeviceNumber = deviceNumber;
            Preferences.Save();
            var list = new List<DirectSoundDeviceInfo>(DirectSoundOut.Devices);
            // Product guid may not be unique. Use device number first.
            if (deviceNumber < list.Count && list[deviceNumber].Guid == guid) {
                this.deviceNumber = deviceNumber;
                return;
            }
            // If guid does not match, device number may have changed. Search guid instead.
            this.deviceNumber = 0;
            for (int i = 0; i < list.Count; ++i) {
                if (list[i].Guid == guid) {
                    this.deviceNumber = i;
                    break;
                }
            }
        }

        public List<AudioOutputDevice> GetOutputDevices() {
            var outDevices = new List<AudioOutputDevice>();
            var devices = DirectSoundOut.Devices;
            int i = 0;
            foreach (var capability in devices) {
                outDevices.Add(new AudioOutputDevice {
                    api = "DirectSound",
                    name = capability.Description,
                    deviceNumber = i,
                    guid = capability.Guid,
                });
                i++;
            }
            return outDevices;
        }
    }
#endif
}
