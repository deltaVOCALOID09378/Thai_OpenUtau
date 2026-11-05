// ============================================================================
// 1. วัน/เดือน/ปี: 1 พฤศจิกายน พ.ศ. 2569 / 01 November 2026
// 2. ชื่อไฟล์โค้ด: OpusOggWaveReader.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.2
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/Format/OpusOggWaveReader.cs
// 5. หน้าที่: ตัวอ่านไฟล์เสียงรูปแบบ Opus (.opus / .ogg) รองรับทั้งพาธไฟล์และ Stream โดยตรง
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Agentic AI | Original by OpenUtau Authors
// ============================================================================

using System;
using System.IO;
using Concentus.Oggfile;
using Concentus.Structs;
using NAudio.Wave;

namespace OpenUtau.Core.Format {
    public class OpusOggWaveReader : WaveStream {
        WaveFormat waveFormat;
        MemoryStream oggStream;
        OpusDecoder decoder;
        OpusOggReadStream decodeStream;
        byte[] wavData;

        public OpusOggWaveReader(string oggFile) {
            using (FileStream fileStream = new FileStream(oggFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) {
                oggStream = new MemoryStream();
                fileStream.CopyTo(oggStream);
            }
            InitializeDecoder();
        }

        public OpusOggWaveReader(Stream inputStream, bool leaveOpen = false) {
            if (inputStream == null) {
                throw new ArgumentNullException(nameof(inputStream));
            }
            try {
                oggStream = new MemoryStream();
                inputStream.CopyTo(oggStream);
            } finally {
                if (!leaveOpen) {
                    inputStream.Dispose();
                }
            }
            InitializeDecoder();
        }

        private void InitializeDecoder() {
            oggStream.Seek(0, SeekOrigin.Begin);
            waveFormat = new WaveFormat(48000, 16, 2);
            decoder = new OpusDecoder(48000, 2);
            decodeStream = new OpusOggReadStream(decoder, oggStream);
        }

        byte[] Decode() {
            using (var wavStream = new MemoryStream()) {
                oggStream.Seek(0, SeekOrigin.Begin);
                var packetDecoder = new OpusDecoder(48000, 2);
                var oggIn = new OpusOggReadStream(packetDecoder, oggStream);
                while (oggIn.HasNextPacket) {
                    short[] packet = oggIn.DecodeNextPacket();
                    if (packet != null) {
                        byte[] binary = ShortsToBytes(packet);
                        wavStream.Write(binary, 0, binary.Length);
                    }
                }
                return wavStream.ToArray();
            }
        }

        public override WaveFormat WaveFormat => waveFormat;

        public override long Length {
            get {
                if (wavData == null) {
                    wavData = Decode();
                }
                return wavData.LongLength;
            }
        }

        public override TimeSpan TotalTime => decodeStream.TotalTime;

        public override long Position { get; set; }

        public override int Read(byte[] buffer, int offset, int count) {
            if (wavData == null) {
                wavData = Decode();
            }
            int n = (int)Math.Min(wavData.Length - Position, count);
            if (n <= 0) {
                return 0;
            }
            Array.Copy(wavData, Position, buffer, offset, n);
            Position += n;
            return n;
        }

        static byte[] ShortsToBytes(short[] input) {
            byte[] output = new byte[input.Length * sizeof(short) / sizeof(byte)];
            for (int i = 0; i < input.Length; ++i) {
                output[i * 2] = (byte)(input[i] & 0xFF);
                output[i * 2 + 1] = (byte)((input[i] >> 8) & 0xFF);
            }
            return output;
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                oggStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
