using System.Collections.Generic;
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
            enumGenerator.ConfigureReservedKeys
            (
                LogChannelDefaults.BaseKeys,
                LogChannelDefaults.FlagCombinations
            );

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

            return DLLGenerationHelper.TryGeneratingDLL
            (
                sourceFiles,
                "Log",
                "Log",
                runtimeAssemblyReferences,
                editorAssemblyReferences,
                scriptingSymbols
            );
        }
    }
}