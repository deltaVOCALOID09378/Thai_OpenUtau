// ============================================================================
// Date         : 10/11/2026 (B.E. 2569 / A.D. 2026)
// File Name    : DocManager.cs
// Version      : 1.4
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.Core/DocManager.cs
// Description  : Central document, command, plugin, and undo/redo manager with fast plugin
//                assembly deduplication by AssemblyName.Name and thread-safe OtoChangedNotification filtering.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Lib;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core {
    public struct ValidateOptions {
        public bool SkipTiming;
        public UPart Part;
        public bool SkipPhonemizer;
        public bool SkipPhoneme;
    }

    public class DocManager : SingletonBase<DocManager> {
        DocManager() {
            Project = new UProject();
        }

        private Thread mainThread;
        private TaskScheduler mainScheduler;

        public int playPosTick = 0;
        public int rangeStartTick = 0;
        public int rangeEndTick = 0;

        public TaskScheduler MainScheduler => mainScheduler;
        public Action<Action> PostOnUIThread { get; set; }
        public OpenUtau.Classic.Plugin[] Plugins { get; private set; }
        public PhonemizerFactory[] PhonemizerFactories { get; private set; }
        public UProject Project { get; private set; }
        public bool HasOpenUndoGroup => undoGroup != null;
        public OpenUtau.Core.Integrations.GeminiSparkClient GeminiSpark { get; private set; }
        public List<UPart>? PartsClipboard { get; set; }
        public List<UNote>? NotesClipboard { get; set; }
        public CurveSelection? CurvesClipboard { get; set; }
        internal PhonemizerRunner PhonemizerRunner { get; private set; }
        public List<Type> ExternalBatchEditTypes { get; private set; } = new List<Type>();

        public void Initialize(Thread mainThread, TaskScheduler mainScheduler) {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((sender, args) => {
                CrashSave();
            });
            SearchAllPlugins();
            SearchAllLegacyPlugins();
            this.mainThread = mainThread;
            this.mainScheduler = mainScheduler;
            PhonemizerRunner = new PhonemizerRunner(mainScheduler);
            RealTimePitchGenerationService.Inst.Initialize();
            GeminiSpark = new OpenUtau.Core.Integrations.GeminiSparkClient(Util.Preferences.Default.GeminiSparkApiKey);
        }

        public void SearchAllLegacyPlugins() {
            try {
                var stopWatch = Stopwatch.StartNew();
                var pluginList = new List<OpenUtau.Classic.Plugin>();
                if (Directory.Exists(PathManager.Inst.PluginsPath)) {
                    pluginList.AddRange(PluginLoader.LoadAll(PathManager.Inst.PluginsPath));
                }
                string exePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? string.Empty;
                if (!string.IsNullOrEmpty(exePath)) {
                    string exePlugins = Path.Combine(exePath, "Plugins");
                    if (Directory.Exists(exePlugins) && !string.Equals(exePlugins, PathManager.Inst.PluginsPath, StringComparison.OrdinalIgnoreCase)) {
                        pluginList.AddRange(PluginLoader.LoadAll(exePlugins));
                    }
                }
                Plugins = pluginList
                    .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToArray();
                stopWatch.Stop();
                Log.Information($"Search all legacy plugins: {stopWatch.Elapsed} (Found {Plugins.Length})");
            } catch (Exception e) {
                Log.Error(e, "Failed to search legacy plugins.");
                Plugins = new OpenUtau.Classic.Plugin[0];
            }
        }

        private static readonly HashSet<string> KnownNativeOrCoreDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "OpenUtau.Plugin.Builtin.dll",
            "OpenUtau.Core.dll",
            "OpenUtau.dll",
            "worldline.dll",
            "libworldline.dll",
            "DirectML.dll",
            "SDL3.dll",
            "onnxruntime.dll",
            "onnxruntime_providers_shared.dll",
            "onnxruntime_providers_cuda.dll",
            "onnxruntime_providers_dml.dll",
            "onnxruntime_providers_tensorrt.dll",
            "Melanchall_DryWetMidi_Native32.dll",
            "Melanchall_DryWetMidi_Native64.dll",
            "libSkiaSharp.dll",
            "libHarfBuzzSharp.dll",
            "av_libglesv2.dll",
            "d3dcompiler_47.dll"
        };

        public void SearchAllPlugins() {
            const string kBuiltin = "OpenUtau.Plugin.Builtin.dll";
            var stopWatch = Stopwatch.StartNew();
            var files = new List<string>();
            try {
                string builtinPath = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory) ?? AppContext.BaseDirectory, kBuiltin);
                if (File.Exists(builtinPath)) {
                    files.Add(builtinPath);
                }
                Directory.CreateDirectory(PathManager.Inst.PluginsPath);
                string oldBuiltin = Path.Combine(PathManager.Inst.PluginsPath, kBuiltin);
                if (File.Exists(oldBuiltin)) {
                    try { File.Delete(oldBuiltin); } catch { }
                }
                files.AddRange(Directory.EnumerateFiles(
                    PathManager.Inst.PluginsPath,
                    "*.dll",
                    new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true, MaxRecursionDepth = 2 })
                    .Where(f => !KnownNativeOrCoreDlls.Contains(Path.GetFileName(f))));
            } catch (Exception e) {
                Log.Error(e, "Failed to search plugins.");
            }

            var managedCandidates = files
                .AsParallel()
                .AsOrdered()
                .Select(file => {
                    try {
                        if (!LibraryLoader.IsManagedAssembly(file)) {
                            Log.Information("Skipping unmanaged plugin candidate {File}.", file);
                            return default;
                        }
                        var assemblyName = AssemblyName.GetAssemblyName(file);
                        return (
                            valid: true,
                            file,
                            assemblyName,
                            identity: assemblyName.FullName ?? assemblyName.Name ?? file);
                    } catch (Exception e) {
                        Log.Warning(e, "Failed to inspect plugin assembly {File}.", file);
                        return default;
                    }
                })
                .Where(x => x.valid)
                .Select(x => (x.file, x.assemblyName, x.identity))
                .ToList();

            var assembliesToLoad = new List<(string file, AssemblyName assemblyName)>();
            var allLoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var group in managedCandidates.GroupBy(
                candidate => candidate.assemblyName.Name ?? candidate.identity,
                StringComparer.OrdinalIgnoreCase)) {
                var first = group.First();
                var loadedAssembly = allLoadedAssemblies
                    .FirstOrDefault(assembly =>
                        string.Equals(assembly.GetName().Name, first.assemblyName.Name, StringComparison.OrdinalIgnoreCase) ||
                        AssemblyName.ReferenceMatchesDefinition(
                            assembly.GetName(),
                            first.assemblyName));
                if (loadedAssembly != null) {
                    var builtIn = group.FirstOrDefault(candidate =>
                        string.Equals(Path.GetFileName(candidate.file), kBuiltin, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(builtIn.file)) {
                        assembliesToLoad.Add((builtIn.file, builtIn.assemblyName));
                        foreach (var duplicate in group.Where(candidate => candidate.file != builtIn.file)) {
                            Log.Debug(
                                "Skipping redundant builtin plugin copy {AssemblyName} at {File}.",
                                duplicate.identity,
                                duplicate.file);
                        }
                    } else {
                        foreach (var candidate in group) {
                            Log.Information(
                                "Skipping plugin assembly {AssemblyName} at {File} because it is already loaded.",
                                candidate.identity,
                                candidate.file);
                        }
                    }
                    continue;
                }

                (string file, AssemblyName assemblyName, string identity)? selected = null;
                foreach (var candidate in group
                    .OrderByDescending(candidate => File.GetLastWriteTimeUtc(candidate.file))
                    .ThenBy(candidate => candidate.file, StringComparer.OrdinalIgnoreCase)) {
                    if (CanInspectPluginAssembly(candidate.file, out string failure)) {
                        selected = candidate;
                        break;
                    }
                    Log.Warning(
                        "Skipping incompatible plugin assembly {File}: {Failure}",
                        candidate.file,
                        failure);
                }
                if (selected.HasValue) {
                    assembliesToLoad.Add((selected.Value.file, selected.Value.assemblyName));
                    foreach (var duplicate in group.Where(candidate =>
                        candidate.file != selected.Value.file)) {
                        Log.Information(
                            "Skipping duplicate plugin assembly {AssemblyName} at {File}.",
                            duplicate.identity,
                            duplicate.file);
                    }
                }
            }

            var typesAndFiles = assembliesToLoad.AsParallel().SelectMany(item => {
                try {
                    var loadedAssembly = allLoadedAssemblies
                        .FirstOrDefault(assembly =>
                            AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), item.assemblyName));
                    var assembly = loadedAssembly
                        ?? System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(item.file);
                    return assembly.GetExportedTypes().Select(type => (type, file: item.file));
                } catch (Exception e) {
                    Log.Warning(e, "Failed to load plugin assembly {File}.", item.file);
                    return Array.Empty<(Type type, string file)>();
                }
            }).ToList();

            foreach (var item in typesAndFiles) {
                var type = item.type;
                var file = item.file;
                if (!type.IsAbstract && type.IsSubclassOf(typeof(Phonemizer))) {
                    try {
                        PhonemizerFactory.Get(type);
                    } catch (Exception e) {
                        Log.Warning(
                            e,
                            "Skipping incompatible phonemizer type {Type} from {File}.",
                            type.FullName,
                            file);
                        continue;
                    }
                }
                if (typeof(BatchEdit).IsAssignableFrom(type)
                    && !type.IsInterface
                    && !type.IsAbstract
                    && type.GetConstructor(Type.EmptyTypes) != null
                    && !ExternalBatchEditTypes.Contains(type)) {
                    ExternalBatchEditTypes.Add(type);
                }
            }
            foreach (var type in GetType().Assembly.GetExportedTypes()) {
                if (!type.IsAbstract && type.IsSubclassOf(typeof(Phonemizer))) {
                    PhonemizerFactory.Get(type);
                }
            }
            PhonemizerFactory.BuildList();
            stopWatch.Stop();
            Log.Information($"Search all plugins: {stopWatch.Elapsed}");
        }

        private static bool CanInspectPluginAssembly(string file, out string failure) {
            try {
                using var stream = File.OpenRead(file);
                using var peReader = new PEReader(stream);
                if (!peReader.HasMetadata) {
                    failure = "The file does not contain managed metadata.";
                    return false;
                }

                var metadata = peReader.GetMetadataReader();
                var supportedConstructorSizes = typeof(PhonemizerAttribute)
                    .GetConstructors()
                    .Select(constructor => constructor.GetParameters().Length)
                    .ToHashSet();
                foreach (var attributeHandle in metadata.CustomAttributes) {
                    var attribute = metadata.GetCustomAttribute(attributeHandle);
                    if (attribute.Constructor.Kind != HandleKind.MemberReference) {
                        continue;
                    }
                    var constructor = metadata.GetMemberReference(
                        (MemberReferenceHandle)attribute.Constructor);
                    if (!IsPhonemizerAttribute(metadata, constructor.Parent)) {
                        continue;
                    }

                    var signature = metadata.GetBlobReader(constructor.Signature);
                    var header = signature.ReadSignatureHeader();
                    if (header.IsGeneric) {
                        _ = signature.ReadCompressedInteger();
                    }
                    int parameterCount = signature.ReadCompressedInteger();
                    if (!supportedConstructorSizes.Contains(parameterCount)) {
                        failure =
                            $"PhonemizerAttribute constructor has {parameterCount} parameters, " +
                            $"but this OpenUtau build supports " +
                            $"{string.Join(", ", supportedConstructorSizes.OrderBy(count => count))}.";
                        return false;
                    }
                }
                failure = string.Empty;
                return true;
            } catch (Exception e) {
                failure = e.GetBaseException().Message;
                return false;
            }
        }

        private static bool IsPhonemizerAttribute(
            MetadataReader metadata,
            EntityHandle typeHandle) {
            const string attributeNamespace = "OpenUtau.Api";
            const string attributeName = nameof(PhonemizerAttribute);
            return typeHandle.Kind switch {
                HandleKind.TypeReference => IsMatchingType(
                    metadata.GetTypeReference((TypeReferenceHandle)typeHandle)),
                HandleKind.TypeDefinition => IsMatchingDefinition(
                    metadata.GetTypeDefinition((TypeDefinitionHandle)typeHandle)),
                _ => false,
            };

            bool IsMatchingType(TypeReference type) =>
                metadata.GetString(type.Namespace) == attributeNamespace
                && metadata.GetString(type.Name) == attributeName;

            bool IsMatchingDefinition(TypeDefinition type) =>
                metadata.GetString(type.Namespace) == attributeNamespace
                && metadata.GetString(type.Name) == attributeName;
        }

        #region Command Queue

        readonly Deque<UCommandGroup> undoQueue = new Deque<UCommandGroup>();
        readonly Deque<UCommandGroup> redoQueue = new Deque<UCommandGroup>();
        UCommandGroup? undoGroup = null;
        UCommandGroup? savedPoint = null;
        UCommandGroup? autosavedPoint = null;
        public bool Recovered { get; set; } = false; // Flag to not overwrite backup file

        public bool ChangesSaved {
            get {
                return (Project.Saved || (Project.tracks.Count <= 1 && Project.parts.Count == 0)) &&
                    (undoQueue.Count > 0 && savedPoint == undoQueue.Last() || undoQueue.Count == 0 && savedPoint == null) &&
                    !Recovered;
            }
        }


        private void CrashSave() {
            try {
                if (Project == null) {
                    Log.Warning("Crash save project is null.");
                    return;
                }
                bool untitled = string.IsNullOrEmpty(Project.FilePath);
                if (untitled) {
                    Directory.CreateDirectory(PathManager.Inst.BackupsPath);
                }
                string dir = untitled
                    ? PathManager.Inst.BackupsPath
                    : Path.GetDirectoryName(Project.FilePath);
                string filename = untitled
                    ? "Untitled"
                    : Path.GetFileNameWithoutExtension(Project.FilePath);
                string backup = Path.Join(dir, filename + "-backup.ustx");
                Log.Information($"Saving backup {backup}.");
                Format.Ustx.AutoSave(backup, Project);
                Log.Information($"Saved backup {backup}.");
            } catch (Exception e) {
                Log.Error(e, "Save backup failed.");
            }
        }

        public void AutoSave() {
            if (Project == null) {
                return;
            }
            if (undoQueue.LastOrDefault() == autosavedPoint) {
                Log.Information("Autosave skipped.");
                return;
            }
            try {
                bool untitled = string.IsNullOrEmpty(Project.FilePath);
                if (untitled) {
                    Directory.CreateDirectory(PathManager.Inst.BackupsPath);
                }
                string dir = untitled
                    ? PathManager.Inst.BackupsPath
                    : Path.GetDirectoryName(Project.FilePath);
                string filename = untitled
                    ? "Untitled"
                    : Path.GetFileNameWithoutExtension(Project.FilePath);

                string backup = Path.Join(dir, filename + "-autosave.ustx");
                Log.Information($"Autosave {backup}.");
                Format.Ustx.AutoSave(backup, Project);
                Log.Information($"Autosaved {backup}.");
                autosavedPoint = undoQueue.LastOrDefault();
            } catch (Exception e) {
                Log.Error(e, "Autosave failed.");
            }
        }

        public void ExecuteCmd(UCommand cmd) {
            if (mainThread != null && mainThread != Thread.CurrentThread) {
                if (!(cmd is ProgressBarNotification)
                    && !(cmd is WaveformReadyNotification)
                    && !(cmd is PhraseRenderedNotification)
                    && !(cmd is PartRenderedNotification)
                    && !(cmd is OtoChangedNotification)) {
                    Log.Warning($"{cmd} not on main thread");
                }
                if (PostOnUIThread != null) {
                    PostOnUIThread(() => ExecuteCmd(cmd));
                    return;
                }
            }
            if (cmd is UNotification) {
                if (cmd is SaveProjectNotification saveProjectNotif) {
                    if (undoQueue.Count > 0) {
                        savedPoint = undoQueue.Last();
                    }
                    if (string.IsNullOrEmpty(saveProjectNotif.Path)) {
                        Format.Ustx.Save(Project.FilePath, Project);
                    } else {
                        Format.Ustx.Save(saveProjectNotif.Path, Project);
                    }
                } else if (cmd is LoadProjectNotification notification) {
                    undoQueue.Clear();
                    redoQueue.Clear();
                    undoGroup = null;
                    savedPoint = null;
                    autosavedPoint = null;
                    Project = notification.project;
                    playPosTick = 0;
                    rangeStartTick = 0;
                    rangeEndTick = 0;
                } else if (cmd is SetPlayPosTickNotification setPlayPosTickNotif) {
                    playPosTick = setPlayPosTickNotif.playPosTick;
                } else if (cmd is SetRangeSelectionNotification setRange) {
                    rangeStartTick = setRange.startTick;
                    rangeEndTick = setRange.endTick;
                } else if (cmd is RealCurvesUpdatedNotification realCurvesNotif) {
                    if (realCurvesNotif.part is UVoicePart voicePart) {
                        RealCurveUpdater.Apply(Project, voicePart, realCurvesNotif.updates);
                    }
                } else if (cmd is SingersChangedNotification) {
                    SingerManager.Inst.SearchAllSingers();
                } else if (cmd is ValidateProjectNotification) {
                    Project.ValidateFull();
                } else if (cmd is SingersRefreshedNotification || cmd is OtoChangedNotification) {
                    foreach (var track in Project.tracks) {
                        track.OnSingerRefreshed();
                    }
                    Project.ValidateFull();
                    if (cmd is OtoChangedNotification) {
                        ExecuteCmd(new PreRenderNotification());
                    }
                }
                Publish(cmd);
                if (!cmd.Silent) {
                    if (cmd is PartRenderedNotification
                        || cmd is WaveformReadyNotification
                        || cmd is PhraseRenderedNotification
                        || cmd is ProgressBarNotification
                        || cmd is SetPlayPosTickNotification
                        || cmd is RealCurvesUpdatedNotification) {
                        Log.Debug($"Publish notification {cmd}");
                    } else {
                        Log.Information($"Publish notification {cmd}");
                    }
                }
                return;
            }
            if (undoGroup == null) {
                Log.Error($"No active UndoGroup {cmd}");
                return;
            }
            undoGroup.Commands.Add(cmd);
            lock (Project) {
                cmd.Execute();
            }
            if (!cmd.Silent) {
                Log.Information($"ExecuteCmd {cmd}");
            }
            Publish(cmd);
            if (!undoGroup.DeferValidate) {
                Project.Validate(cmd.ValidateOptions);
                ScheduleRealCurveRefresh(cmd);
            }
        }

        void ScheduleRealCurveRefresh(UCommand cmd) {
            if (cmd is not ExpCommand expCommand) {
                return;
            }
            var part = expCommand.Part;
            if (!Project.parts.Contains(part) ||
                part.trackNo < 0 ||
                part.trackNo >= Project.tracks.Count) {
                return;
            }
            Project.tracks[part.trackNo].RendererSettings.Renderer
                ?.ScheduleRealCurveRefresh(Project, part, cmd);
        }

        void ScheduleRealCurveRefresh(IEnumerable<UCommand> commands) {
            foreach (var cmd in commands) {
                ScheduleRealCurveRefresh(cmd);
            }
        }

        public void StartUndoGroup(string? nameKey = null, bool deferValidate = false) {
            if (undoGroup != null) {
                Log.Error("undoGroup already started");
                EndUndoGroup();
            }
            undoGroup = new UCommandGroup(nameKey, deferValidate);
            Log.Information("undoGroup started");
        }

        public void EndUndoGroup() {
            if (undoGroup == null) {
                Log.Error("No active undoGroup to end.");
                return;
            }
            if (undoGroup.Commands.Count > 0) {
                undoQueue.AddToBack(undoGroup);
                redoQueue.Clear();
            }
            while (undoQueue.Count > Util.Preferences.Default.UndoLimit) {
                undoQueue.RemoveFromFront();
            }
            if (undoGroup.DeferValidate) {
                Project.ValidateFull();
            }
            undoGroup.Merge();
            ScheduleRealCurveRefresh(undoGroup.Commands);
            undoGroup = null;
            Log.Information("undoGroup ended");
            ExecuteCmd(new PreRenderNotification());
        }

        public void RollBackUndoGroup() {
            if (undoGroup == null) {
                // If there is no active undoGroup, quietly return without error spam
                return;
            }
            for (int i = undoGroup.Commands.Count - 1; i >= 0; i--) {
                var cmd = undoGroup.Commands[i];
                cmd.Unexecute();
                if (i == 0) {
                    Project.ValidateFull();
                }
                Publish(cmd, true);
            }
            undoGroup.Commands.Clear();
        }

        public void Undo() {
            if (undoQueue.Count == 0) {
                return;
            }
            var group = undoQueue.RemoveFromBack();
            for (int i = group.Commands.Count - 1; i >= 0; i--) {
                var cmd = group.Commands[i];
                cmd.Unexecute();
                if (i == 0) {
                    Project.ValidateFull();
                }
                Publish(cmd, true);
            }
            redoQueue.AddToBack(group);
            ScheduleRealCurveRefresh(group.Commands);
            ExecuteCmd(new PreRenderNotification());
        }

        public void Redo() {
            if (redoQueue.Count == 0) {
                return;
            }
            var group = redoQueue.RemoveFromBack();
            for (var i = 0; i < group.Commands.Count; i++) {
                var cmd = group.Commands[i];
                cmd.Execute();
                if (i == group.Commands.Count - 1) {
                    Project.ValidateFull();
                }
                Publish(cmd);
            }
            undoQueue.AddToBack(group);
            ScheduleRealCurveRefresh(group.Commands);
            ExecuteCmd(new PreRenderNotification());
        }

        public bool GetUndoState(out string? key) {
            key = null;
            if (undoQueue.Count > 0) {
                key = undoQueue.Last().NameKey;
                return true;
            } else {
                return false;
            }
        }
        public bool GetRedoState(out string? key) {
            key = null;
            if (redoQueue.Count > 0) {
                key = redoQueue.Last().NameKey;
                return true;
            } else {
                return false;
            }
        }

        # endregion

        # region Command Subscribers

        private readonly object lockObj = new object();
        private readonly List<ICmdSubscriber> subscribers = new List<ICmdSubscriber>();

        public void AddSubscriber(ICmdSubscriber sub) {
            lock (lockObj) {
                if (!subscribers.Contains(sub)) {
                    subscribers.Add(sub);
                }
            }
        }

        public void RemoveSubscriber(ICmdSubscriber sub) {
            lock (lockObj) {
                if (subscribers.Contains(sub)) {
                    subscribers.Remove(sub);
                }
            }
        }

        private void Publish(UCommand cmd, bool isUndo = false) {
            lock (lockObj) {
                foreach (var sub in subscribers) {
                    sub.OnNext(cmd, isUndo);
                }
            }
        }

        /// <summary>
        /// Apply commands without recording undo. Still notifies subscribers.
        /// </summary>
        public void ApplyTransient(IEnumerable<UCommand> commands, ValidateOptions? validateOptions = null, bool preRender = true) {
            if (mainThread != Thread.CurrentThread) {
                PostOnUIThread(() => ApplyTransient(commands, validateOptions, preRender));
                return;
            }
            RealTimePitchGenerationService.SuppressCallbacks = true;
            try {
                foreach (var cmd in commands) {
                    lock (Project) {
                        cmd.Execute();
                    }
                    Publish(cmd);
                }
                if (validateOptions != null) {
                    Project.Validate(validateOptions.Value);
                    if (preRender) {
                        ExecuteCmd(new PreRenderNotification());
                    }
                }
            } finally {
                RealTimePitchGenerationService.SuppressCallbacks = false;
            }
        }

        #endregion
    }
}
