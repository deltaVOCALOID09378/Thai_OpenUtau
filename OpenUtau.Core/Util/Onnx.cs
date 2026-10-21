// ============================================================================
// Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : Onnx.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Contributors ]
// File Location: OpenUtau.Core/Util/Onnx.cs
// Description  : ONNX Runtime session manager with DirectML hardware enumeration,
//                ORT_ENABLE_ALL graph optimization, and balanced CPU thread allocation.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core {
    public class GpuInfo {
        public int deviceId;
        public string description = "";

        override public string ToString() {
            return $"[{deviceId}] {description}";
        }
    }

    public enum OnnxRunnerChoice {
        Default,
        CPU,
        CPUForCoreML,
    }

    public class Onnx {
        private static readonly Dictionary<int, OrtEpDevice> devices = initializeDevices();

        private static Dictionary<int, OrtEpDevice> initializeDevices() {
            try {
                var env = OrtEnv.Instance();
                var ortDevices = env.GetEpDevices();

                return ortDevices
                    .Where(device => device.EpName.ToLower().Contains("dml"))
                    .Select((device, index) => new { index, device })
                    .ToDictionary(x => x.index, x => x.device);
            } catch (Exception ex) {
                Log.Warning(ex, "Failed to initialize DirectML EP devices list at startup.");
                return new Dictionary<int, OrtEpDevice>();
            }
        }

        public static List<string> getRunnerOptions() {
            if (OS.IsWindows()) {
                return new List<string> {
                    "CPU",
                    "DirectML"
                };
            } else if (OS.IsMacOS()) {
                return new List<string> {
                    "CPU",
                    "CoreML"
                };
            } else if (OS.IsAndroid()) {
                return new List<string> {
                    "CPU",
                    "NNAPI"
                };
            }
            return new List<string> {
                "CPU"
            };
        }

        public static List<GpuInfo> getGpuInfo() {
            if (OS.IsAndroid()) {
                return new List<GpuInfo>{new GpuInfo {
                    deviceId = 0, // eliminate exception of taking OnnxGpuOptions[0]
                }};
            }
            List<GpuInfo> gpuList = new List<GpuInfo>();
            try {
                var env = OrtEnv.Instance();
                var ortDevices = env.GetEpDevices();

                var i = 0;
                foreach (var device in ortDevices.Where(device => device.EpName.ToLower().Contains("dml"))) {
                    var description = "";
                    foreach (var item in device.HardwareDevice.Metadata.Entries) {
                        if (item.Key.ToLower() == "description") {
                            description = $"{item.Value} ({device.HardwareDevice.Type})";
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(description)) { // fallback
                        description = $"{device.EpName} {device.HardwareDevice.Vendor} ({device.HardwareDevice.Type})";
                    }
                    devices[i] = device;
                    gpuList.Add(new GpuInfo {
                        deviceId = i++,
                        description = description
                    });
                }
            } catch (Exception ex) {
                Log.Warning(ex, "Failed to enumerate DirectML devices via OrtEnv.");
            }
            if (gpuList.Count == 0) {
                gpuList.Add(new GpuInfo {
                    deviceId = 0,
                    description = "DirectML GPU 0 (Default)"
                });
            }
            return gpuList;
        }

        private static SessionOptions getOnnxSessionOptions(bool coremlEnableOnSubgraphs = false) {
            SessionOptions options = new SessionOptions();
            
            // DELTA SYNTH Optimization: Enhance CPU & DirectML performance
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
            int coreCount = Math.Max(1, Environment.ProcessorCount / 2);
            options.InterOpNumThreads = coreCount;
            options.IntraOpNumThreads = coreCount;

            List<string> runnerOptions = getRunnerOptions();
            string runner = Preferences.Default.OnnxRunner;
            if (String.IsNullOrEmpty(runner)) {
                runner = runnerOptions[0];
            }
            if (!runnerOptions.Contains(runner)) {
                runner = "CPU";
            }
            switch (runner) {
                case "DirectML":
                    int gpuId = Math.Max(0, Preferences.Default.OnnxGpu);
                    bool attached = false;
                    if (devices.TryGetValue(gpuId, out var d)) {
                        try {
                            options.AppendExecutionProvider(
                                OrtEnv.Instance(),
                                new List<OrtEpDevice> { d },
                                new Dictionary<string, string> { }
                            );
                            attached = true;
                        } catch (Exception ex) {
                            Log.Warning(ex, $"Failed to append DirectML provider via OrtEpDevice for GPU {gpuId}, attempting AppendExecutionProvider_DML.");
                        }
                    }
                    if (!attached) {
                        try {
                            options.AppendExecutionProvider_DML(gpuId);
                            attached = true;
                        } catch (Exception ex) {
                            Log.Warning(ex, $"Failed to append DirectML provider via AppendExecutionProvider_DML for GPU {gpuId}.");
                        }
                    }
                    break;
                case "CoreML":
                    options.AppendExecutionProvider("CoreML", new Dictionary<string, string> {
                        { "MLComputeUnits", "ALL" },
                        { "RequireStaticInputShapes", "1"},
                        { "ModelFormat", "NeuralNetwork"},
                        { "EnableOnSubgraphs", coremlEnableOnSubgraphs ? "1" : "0" }
                    });
                    break;
                case "NNAPI":
                    options.AppendExecutionProvider_Nnapi();
                    break;
            }
            return options;
        }

        public static InferenceSession getInferenceSession(byte[] model, OnnxRunnerChoice runnerChoice = OnnxRunnerChoice.Default) {
            if (runnerChoice == OnnxRunnerChoice.CPU ||
                (runnerChoice == OnnxRunnerChoice.CPUForCoreML && Preferences.Default.OnnxRunner == "CoreML")) {
                return new InferenceSession(model);
            }

            if (OS.IsWindows() && Preferences.Default.OnnxRunner == "DirectML") {
                try {
                    using var options = getOnnxSessionOptions();
                    return new InferenceSession(model, options);
                } catch (Exception ex) {
                    Log.Warning(ex, "Failed to create InferenceSession with DirectML execution provider. Falling back to CPU.");
                    return new InferenceSession(model);
                }
            }

            if (OS.IsMacOS() && Preferences.Default.OnnxRunner == "CoreML") {
                try {
                    using var options = getOnnxSessionOptions(coremlEnableOnSubgraphs: true);
                    return new InferenceSession(model, options);
                } catch (Exception e) {
                    Log.Warning(e, "Failed to create session with CoreML subgraphs enabled, falling back to default settings");
                }
            }

            try {
                using var options = getOnnxSessionOptions();
                return new InferenceSession(model, options);
            } catch (Exception ex) {
                Log.Warning(ex, $"Failed to create InferenceSession with runner {Preferences.Default.OnnxRunner}. Falling back to CPU.");
                return new InferenceSession(model);
            }
        }

        public static InferenceSession getInferenceSession(string modelPath, OnnxRunnerChoice runnerChoice = OnnxRunnerChoice.Default) {
            if (runnerChoice == OnnxRunnerChoice.CPU ||
                (runnerChoice == OnnxRunnerChoice.CPUForCoreML && Preferences.Default.OnnxRunner == "CoreML")) {
                return new InferenceSession(modelPath);
            }

            if (OS.IsWindows() && Preferences.Default.OnnxRunner == "DirectML") {
                try {
                    using var options = getOnnxSessionOptions();
                    return new InferenceSession(modelPath, options);
                } catch (Exception ex) {
                    Log.Warning(ex, $"Failed to create InferenceSession with DirectML execution provider for {modelPath}. Falling back to CPU.");
                    return new InferenceSession(modelPath);
                }
            }

            if (OS.IsMacOS() && Preferences.Default.OnnxRunner == "CoreML") {
                try {
                    using var options = getOnnxSessionOptions(coremlEnableOnSubgraphs: true);
                    return new InferenceSession(modelPath, options);
                } catch (Exception e) {
                    Log.Warning(e, "Failed to create session with CoreML subgraphs enabled, falling back to default settings");
                }
            }

            try {
                using var options = getOnnxSessionOptions();
                return new InferenceSession(modelPath, options);
            } catch (Exception ex) {
                Log.Warning(ex, $"Failed to create InferenceSession with runner {Preferences.Default.OnnxRunner} for {modelPath}. Falling back to CPU.");
                return new InferenceSession(modelPath);
            }
        }

        public static void VerifyInputNames(InferenceSession session, IEnumerable<NamedOnnxValue> inputs) {
            var sessionInputNames = session.InputNames.ToHashSet();
            var givenInputNames = inputs.Select(v => v.Name).ToHashSet();
            var missing = sessionInputNames
                .Except(givenInputNames)
                .OrderBy(s => s, StringComparer.InvariantCulture)
                .ToArray();
            if (missing.Length > 0) {
                throw new ArgumentException("Missing input(s) for the inference session: " + string.Join(", ", missing));
            }
            var unexpected = givenInputNames
                .Except(sessionInputNames)
                .OrderBy(s => s, StringComparer.InvariantCulture)
                .ToArray();
            if (unexpected.Length > 0) {
                throw new ArgumentException("Unexpected input(s) for the inference session: " + string.Join(", ", unexpected));
            }
        }
    }
}
