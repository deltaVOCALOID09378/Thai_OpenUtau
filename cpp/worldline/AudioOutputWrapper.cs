/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: AudioOutputWrapper.cs
 * เวอร์ชั่น: 1.0
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & Staff
 * รายละเอียดการแก้ไข: คลาสเชื่อมต่อ C++ DLL (P/Invoke) ป้องกัน GC จัดเก็บ Delegate และแปลง UTF-8 อัตโนมัติ
 */

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace DeltaSynth.Worldline
{
    // โครงสร้างรับข้อมูลดิบจาก C++
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeAudioDeviceInfo
    {
        public IntPtr name;
        public ulong id;
        public IntPtr api;
        public uint api_id;
    }

    // Delegate ควบคุม Callback (Cdecl)
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AudioDataCallback(IntPtr buffer, uint channels, uint frameCount);

    // โครงสร้างข้อมูลระดับสูงสำหรับใช้งานใน C#
    public class AudioDeviceInfo
    {
        public string Name { get; set; } = string.Empty;
        public ulong Id { get; set; }
        public string Api { get; set; } = string.Empty;
        public uint ApiId { get; set; }
    }

    public class AudioEngine : IDisposable
    {
        private const string DllName = "worldline"; // รองรับทั้ง .dll และ .so อัตโนมัติใน .NET 8+

        // P/Invoke Declarations
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ou_get_audio_device_infos([Out] NativeAudioDeviceInfo[] device_infos, int max_count);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ou_free_audio_device_infos([In] NativeAudioDeviceInfo[] device_infos, int count);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ou_init_audio_device(uint api_id, ulong id, AudioDataCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ou_init_audio_device_auto(AudioDataCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ou_free_audio_device(IntPtr context);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ou_audio_device_start(IntPtr context);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ou_audio_device_stop(IntPtr context);

        private IntPtr _context = IntPtr.Zero;
        private AudioDataCallback? _keptAliveCallback; // ป้องกัน GC ลบ Callback ทิ้งระหว่างรัน (Zero-Defect)
        private bool _disposed = false;

        /// <summary>
        /// ดึงข้อมูลอุปกรณ์เสียงทั้งหมด
        /// </summary>
        public static List<AudioDeviceInfo> GetAudioDevices(int maxCount = 20)
        {
            var nativeInfos = new NativeAudioDeviceInfo[maxCount];
            int count = ou_get_audio_device_infos(nativeInfos, maxCount);
            var result = new List<AudioDeviceInfo>(count);

            for (int i = 0; i < count; i++)
            {
                result.Add(new AudioDeviceInfo
                {
                    Name = Marshal.PtrToStringUTF8(nativeInfos[i].name) ?? "Unknown",
                    Id = nativeInfos[i].id,
                    Api = Marshal.PtrToStringUTF8(nativeInfos[i].api) ?? "Unknown",
                    ApiId = nativeInfos[i].api_id
                });
            }

            ou_free_audio_device_infos(nativeInfos, count); // คืนค่า Memory กลับให้ C++
            return result;
        }

        /// <summary>
        /// เริ่มต้นใช้งานอุปกรณ์เสียงอัตโนมัติ
        /// </summary>
        public bool InitializeAuto(AudioDataCallback callback)
        {
            _keptAliveCallback = callback; // ถือครอง Reference ไว้
            _context = ou_init_audio_device_auto(_keptAliveCallback);
            return _context != IntPtr.Zero;
        }

        public bool Start() => _context != IntPtr.Zero && ou_audio_device_start(_context) == 0;
        public bool Stop() => _context != IntPtr.Zero && ou_audio_device_stop(_context) == 0;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_context != IntPtr.Zero)
                {
                    ou_audio_device_stop(_context);
                    ou_free_audio_device(_context);
                    _context = IntPtr.Zero;
                }
                _keptAliveCallback = null;
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AudioEngine() => Dispose(false);
    }
}