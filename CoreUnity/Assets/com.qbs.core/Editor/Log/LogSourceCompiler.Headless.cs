using System.Collections.Generic;
using System.Linq;
using QBS.SourceGenerators;
using QBS.SourceGenerators.GeneratorDiscoveryHelpers;
using UnityEngine;

namespace QBS.Core.Editor
{
    public partial class LogSourceCompiler
    {
        public static bool GenerateLogDLLsHeadless()
        {
            var sourceFiles = new List<SourceFile>();
            var runtimeAssemblyReferences = new List<string>();
            var editorAssemblyReferences = new List<string>();
            var scriptingSymbols = new List<string> { "ENABLE_LOGS" };

            if (!ReadSources(sourceFiles, runtimeAssemblyReferences, editorAssemblyReferences))
            {
                Debug.LogError("GenerateLogDLLsHeadless: Failed to read sources.");
                return false;
            }

            var enumGenerator = new EnumGeneratorComponent();
            enumGenerator.Initialize();
            enumGenerator.ConfigureEnumProperties
            (
                LogChannelsName,
                NamespaceStr,
                EnumGeneratorComponent.EnumTypeOption.Flags,
                EnumGeneratorComponent.BackingType.Long
            );
            var errors = new List<string>();
            var channelSources = LogChannelResolver.FindSources(errors);

            //Generating from the manifests alone would delete every game channel that predates them from under
            //its own call sites, and moving those into a manifest moves their bits, which wants a person at the
            //window rather than an unattended rebuild.
            var notCarriedOver = CreateProjectSourceFromCompiled(channelSources);
            if (notCarriedOver != null)
            {
                var names = notCarriedOver.Manifest.Channels
                    .Concat(notCarriedOver.Manifest.Combinations.Select(combination => combination.Name));
                Debug.LogError
                (
                    $"GenerateLogDLLsHeadless: the {LogChannelsName} compiled into this project has members no "
                    + $"{LogChannelManifest.FileName} declares ({string.Join(", ", names)}). Open Tools > QBS > Logs > "
                    + $"Log Source Compiler once to move them into {LogChannelResolver.ProjectManifestPath}."
                );
                return false;
            }

            var segments = new List<FlagSegment>();
            var combinations = new List<EnumGeneratorComponent.FlagCombinationEntry>();

            if (!LogChannelResolver.Resolve(channelSources, segments, combinations, errors) || errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Debug.LogError($"GenerateLogDLLsHeadless: {error}");
                }

                return false;
            }

            Debug.Log($"GenerateLogDLLsHeadless: laying out the channels of {channelSources.Count} {LogChannelManifest.FileName} manifests.");

            enumGenerator.ConfigureFlagSegments(segments);
            enumGenerator.ConfigureEnumKeys(flagCombinations: combinations);

            if (!enumGenerator.GenerateEnum())
            {
                Debug.LogError("GenerateLogDLLsHeadless: Enum generation failed.");
                return false;
            }

            var capturedEnumCode = enumGenerator.GeneratedCode;

            var enumKeys = enumGenerator.GetGeneratedKeyNames().ToArray();

            var toStringNoBoxSource = EnumUtilsSourceWriter.CreateUtilsFromEnumDetails
            (
                new EnumDetails
                (
                    LogChannelsName,
                    enumKeys,
                    EnumUtilsGenOptions.GenerateToStringFast,
                    NamespaceStr
                )
            );

            sourceFiles.Add(new SourceFile(capturedEnumCode, "LogChannels.cs"));
            sourceFiles.Add(new SourceFile(toStringNoBoxSource, "LogChannelsStringUtils.cs"));

            var previousChannelValues = ReadCompiledChannelValues();
            var success = DLLGenerationHelper.TryGeneratingDLL
            (
                sourceFiles,
                "Log",
                "Log",
                runtimeAssemblyReferences,
                editorAssemblyReferences,
                scriptingSymbols
            );

            if (success)
            {
                RemapSavedChannelMasks(previousChannelValues, LogChannelResolver.GetChannelValues(segments));
            }

            return success;
        }
    }
}