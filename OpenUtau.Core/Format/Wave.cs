/*
 * Made And Checked By DELTA SYNTH & Gemini AI
 * Original by OpenUtau Contributors
 * Version: 2.0 (Resilient Multi-Format Audio Decoder with Universal Fallbacks)
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Flac;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;
using NWaves.Signals;

namespace OpenUtau.Core.Format {
    public static class Wave {
        public static Func<string, WaveStream> OverrideMp3Reader;

        public static WaveStream OpenFile(string filepath) {
            if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath)) {
                Serilog.Log.Warning($"Audio file does not exist: {filepath}. Returning silence fallback.");
                return new RawSourceWaveStream(new MemoryStream(new byte[8820]), new WaveFormat(44100, 16, 1));
            }

            var fileInfo = new FileInfo(filepath);
            if (fileInfo.Length < 12) {
                Serilog.Log.Warning($"Audio file '{filepath}' is empty or too small ({fileInfo.Length} bytes). Returning silence fallback.");
                return new RawSourceWaveStream(new MemoryStream(new byte[8820]), new WaveFormat(44100, 16, 1));
            }

            var ext = (Path.GetExtension(filepath) ?? "").ToLowerInvariant();
            byte[] buffer = new byte[1024];
            int bytesRead = 0;
            string tag = "";

            try {
                using (var stream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    if (stream.CanSeek) {
                        bytesRead = stream.Read(buffer, 0, Math.Min(buffer.Length, (int)stream.Length));
                        if (bytesRead >= 4) {
                            tag = System.Text.Encoding.UTF8.GetString(buffer.AsSpan(0, 4));
                        }
                    }
                }
            } catch (Exception ex) {
                Serilog.Log.Warning(ex, $"Failed to read file header for {filepath}. Attempting universal fallbacks.");
            }

            // 1. WAV / RIFF Container
            if (tag == "RIFF" || ext == ".wav" || ext == ".wave") {
                try {
                    return new WaveFileReader(filepath);
                } catch (Exception ex) {
                    Serilog.Log.Debug(ex, $"Standard WaveFileReader failed for {filepath}. Attempting fallback decoders.");
                }
            }

            // 2. MP3 Container
            if (ext == ".mp3" || tag.StartsWith("ID3") || (bytesRead >= 2 && buffer[0] == 0xFF && (buffer[1] & 0xE0) == 0xE0)) {
                if (OverrideMp3Reader != null) {
                    try {
                        return OverrideMp3Reader(filepath);
                    } catch (Exception ex) {
                        Serilog.Log.Debug(ex, $"OverrideMp3Reader failed for {filepath}.");
                    }
                }
                try {
                    return new Mp3FileReaderBase(filepath, wf => new Mp3FrameDecompressor(wf));
                } catch (Exception ex) {
                    Serilog.Log.Debug(ex, $"Mp3FileReaderBase failed for {filepath}. Attempting fallback decoders.");
                }
            }

            // 3. OGG Vorbis / Opus Container
            if (tag == "OggS" || ext == ".ogg" || ext == ".opus") {
                string text = bytesRead > 0 ? System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead) : "";
                if (text.Contains("vorbis") || ext == ".ogg") {
                    try {
                        return new VorbisWaveReader(filepath);
                    } catch (Exception ex) {
                        Serilog.Log.Debug(ex, $"VorbisWaveReader failed for {filepath}.");
                    }
                }
                if (text.Contains("OpusHead") || ext == ".opus") {
                    try {
                        return new OpusOggWaveReader(filepath);
                    } catch (Exception ex) {
                        Serilog.Log.Debug(ex, $"OpusOggWaveReader failed for {filepath}.");
                    }
                }
            }

            // 4. FLAC
            if (tag == "fLaC" || ext == ".flac") {
                try {
                    return new FlacReader(filepath);
                } catch (Exception ex) {
                    Serilog.Log.Debug(ex, $"FlacReader failed for {filepath}.");
                }
            }

            // 5. AIFF / AIF
            if (ext == ".aiff" || ext == ".aif" || ext == ".aifc" || tag == "FORM") {
                try {
                    return new AiffFileReader(filepath);
                } catch (Exception ex) {
                    Serilog.Log.Debug(ex, $"AiffFileReader failed for {filepath}.");
                }
            }

            // 6. M4A / AAC / MP4 Container
            string ftyp = bytesRead >= 8 ? System.Text.Encoding.ASCII.GetString(buffer.AsSpan(4, 4)) : "";
            if (ftyp == "ftyp" || ext == ".m4a" || ext == ".mp4" || ext == ".aac") {
#if WINDOWS
                try {
                    return new MediaFoundationReader(filepath);
                } catch (Exception ex) {
                    Serilog.Log.Debug(ex, $"MediaFoundationReader failed for {filepath}.");
                }
#else
                try {
                    return new AACWaveReader(filepath);
                } catch (Exception ex) {
                    Serilog.Log.Debug(ex, $"AACWaveReader failed for {filepath}.");
                }
#endif
            }

            // 7. Universal Fallback: MediaFoundation (Windows) & AudioFileReader (Cross-platform)
#if WINDOWS
            try {
                return new MediaFoundationReader(filepath);
            } catch (Exception ex) {
                Serilog.Log.Debug(ex, $"Universal MediaFoundationReader fallback failed for {filepath}.");
            }
#endif
            try {
                return new AudioFileReader(filepath);
            } catch (Exception ex) {
                Serilog.Log.Debug(ex, $"Universal AudioFileReader fallback failed for {filepath}.");
            }

            // 8. Graceful Silence Fallback: Prevents entire render engine crash on corrupted or partial audio
            Serilog.Log.Warning($"Unable to decode audio format for file '{filepath}'. Returning silence fallback to prevent render crash.");
            return new RawSourceWaveStream(new MemoryStream(new byte[8820]), new WaveFormat(44100, 16, 1));
        }

        public static float[] GetStereoSamples(WaveStream waveStream) {
            ISampleProvider provider = waveStream.ToSampleProvider();
            if (provider.WaveFormat.SampleRate != 44100) {
                provider = new WdlResamplingSampleProvider(provider, 44100);
            }
            if (provider.WaveFormat.Channels > 2) {
                provider = provider.ToStereo();
            }
            return GetSamples(provider);
        }

        /// <summary>Writes a 44.1 kHz mono float buffer as a 16-bit WAV — the renderer cache file format.</summary>
        public static void WriteMono16Wav(string path, float[] samples) {
            using var writer = new WaveFileWriter(path, new WaveFormat(44100, 16, 1));
            var pcm = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; ++i) {
                float v = samples[i];
                if (v > 1f) {
                    v = 1f;
                }
                if (v < -1f) {
                    v = -1f;
                }
                var s = (short)(v * short.MaxValue);
                pcm[i * 2] = (byte)(s & 0xFF);
                pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
            }
            writer.Write(pcm, 0, pcm.Length);
        }

        public static float[] GetSamples(ISampleProvider sampleProvider) {
            if (sampleProvider.WaveFormat.SampleRate != 44100) {
                sampleProvider = new WdlResamplingSampleProvider(sampleProvider, 44100);
            }
            List<float> samples = new List<float>();
            float[] buffer = new float[44100];
            int n;
            while ((n = sampleProvider.Read(buffer, 0, buffer.Length)) > 0) {
                samples.AddRange(buffer.Take(n));
            }
            return samples.ToArray();
        }

        public static DiscreteSignal GetSignal(ISampleProvider sampleProvider) {
            List<float> samples = new List<float>();
            float[] buffer = new float[sampleProvider.WaveFormat.SampleRate];
            int n;
            while ((n = sampleProvider.Read(buffer, 0, buffer.Length)) > 0) {
                samples.AddRange(buffer.Take(n));
            }
            return new DiscreteSignal(sampleProvider.WaveFormat.SampleRate, samples.ToArray());
        }

        public static float[] BuildPeaks(WaveStream stream, IProgress<int> progress) {
            const double peaksRate = 4000;
            float[] peaks;
            int channels = stream.WaveFormat.Channels;
            double peaksSamples = (int)((double)stream.Length / stream.WaveFormat.BlockAlign / stream.WaveFormat.SampleRate * peaksRate);
            peaks = new float[(int)(peaksSamples + 1) * channels];
            double blocksPerPixel = stream.Length / stream.WaveFormat.BlockAlign / peaksSamples;

            var sampleProvider = stream.ToSampleProvider();

            float[] buffer = new float[128 * 1024];

            int readPos = 0;
            int peaksPos = 0;
            double bufferPos = 0;
            float lmax = 0, lmin = 0, rmax = 0, rmin = 0;
            int lastProgress = 0;
            int n;
            while ((n = sampleProvider.Read(buffer, 0, buffer.Length)) != 0) {
                readPos += n;
                for (int i = 0; i < n; i += channels) {
                    lmax = Math.Max(lmax, buffer[i]);
                    lmin = Math.Min(lmin, buffer[i]);
                    if (channels > 1) {
                        rmax = Math.Max(rmax, buffer[i + 1]);
                        rmin = Math.Min(rmin, buffer[i + 1]);
                    }
                    if (i > bufferPos) {
                        lmax = -lmax; lmin = -lmin; rmax = -rmax; rmin = -rmin; // negate peaks to flipped waveform
                        peaks[peaksPos * channels] = lmax == 0 ? lmin : lmin == 0 ? lmax : (lmin + lmax) / 2;
                        peaks[peaksPos * channels + 1] = rmax == 0 ? rmin : rmin == 0 ? rmax : (rmin + rmax) / 2;
                        peaksPos++;
                        lmax = lmin = rmax = rmin = 0;
                        bufferPos += blocksPerPixel * stream.WaveFormat.Channels;
                    }
                }
                bufferPos -= n;
                int newProgress = (int)((double)readPos * sizeof(float) * 100 / stream.Length);
                if (newProgress != lastProgress) {
                    progress.Report(newProgress);
                }
            }
            return peaks;
        }

        public static void CorrectSampleScale(float[] samples) {
            float max = samples.Max();
            float scale = 1;
            if (max > Math.Pow(2, 23)) {
                scale = (float)Math.Pow(0.5, 31); // 32 bit
            } else if (max > 8) {
                scale = (float)Math.Pow(0.5, 15); // 16 bit
            }
            for (int i = 0; i < samples.Length; i++) {
                samples[i] *= scale;
            }
        }
    }
}
