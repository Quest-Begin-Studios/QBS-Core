using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QBS.SourceGenerators;
using QBS.SourceGenerators.GeneratorDiscoveryHelpers;
using UnityEditor;
using UnityEngine;

namespace QBS.Core.Editor
{
    public partial class LogSourceCompiler : EditorWindow
    {
        private const string NamespaceStr = "QBS.Core";
        private const string LogChannelsName = "LogChannel";
        private const string LogChannelTypeName = NamespaceStr + "." + LogChannelsName;
        private const string AssemblyReferencesJson = "AssemblyReferences.json";
        private const string RawSourceFolderPath = @"Assets\com.qbs.core\RawSource~\LogSource";

        private EnumGeneratorComponent _enumGenerator;
        private Vector2 _scrollPosition;
        private Vector2 _enumScrollPosition;

        private List<SourceFile> _sourceFiles;
        private string _capturedEnumCode = "";
        private string _channelSourceSummary = "";
        private List<string> _runtimeAssemblyReferences;
        private List<string> _editorAssemblyReferences;
        private readonly List<string> _scriptingSymbols = new() { "ENABLE_LOGS" };

        private List<LogChannelSource> _channelSources;
        private List<LogChannelSource> _missingChannelSources;
        private readonly List<string> _channelLoadErrors = new();
        private readonly List<string> _channelErrors = new();
        private readonly Dictionary<LogChannelSource, string> _newChannelNames = new();
        private Dictionary<string, long> _generatedChannelValues;

        [MenuItem("Tools/QBS/Logs/Log Source Compiler")]
        public static void ShowWindow()
        {
            var window = GetWindow<LogSourceCompiler>("Log Source Compiler");
            window.minSize = new Vector2(600, 1050);
        }

        private void OnEnable()
        {
            _sourceFiles = new List<SourceFile>();
            _editorAssemblyReferences = new List<string>();
            _runtimeAssemblyReferences = new List<string>();
            InitializeAndConfigureEnumGenerator();
        }

        private void InitializeAndConfigureEnumGenerator()
        {
            _enumGenerator = new EnumGeneratorComponent();

            _enumGenerator.Initialize();
            _enumGenerator.ConfigureEnumProperties
            (
                LogChannelsName,
                NamespaceStr,
                EnumGeneratorComponent.EnumTypeOption.Flags,
                EnumGeneratorComponent.BackingType.Long
            );

            ReloadChannelSources();
        }

        /// <summary>
        ///     Reads every manifest from disk, discarding unsaved edits. When the project has no manifest of
        ///     its own but its compiled enum has channels no manifest declares, those are listed as a new
        ///     project manifest, so generating from manifests never drops a game's channels from under its
        ///     own call sites.
        /// </summary>
        private void ReloadChannelSources()
        {
            _channelLoadErrors.Clear();
            _newChannelNames.Clear();
            _channelSources = LogChannelResolver.FindSources(_channelLoadErrors);

            var carriedOver = CreateProjectSourceFromCompiled(_channelSources);
            if (carriedOver != null)
            {
                _channelSources.Add(carriedOver);
                _channelSourceSummary =
                    $"{carriedOver.Manifest.Channels.Count} channels compiled into this project are in no "
                    + $"{LogChannelManifest.FileName}. They are listed under {LogChannelResolver.ProjectOwner}, counting "
                    + $"down from bit {carriedOver.Manifest.StartBit}, and saved to {LogChannelResolver.ProjectManifestPath} "
                    + "when you generate. Saved channel masks follow them to their new bits.";
            }
            else
            {
                _channelSourceSummary =
                    $"Channels come from every {LogChannelManifest.FileName} in the project. Greyed-out ones belong to "
                    + "packages installed read-only. Channels are pinned to bits, so a list only grows: retire a "
                    + "channel rather than delete it.";
            }

            _missingChannelSources = LogChannelResolver.FindMissingEditableSources(_channelSources);
            ResolveChannels(new List<FlagSegment>(), new List<EnumGeneratorComponent.FlagCombinationEntry>());
        }

        private bool ResolveChannels(List<FlagSegment> segments, List<EnumGeneratorComponent.FlagCombinationEntry> combinations)
        {
            _channelErrors.Clear();
            _channelErrors.AddRange(_channelLoadErrors);
            return LogChannelResolver.Resolve(_channelSources, segments, combinations, _channelErrors)
                   && _channelLoadErrors.Count == 0;
        }

        private void SaveDirtyManifests()
        {
            var saved = false;
            foreach (var source in _channelSources)
            {
                if (source.IsDirty && source.IsEditable)
                {
                    LogChannelResolver.Save(source);
                    saved = true;
                }
            }

            if (saved)
            {
                AssetDatabase.Refresh();
            }
        }

        private void OnDisable()
        {
            _sourceFiles = null;
            _enumGenerator = null;

            _capturedEnumCode = "";
            _runtimeAssemblyReferences = null;
            _editorAssemblyReferences = null;

            _channelSources = null;
            _missingChannelSources = null;
            _generatedChannelValues = null;
        }

        private void OnGUI()
        {
            GUILayout.Label("Log Source Compiler", EditorStyles.largeLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
            var helpBoxStyle = new GUIStyle(EditorStyles.helpBox);
            helpBoxStyle.fontSize = 13;
            helpBoxStyle.padding = new RectOffset(10, 10, 10, 10);
            EditorGUILayout.LabelField
            (
                "This tool compiles log sources into a DLL.\n\n" +
                "Steps:\n" +
                "1. Review the LogChannels.json manifests below; edit the project's and any embedded package's\n" +
                "2. Click 'Generate Enum' to save them, lay the channels out on their bits and read sources from RawSource~ folder\n" +
                "3. Click 'Generate DLL' to compile the final DLL\n\n" +
                "Output: The compiled DLL will be placed in the Plugins folder.",
                helpBoxStyle
            );
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (!string.IsNullOrEmpty(_capturedEnumCode))
            {
                EditorGUILayout.Space(20);
                DrawDLLGenerationSection();
            }

            DrawEnumGenerationSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawDLLGenerationSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("DLL Generation", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Enum generated successfully! Click below to compile the DLL.\nScripting symbols: ENABLE_LOGS", MessageType.Info);
            EditorGUILayout.Space(10);

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("Generate DLL", GUILayout.Height(50)))
            {
                GUI.backgroundColor = originalColor;
                //Read before the new DLL replaces it: the masks are carried from this layout to the new one.
                var previousChannelValues = ReadCompiledChannelValues();
                var dllGenerationSuccess = DLLGenerationHelper.TryGeneratingDLL
                (
                    _sourceFiles,
                    "Log",
                    "Log",
                    _runtimeAssemblyReferences,
                    _editorAssemblyReferences,
                    _scriptingSymbols
                );

                if (dllGenerationSuccess)
                {
                    RemapSavedChannelMasks(previousChannelValues, _generatedChannelValues);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    EditorUtility.RequestScriptReload();
                }
            }

            GUI.backgroundColor = originalColor;
            EditorGUILayout.EndVertical();
        }

        private void DrawEnumGenerationSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Enum Generation", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"Create the {LogChannelsName} enum here.\n\n{_channelSourceSummary}", MessageType.Info);
            EditorGUILayout.Space();

            _enumScrollPosition = EditorGUILayout.BeginScrollView(_enumScrollPosition, GUILayout.Height(500));

            EditorGUI.BeginDisabledGroup(true);
            _enumGenerator.DrawConfigurationGUI();
            EditorGUI.EndDisabledGroup();

            DrawChannelSourcesGUI();

            EditorGUILayout.EndScrollView();

            if (_channelErrors.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Join("\n", _channelErrors), MessageType.Error);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Manifests"))
            {
                SaveDirtyManifests();
            }

            if (GUILayout.Button("Reload From Disk"))
            {
                ReloadChannelSources();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Generate Enum", GUILayout.Height(30)))
            {
                _sourceFiles.Clear();
                _runtimeAssemblyReferences.Clear();
                _editorAssemblyReferences.Clear();
                _capturedEnumCode = "";

                ReadSources(_sourceFiles, _runtimeAssemblyReferences, _editorAssemblyReferences);

                var segments = new List<FlagSegment>();
                var combinations = new List<EnumGeneratorComponent.FlagCombinationEntry>();

                //A clash stops here rather than move a channel something was compiled against.
                if (ResolveChannels(segments, combinations))
                {
                    SaveDirtyManifests();
                    _enumGenerator.ConfigureFlagSegments(segments);
                    _enumGenerator.ConfigureEnumKeys(flagCombinations: combinations);

                    var enumGenSuccess = _enumGenerator.GenerateEnum();

                    if (enumGenSuccess)
                    {
                        _capturedEnumCode = _enumGenerator.GeneratedCode;
                        _generatedChannelValues = LogChannelResolver.GetChannelValues(segments);
                        //Also generate the relevant ToStringNoBox methods:
                        GenerateEnumHelpers();
                    }
                }
            }

            _enumGenerator.DrawOutputGUI();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20);

            if (!string.IsNullOrEmpty(_capturedEnumCode))
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("Captured Enum for Processing", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox
                (
                    $"Enum code captured! You can now use this in other functions.\nLength: {_capturedEnumCode.Length} characters",
                    MessageType.Info
                );

                if (GUILayout.Button("Clear Captured Enum"))
                {
                    _capturedEnumCode = "";
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawChannelSourcesGUI()
        {
            GUILayout.Label("Log Channels", EditorStyles.boldLabel);

            var changed = false;
            foreach (var source in _channelSources)
            {
                changed |= DrawChannelSource(source);
            }

            LogChannelSource created = null;
            foreach (var missing in _missingChannelSources)
            {
                if (GUILayout.Button($"Add a {LogChannelManifest.FileName} to {missing.Owner}"))
                {
                    created = missing;
                }
            }

            if (created != null)
            {
                //Laid out now and never again: from here on the file itself says where its channels go.
                created.Manifest = LogChannelResolver.CreateManifest(created.IsPackage, _channelSources);
                created.IsDirty = true;
                _missingChannelSources.Remove(created);
                _channelSources.Add(created);
                changed = true;
            }

            if (changed)
            {
                ResolveChannels(new List<FlagSegment>(), new List<EnumGeneratorComponent.FlagCombinationEntry>());
            }
        }

        /// <summary>
        ///     One manifest: each channel beside its bit, then its combinations. Editable in place for the
        ///     project's and embedded or local packages' manifests, greyed out for the rest. Returns whether
        ///     anything changed.
        /// </summary>
        private bool DrawChannelSource(LogChannelSource source)
        {
            var manifest = source.Manifest;
            var hasDirection = LogChannelResolver.TryGetDirection(manifest, out var direction);
            var segment = new FlagSegment(manifest.StartBit, direction, manifest.Channels);
            var retiredIndex = -1;
            var removedCombinationIndex = -1;
            var changed = false;

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label(source.IsDirty ? $"{source.Owner} (unsaved)" : source.Owner, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(source.DisplayPath, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(DescribeLayout(source, hasDirection, direction), EditorStyles.miniLabel);

            EditorGUI.BeginDisabledGroup(!source.IsEditable);
            EditorGUI.BeginChangeCheck();

            for (var i = 0; i < manifest.Channels.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(hasDirection ? $"Bit {segment.BitAt(i)}" : "Bit ?", GUILayout.Width(60));

                if (string.IsNullOrWhiteSpace(manifest.Channels[i]))
                {
                    EditorGUILayout.LabelField("(retired)", EditorStyles.miniLabel);
                }
                else
                {
                    manifest.Channels[i] = EditorGUILayout.TextField(manifest.Channels[i]);
                    if (source.IsEditable && GUILayout.Button("Retire", GUILayout.Width(60)))
                    {
                        retiredIndex = i;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            if (manifest.Combinations.Count > 0)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Combinations", EditorStyles.miniBoldLabel);
            }

            for (var i = 0; i < manifest.Combinations.Count; i++)
            {
                var combination = manifest.Combinations[i];
                EditorGUILayout.BeginHorizontal();
                combination.Name = EditorGUILayout.TextField(combination.Name, GUILayout.Width(140));

                var flags = string.Join(", ", combination.Flags);
                var editedFlags = EditorGUILayout.TextField(flags);
                if (editedFlags != flags)
                {
                    //Split without dropping blanks, so the comma typed before the next name survives the redraw.
                    combination.Flags = editedFlags.Split(',').Select(flag => flag.Trim()).ToList();
                }

                if (source.IsEditable && GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    removedCombinationIndex = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            changed |= EditorGUI.EndChangeCheck();
            EditorGUI.EndDisabledGroup();

            if (source.IsEditable)
            {
                EditorGUILayout.BeginHorizontal();
                _newChannelNames.TryGetValue(source, out var newChannelName);
                newChannelName = EditorGUILayout.TextField(newChannelName ?? string.Empty);
                _newChannelNames[source] = newChannelName;

                if (GUILayout.Button("Add Channel", GUILayout.Width(100)) && !string.IsNullOrWhiteSpace(newChannelName))
                {
                    manifest.Channels.Add(newChannelName.Trim());
                    _newChannelNames[source] = string.Empty;
                    GUI.FocusControl(null);
                    changed = true;
                }

                if (GUILayout.Button("Add Combination", GUILayout.Width(120)))
                {
                    manifest.Combinations.Add(new EnumGeneratorComponent.FlagCombinationEntry());
                    changed = true;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (retiredIndex >= 0)
            {
                //Emptied rather than removed, so every channel after it keeps its bit.
                manifest.Channels[retiredIndex] = string.Empty;
                changed = true;
            }

            if (removedCombinationIndex >= 0)
            {
                manifest.Combinations.RemoveAt(removedCombinationIndex);
                changed = true;
            }

            if (changed)
            {
                source.IsDirty = true;
            }

            EditorGUILayout.EndVertical();
            return changed;
        }

        private static string DescribeLayout(LogChannelSource source, bool hasDirection, SegmentDirection direction)
        {
            var manifest = source.Manifest;
            var access = source.IsEditable ? "editable" : "read-only";

            if (!hasDirection)
            {
                return $"Direction '{manifest.Direction}' is neither Up nor Down ({access})";
            }

            if (direction == SegmentDirection.Down)
            {
                return $"Counts down from bit {manifest.StartBit} ({access})";
            }

            return manifest.Capacity > 0
                ? $"Counts up from bit {manifest.StartBit}, reserving bits {manifest.StartBit} to "
                  + $"{manifest.StartBit + manifest.Capacity - 1} ({access})"
                : $"Counts up from bit {manifest.StartBit} ({access})";
        }


        private void GenerateEnumHelpers()
        {
            var enumKeys = _enumGenerator.GetGeneratedKeyNames();

            var toStringNoBoxSource = EnumUtilsSourceWriter.CreateUtilsFromEnumDetails
            (
                new EnumDetails
                (
                    LogChannelsName,
                    enumKeys.ToArray(),
                    EnumUtilsGenOptions.GenerateToStringFast,
                    NamespaceStr
                )
            );

            _sourceFiles.Add(new SourceFile(_capturedEnumCode, "LogChannels.cs"));
            _sourceFiles.Add(new SourceFile(toStringNoBoxSource, "LogChannelsStringUtils.cs"));
        }

        /// <summary>
        ///     Reads the <see cref="LogChannelsName" /> already compiled into the project, so a regeneration
        ///     reproduces the channels the project has instead of replacing them with the studio defaults.
        ///     Members come back in value order, which for a flags enum is bit order, so the numbering every
        ///     saved mask and every package assembly depends on survives the round trip.
        ///     Returns <c>false</c> when no such enum is loaded, which is the first-generation case.
        /// </summary>
        private static bool TryReadCompiledChannels(out List<string> baseKeys,
            out List<EnumGeneratorComponent.FlagCombinationEntry> flagCombinations)
        {
            baseKeys = null;
            flagCombinations = null;

            var channelType = FindCompiledChannelType();
            if (channelType == null)
            {
                return false;
            }

            var names = Enum.GetNames(channelType);
            var values = Enum.GetValues(channelType);

            var singles = new List<(string Name, long Value)>();
            var composites = new List<(string Name, long Value)>();
            long union = 0;

            for (var i = 0; i < names.Length; i++)
            {
                var value = Convert.ToInt64(values.GetValue(i));
                if (HasSingleBit(value))
                {
                    singles.Add((names[i], value));
                    union |= value;
                }
            }

            for (var i = 0; i < names.Length; i++)
            {
                var value = Convert.ToInt64(values.GetValue(i));

                //None is 0, and All carries every bit including ones no channel owns. Both are written by
                //the generator itself, so neither belongs in the lists the window edits.
                if (value == 0 || HasSingleBit(value) || (value & ~union) != 0)
                {
                    continue;
                }

                composites.Add((names[i], value));
            }

            singles.Sort((left, right) => left.Value.CompareTo(right.Value));
            composites.Sort((left, right) => left.Value.CompareTo(right.Value));

            baseKeys = new List<string>(singles.Count);
            foreach (var single in singles)
            {
                baseKeys.Add(single.Name);
            }

            flagCombinations = new List<EnumGeneratorComponent.FlagCombinationEntry>(composites.Count);
            foreach (var composite in composites)
            {
                flagCombinations.Add
                (
                    new EnumGeneratorComponent.FlagCombinationEntry
                    {
                        Name = composite.Name,
                        Flags = DecomposeChannel(composite.Value, singles, composites),
                    }
                );
            }

            return baseKeys.Count > 0;
        }

        private static Type FindCompiledChannelType()
        {
            foreach (var assembly in AssemblyCompat.GetLoadedAssemblies())
            {
                //GetType over GetTypes: the latter throws on any assembly with an unresolved reference,
                //and this runs across every assembly in the domain.
                var type = assembly.GetType(LogChannelTypeName, throwOnError: false);
                if (type is { IsEnum: true })
                {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        ///     Names the members a composite value is made of, preferring a smaller composite over spelling
        ///     out its bits so <c>AI | Physics | Core</c> comes back as it was written rather than flattened.
        ///     Only a strictly smaller composite is used, so a definition can never reference itself.
        /// </summary>
        private static List<string> DecomposeChannel(long value, List<(string Name, long Value)> singles,
            List<(string Name, long Value)> composites)
        {
            var flags = new List<string>();
            var remaining = value;

            for (var i = composites.Count - 1; i >= 0; i--)
            {
                var candidate = composites[i];
                if (candidate.Value >= value || (remaining & candidate.Value) != candidate.Value)
                {
                    continue;
                }

                flags.Add(candidate.Name);
                remaining &= ~candidate.Value;
            }

            foreach (var single in singles)
            {
                if ((remaining & single.Value) != 0)
                {
                    flags.Add(single.Name);
                    remaining &= ~single.Value;
                }
            }

            return flags;
        }

        private static bool HasSingleBit(long value)
        {
            return value != 0 && (value & (value - 1)) == 0;
        }

        private static bool ReadSources(List<SourceFile> sourceFiles, List<string> runtimeAssemblyReferences, List<string> editorAssemblyReferences)
        {
            const string packagePath = "Packages/com.qbs.core";
            var rawSourceFolder = Path.Combine(packagePath, "RawSource~/LogSource");

            if (!Directory.Exists(rawSourceFolder))
            {
                Debug.LogWarning($"RawSource~ folder not found at package path: {rawSourceFolder}");
                rawSourceFolder = RawSourceFolderPath;

                if (!Directory.Exists(rawSourceFolder))
                {
                    Debug.LogError($"RawSource~ folder not found at fallback path: {rawSourceFolder}");
                    return false;
                }

                Debug.Log($"Using fallback RawSource~ folder at: {rawSourceFolder}");
            }

            var fileList = new List<string>();
            fileList.AddRange(Directory.GetFiles(rawSourceFolder, "*.cs", SearchOption.AllDirectories));
            fileList.AddRange(Directory.GetFiles(rawSourceFolder, "*.txt", SearchOption.AllDirectories));

            foreach (var file in fileList)
            {
                using var reader = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read));
                var sourceContent = reader.ReadToEnd();

                var fileName = Path.GetFileName(file);
                if (Path.GetExtension(fileName) != ".cs")
                {
                    fileName = Path.GetFileNameWithoutExtension(fileName) + ".cs";
                }

                sourceFiles.Add(new SourceFile(sourceContent, fileName));
            }

            // Read JSON file for assembly references
            var jsonFile = Path.Combine(rawSourceFolder, AssemblyReferencesJson);
            if (!File.Exists(jsonFile))
            {
                Debug.LogWarning($"AssemblyReferences.json not found at: {jsonFile}");
                return false;
            }

            try
            {
                var assemblyReferences = JsonUtility.FromJson<AssemblyReferencesData>(File.ReadAllText(jsonFile));
                if (assemblyReferences == null)
                {
                    return false;
                }

                if (assemblyReferences.RuntimeAssemblies is { Length: > 0 })
                {
                    runtimeAssemblyReferences.AddRange(ResolveAssemblyPaths(assemblyReferences.RuntimeAssemblies));
                    Debug.Log($"Loaded {assemblyReferences.RuntimeAssemblies.Length} runtime assembly references");
                }

                if (assemblyReferences.EditorAssemblies is { Length: > 0 })
                {
                    editorAssemblyReferences.AddRange(ResolveAssemblyPaths(assemblyReferences.EditorAssemblies));
                    Debug.Log($"Loaded {assemblyReferences.EditorAssemblies.Length} editor assembly references");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to parse AssemblyReferences.json: {e.Message}");
                return false;
            }

            return true;
        }

        private static List<string> ResolveAssemblyPaths(string[] assemblyNames)
        {
            var resolvedPaths = new List<string>();
            var loadedAssemblies = AssemblyCompat.GetLoadedAssemblies();

            foreach (var assemblyName in assemblyNames)
            {
                var assembly = loadedAssemblies.FirstOrDefault
                (a =>
                    a.GetName().Name.Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase)
                );

                if (assembly != null)
                {
                    var assemblyPath = AssemblyCompat.GetAssemblyPath(assembly);
                    resolvedPaths.Add(assemblyPath);
                    Debug.Log($"Resolved assembly '{assemblyName}' to: {assemblyPath}");
                }
                else
                {
                    Debug.LogWarning($"Could not resolve assembly: {assemblyName}");
                }
            }

            return resolvedPaths;
        }

        [Serializable]
        private class AssemblyReferencesData
        {
            public string[] RuntimeAssemblies;
            public string[] EditorAssemblies;
        }
    }
}