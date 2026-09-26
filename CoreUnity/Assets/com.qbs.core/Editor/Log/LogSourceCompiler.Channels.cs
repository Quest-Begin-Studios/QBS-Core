using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;

namespace QBS.Core.Editor
{
    public partial class LogSourceCompiler
    {
        //Mirror LogEditorConstants, which is generated into Log-Editor.dll and so out of this assembly's reach.
        private const string ActiveChannelsPrefsKey = "QBS_Log_ActiveChannelMask";
        private const string RuntimeSettingsPath = "Assets/Plugins/Log/Runtime/RuntimeLogSettings.asset";
        private const string RuntimeSettingsChannelsField = "enabledChannels";

        /// <summary>
        ///     Whether the <see cref="LogChannelsName" /> compiled into the project is the one the manifests
        ///     describe. A package installed or updated since the last generation brings channels its code
        ///     needs and the compiled enum lacks, and nothing else says why that code then fails to compile.
        /// </summary>
        internal static bool TryDescribeOutdatedChannels(out string description)
        {
            description = null;

            var errors = new List<string>();
            var sources = LogChannelResolver.FindSources(errors);
            var segments = new List<FlagSegment>();
            var combinations = new List<EnumGeneratorComponent.FlagCombinationEntry>();

            if (!LogChannelResolver.Resolve(sources, segments, combinations, errors) || errors.Count > 0)
            {
                description = $"The {LogChannelManifest.FileName} manifests do not resolve:\n" + string.Join("\n", errors);
                return true;
            }

            var compiled = ReadCompiledChannelValues();
            var outdated = LogChannelResolver.GetChannelValues(segments)
                .Where(expected => !compiled.TryGetValue(expected.Key, out var value) || value != expected.Value)
                .Select(expected => expected.Key)
                .ToList();

            if (outdated.Count == 0)
            {
                return false;
            }

            description = $"The compiled {LogChannelsName} does not match the {LogChannelManifest.FileName} manifests: "
                          + $"{string.Join(", ", outdated)} missing or on a different bit.";
            return true;
        }

        /// <summary>
        ///     The channels compiled into the project that no manifest declares — a game's own, from before
        ///     they were kept in one — as a new, unsaved project manifest counting down from the top bit.
        ///     <c>null</c> when there is nothing to carry over, or the project already has a manifest and
        ///     anything missing from it was retired on purpose.
        /// </summary>
        private static LogChannelSource CreateProjectSourceFromCompiled(List<LogChannelSource> sources)
        {
            if (sources.Any(source => source.Owner == LogChannelResolver.ProjectOwner)
                || !TryReadCompiledChannels(out var baseKeys, out var flagCombinations))
            {
                return null;
            }

            var declaredChannels = new HashSet<string>(sources.SelectMany(source => source.Manifest.Channels));
            var declaredCombinations = new HashSet<string>
            (
                sources.SelectMany(source => source.Manifest.Combinations).Select(combination => combination.Name)
            );

            var manifest = LogChannelResolver.CreateManifest(false, sources);
            manifest.Channels.AddRange(baseKeys.Where(channel => !declaredChannels.Contains(channel)));
            manifest.Combinations.AddRange
            (
                flagCombinations.Where(combination => !declaredCombinations.Contains(combination.Name))
            );

            if (manifest.Channels.Count == 0 && manifest.Combinations.Count == 0)
            {
                return null;
            }

            var source = LogChannelResolver.CreateProjectSource();
            source.Manifest = manifest;
            source.IsDirty = true;
            return source;
        }

        /// <summary>
        ///     Every single-bit member of the compiled <see cref="LogChannelsName" />, by name. Empty when none
        ///     is compiled.
        /// </summary>
        private static Dictionary<string, long> ReadCompiledChannelValues()
        {
            var values = new Dictionary<string, long>();

            var channelType = FindCompiledChannelType();
            if (channelType == null)
            {
                return values;
            }

            var names = Enum.GetNames(channelType);
            var enumValues = Enum.GetValues(channelType);

            for (var i = 0; i < names.Length; i++)
            {
                var value = Convert.ToInt64(enumValues.GetValue(i));
                if (HasSingleBit(value))
                {
                    values[names[i]] = value;
                }
            }

            return values;
        }

        /// <summary>
        ///     Carries the editor's and the runtime settings' saved channel masks from the compiled layout to
        ///     the one just generated, so a channel whose bit moved keeps its on/off state. Nothing to do when
        ///     no layout was compiled before.
        /// </summary>
        private static void RemapSavedChannelMasks(Dictionary<string, long> previousValues, Dictionary<string, long> newValues)
        {
            if (previousValues.Count == 0 || newValues == null)
            {
                return;
            }

            var stored = EditorPrefs.GetString(ActiveChannelsPrefsKey, string.Empty);
            if (long.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out var editorMask))
            {
                EditorPrefs.SetString
                (
                    ActiveChannelsPrefsKey,
                    RemapChannelMask(editorMask, previousValues, newValues).ToString(CultureInfo.InvariantCulture)
                );
            }

            var settings = AssetDatabase.LoadMainAssetAtPath(RuntimeSettingsPath);
            if (settings == null)
            {
                return;
            }

            var serializedSettings = new SerializedObject(settings);
            var enabledChannels = serializedSettings.FindProperty(RuntimeSettingsChannelsField);
            if (enabledChannels == null)
            {
                return;
            }

            enabledChannels.longValue = RemapChannelMask(enabledChannels.longValue, previousValues, newValues);
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        ///     Rebuilds <paramref name="mask" /> by channel name: a channel keeps its state wherever its bit went,
        ///     a channel new to the layout starts enabled, and bits no channel owns any more are dropped. A mask
        ///     of every bit (<c>All</c>) is left as it is.
        /// </summary>
        private static long RemapChannelMask(long mask, Dictionary<string, long> previousValues, Dictionary<string, long> newValues)
        {
            if (mask == -1)
            {
                return mask;
            }

            long remapped = 0;
            foreach (var channel in newValues)
            {
                var isEnabled = !previousValues.TryGetValue(channel.Key, out var previousValue) || (mask & previousValue) != 0;
                if (isEnabled)
                {
                    remapped |= channel.Value;
                }
            }

            return remapped;
        }
    }
}
