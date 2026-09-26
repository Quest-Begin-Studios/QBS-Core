using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace QBS.Core.Editor
{
    /// <summary>
    ///     Finds every <c>LogChannels.json</c> the project can see and turns them into the bit layout of
    ///     <c>LogChannel</c>. Each manifest pins its own channels, so the layout is the same on every machine
    ///     whatever order packages were installed in, and a clash stops generation rather than move a channel
    ///     somebody's code was compiled against.
    /// </summary>
    public static class LogChannelResolver
    {
        public const string ProjectOwner = "Project";
        public const string ProjectManifestPath = "Assets/Editor/" + LogChannelManifest.FileName;
        //Bit 63 is the sign bit of the long LogChannel is backed by.
        public const int HighestBit = 62;
        public const int DefaultPackageCapacity = 8;

        private const string PackageManifestFolder = "Editor";
        private const string PackageJson = "package.json";

        /// <summary>
        ///     Every manifest in a registered package's <c>Editor</c> folder or anywhere under <c>Assets</c>,
        ///     read-only ones first. A manifest that cannot be read is reported in <paramref name="errors" />
        ///     and left out.
        /// </summary>
        public static List<LogChannelSource> FindSources(List<string> errors)
        {
            var sources = new List<LogChannelSource>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var package in PackageInfo.GetAllRegisteredPackages())
            {
                var fullPath = Path.GetFullPath
                (
                    Path.Combine(package.resolvedPath, PackageManifestFolder, LogChannelManifest.FileName)
                );
                if (!File.Exists(fullPath) || !seenPaths.Add(fullPath))
                {
                    continue;
                }

                AddSource
                (
                    sources,
                    errors,
                    new LogChannelSource
                    {
                        Owner = package.name,
                        DisplayPath = $"{package.assetPath}/{PackageManifestFolder}/{LogChannelManifest.FileName}",
                        FullPath = fullPath,
                        IsPackage = true,
                        IsEditable = IsEditable(package.source),
                    }
                );
            }

            //A package kept under Assets, the way this repository keeps com.qbs.core, is not registered with
            //the package manager, so its manifest is found here and its package by the package.json above it.
            foreach (var guid in AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(LogChannelManifest.FileName), new[] { "Assets" }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.Equals(Path.GetFileName(assetPath), LogChannelManifest.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fullPath = Path.GetFullPath(assetPath);
                if (!seenPaths.Add(fullPath))
                {
                    continue;
                }

                var packageName = FindOwningPackageName(Path.GetDirectoryName(fullPath));
                AddSource
                (
                    sources,
                    errors,
                    new LogChannelSource
                    {
                        Owner = packageName ?? ProjectOwner,
                        DisplayPath = assetPath,
                        FullPath = fullPath,
                        IsPackage = packageName != null,
                        IsEditable = true,
                    }
                );
            }

            sources.Sort
            (
                (left, right) => left.IsEditable != right.IsEditable
                    ? left.IsEditable.CompareTo(right.IsEditable)
                    : string.CompareOrdinal(left.Owner, right.Owner)
            );

            return sources;
        }

        /// <summary>
        ///     The editable places that could hold a manifest and do not yet: embedded and local packages,
        ///     and the project itself. Each comes back without a <see cref="LogChannelSource.Manifest" />.
        /// </summary>
        public static List<LogChannelSource> FindMissingEditableSources(List<LogChannelSource> sources)
        {
            var owners = new HashSet<string>(sources.Select(source => source.Owner));
            var missing = new List<LogChannelSource>();

            foreach (var package in PackageInfo.GetAllRegisteredPackages())
            {
                if (!IsEditable(package.source) || owners.Contains(package.name))
                {
                    continue;
                }

                missing.Add
                (
                    new LogChannelSource
                    {
                        Owner = package.name,
                        DisplayPath = $"{package.assetPath}/{PackageManifestFolder}/{LogChannelManifest.FileName}",
                        FullPath = Path.GetFullPath
                        (
                            Path.Combine(package.resolvedPath, PackageManifestFolder, LogChannelManifest.FileName)
                        ),
                        IsPackage = true,
                        IsEditable = true,
                    }
                );
            }

            if (!owners.Contains(ProjectOwner))
            {
                missing.Add(CreateProjectSource());
            }

            return missing;
        }

        public static LogChannelSource CreateProjectSource()
        {
            return new LogChannelSource
            {
                Owner = ProjectOwner,
                DisplayPath = ProjectManifestPath,
                FullPath = Path.GetFullPath(ProjectManifestPath),
                IsPackage = false,
                IsEditable = true,
            };
        }

        /// <summary>
        ///     A new, empty manifest laid out the only time its layout is ever decided: now, before anything is
        ///     compiled against it. A package's counts up from past every other upward manifest's reservation.
        ///     Anything else is the game's, the last thing built, and counts down from below every other
        ///     downward manifest, from the top bit when there is none.
        /// </summary>
        public static LogChannelManifest CreateManifest(bool isPackage, List<LogChannelSource> sources)
        {
            var manifest = new LogChannelManifest();

            if (isPackage)
            {
                var startBit = 0;
                foreach (var source in sources)
                {
                    if (source.Manifest == null || !TryGetDirection(source.Manifest, out var direction)
                        || direction != SegmentDirection.Up)
                    {
                        continue;
                    }

                    var reserved = Math.Max(source.Manifest.Capacity, source.Manifest.Channels.Count);
                    startBit = Math.Max(startBit, source.Manifest.StartBit + reserved);
                }

                manifest.StartBit = startBit;
                manifest.Direction = nameof(SegmentDirection.Up);
                manifest.Capacity = DefaultPackageCapacity;
            }
            else
            {
                var startBit = HighestBit;
                foreach (var source in sources)
                {
                    if (source.Manifest == null || !TryGetDirection(source.Manifest, out var direction)
                        || direction != SegmentDirection.Down)
                    {
                        continue;
                    }

                    startBit = Math.Min(startBit, source.Manifest.StartBit - source.Manifest.Channels.Count);
                }

                manifest.StartBit = startBit;
                manifest.Direction = nameof(SegmentDirection.Down);
            }

            return manifest;
        }

        /// <summary>
        ///     Checks the manifests against each other and turns them into the segments and combinations the
        ///     enum generator takes. Two manifests may declare the same channel on the same bit and share it;
        ///     anything else that lands two channels on one bit is an error. Returns <c>false</c> with every
        ///     problem in <paramref name="errors" /> rather than settle a clash by moving a channel.
        /// </summary>
        public static bool Resolve(List<LogChannelSource> sources, List<FlagSegment> segments,
            List<EnumGeneratorComponent.FlagCombinationEntry> combinations, List<string> errors)
        {
            var errorCount = errors.Count;
            var claims = new Dictionary<int, (string Channel, LogChannelSource Source)>();
            var channelBits = new Dictionary<string, (int Bit, LogChannelSource Source)>();

            foreach (var source in sources)
            {
                var manifest = source.Manifest;
                if (!TryGetDirection(manifest, out var direction))
                {
                    errors.Add($"{Describe(source)} has Direction '{manifest.Direction}'; it must be Up or Down.");
                    continue;
                }

                if (manifest.StartBit < 0 || manifest.StartBit > HighestBit)
                {
                    errors.Add($"{Describe(source)} starts at bit {manifest.StartBit}, outside 0 to {HighestBit}.");
                    continue;
                }

                if (direction == SegmentDirection.Up && manifest.Capacity > 0 && manifest.Channels.Count > manifest.Capacity)
                {
                    errors.Add
                    (
                        $"{Describe(source)} lists {manifest.Channels.Count} channels but reserves {manifest.Capacity} bits."
                    );
                }

                var keys = new List<string>(manifest.Channels.Count);
                var segment = new FlagSegment(manifest.StartBit, direction, keys);

                for (var i = 0; i < manifest.Channels.Count; i++)
                {
                    var channel = manifest.Channels[i]?.Trim() ?? string.Empty;
                    keys.Add(channel);

                    var bit = segment.BitAt(i);
                    var label = channel.Length == 0 ? "a retired channel" : channel;

                    if (bit < 0 || bit > HighestBit)
                    {
                        errors.Add($"{label} from {Describe(source)} lands on bit {bit}, outside 0 to {HighestBit}.");
                        continue;
                    }

                    if (channel.Length > 0 && channelBits.TryGetValue(channel, out var declared))
                    {
                        if (declared.Bit != bit)
                        {
                            errors.Add
                            (
                                $"{channel} is on bit {declared.Bit} in {Describe(declared.Source)} "
                                + $"but on bit {bit} in {Describe(source)}."
                            );
                        }

                        continue;
                    }

                    if (claims.TryGetValue(bit, out var claim))
                    {
                        errors.Add
                        (
                            $"Bit {bit} is claimed by {claim.Channel} from {Describe(claim.Source)} "
                            + $"and by {label} from {Describe(source)}."
                        );
                        continue;
                    }

                    claims[bit] = (label, source);
                    if (channel.Length > 0)
                    {
                        channelBits[channel] = (bit, source);
                    }
                }

                segments.Add(segment);
            }

            var combinationSources = new Dictionary<string, (EnumGeneratorComponent.FlagCombinationEntry Entry, LogChannelSource Source)>();

            foreach (var source in sources)
            {
                foreach (var combination in source.Manifest.Combinations)
                {
                    var name = combination?.Name?.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    var flags = (combination.Flags ?? new List<string>())
                        .Select(flag => flag?.Trim())
                        .Where(flag => !string.IsNullOrEmpty(flag))
                        .ToList();

                    if (channelBits.ContainsKey(name))
                    {
                        errors.Add($"Combination {name} from {Describe(source)} has the same name as a channel.");
                        continue;
                    }

                    if (combinationSources.TryGetValue(name, out var declared))
                    {
                        if (!new HashSet<string>(declared.Entry.Flags).SetEquals(flags))
                        {
                            errors.Add
                            (
                                $"Combination {name} is defined differently in {Describe(declared.Source)} "
                                + $"and {Describe(source)}."
                            );
                        }

                        continue;
                    }

                    var entry = new EnumGeneratorComponent.FlagCombinationEntry { Name = name, Flags = flags };
                    combinationSources[name] = (entry, source);
                    combinations.Add(entry);
                }
            }

            foreach (var combination in combinations)
            {
                foreach (var flag in combination.Flags)
                {
                    if (!channelBits.ContainsKey(flag) && !combinationSources.ContainsKey(flag))
                    {
                        errors.Add
                        (
                            $"Combination {combination.Name} from {Describe(combinationSources[combination.Name].Source)} "
                            + $"references {flag}, which no manifest declares."
                        );
                    }
                }
            }

            return errors.Count == errorCount;
        }

        /// <summary>
        ///     The value every live channel in <paramref name="segments" /> gets, by name.
        /// </summary>
        public static Dictionary<string, long> GetChannelValues(IEnumerable<FlagSegment> segments)
        {
            var values = new Dictionary<string, long>();

            foreach (var segment in segments)
            {
                for (var i = 0; i < segment.Keys.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(segment.Keys[i]))
                    {
                        values[segment.Keys[i]] = 1L << segment.BitAt(i);
                    }
                }
            }

            return values;
        }

        public static void Save(LogChannelSource source)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(source.FullPath));
            File.WriteAllText(source.FullPath, JsonUtility.ToJson(source.Manifest, true));
            source.IsDirty = false;
        }

        public static bool TryGetDirection(LogChannelManifest manifest, out SegmentDirection direction)
        {
            //TryParse also takes a number, which IsDefined then rejects unless it names a direction.
            return Enum.TryParse(manifest.Direction, true, out direction)
                   && Enum.IsDefined(typeof(SegmentDirection), direction);
        }

        public static string Describe(LogChannelSource source)
        {
            return $"{source.Owner} ({source.DisplayPath})";
        }

        private static bool IsEditable(PackageSource source)
        {
            return source is PackageSource.Embedded or PackageSource.Local;
        }

        private static void AddSource(List<LogChannelSource> sources, List<string> errors, LogChannelSource source)
        {
            try
            {
                source.Manifest = JsonUtility.FromJson<LogChannelManifest>(File.ReadAllText(source.FullPath));
            }
            catch (Exception e)
            {
                errors.Add($"{Describe(source)} could not be read: {e.Message}");
                return;
            }

            if (source.Manifest == null)
            {
                errors.Add($"{Describe(source)} is empty.");
                return;
            }

            source.Manifest.Channels ??= new List<string>();
            source.Manifest.Combinations ??= new List<EnumGeneratorComponent.FlagCombinationEntry>();
            sources.Add(source);
        }

        /// <summary>
        ///     The name in the nearest <c>package.json</c> between <paramref name="directory" /> and
        ///     <c>Assets</c>, or <c>null</c> when the manifest is the project's own.
        /// </summary>
        private static string FindOwningPackageName(string directory)
        {
            var assetsRoot = Path.GetFullPath(Application.dataPath);

            while (!string.IsNullOrEmpty(directory)
                   && directory.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
            {
                var packageJson = Path.Combine(directory, PackageJson);
                if (File.Exists(packageJson))
                {
                    try
                    {
                        var name = JsonUtility.FromJson<PackageName>(File.ReadAllText(packageJson))?.name;
                        return string.IsNullOrEmpty(name) ? Path.GetFileName(directory) : name;
                    }
                    catch (Exception)
                    {
                        return Path.GetFileName(directory);
                    }
                }

                directory = Path.GetDirectoryName(directory);
            }

            return null;
        }

        [Serializable]
        private class PackageName
        {
            public string name;
        }
    }
}
