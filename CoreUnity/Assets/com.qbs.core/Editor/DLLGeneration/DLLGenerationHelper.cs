using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using UnityEngine;
using UnityEngine.Assemblies;

namespace QBS.Core.Editor
{
    public static class DLLGenerationHelper
    {
        private static readonly Regex AssemblyNameFromCS0012 = new(@"reference to assembly '([^,]+)", RegexOptions.Compiled);

        private const string EditorFolderAddress = "Editor";
        private const string RuntimeFolderAddress = "Runtime";

        private const string RuntimeDLLExtension = ".dll";
        private const string EditorDLLSuffixAndExtension = "-Editor.dll";
        private const string PluginsFolderName = "Plugins";

        /// <summary>
        ///     Generates runtime and editor DLLs from the provided source files.
        /// </summary>
        /// <param name="sources">Collection of source files to compile into DLLs</param>
        /// <param name="outputDLLFolderName">Folder created under the Plugins folder to house the generated DLLs</param>
        /// <param name="dllName">Name of the DLL files to generate</param>
        /// <param name="runtimeAssemblyReferences">Additional assembly references to include in runtime compilation</param>
        /// <param name="editorAssemblyReferences">Additional assembly references to include in editor compilation</param>
        /// <param name="scriptingSymbols">Scripting symbols to define during compilation</param>
        /// <returns>True if both runtime and editor DLL generation succeeded; otherwise false</returns>
        public static bool TryGeneratingDLL(IEnumerable<SourceFile> sources, string outputDLLFolderName, string dllName,
            List<string> runtimeAssemblyReferences = null, List<string> editorAssemblyReferences = null, List<string> scriptingSymbols = null)
        {
            var editorSources = new List<SourceFile>();
            var runtimeSources = new List<SourceFile>();

            // Group sources by lifetime: runtime or editor.
            GroupSourcesByLifetime(sources, editorSources, runtimeSources);

            var pathToPluginsFolder = Path.Combine(Application.dataPath, PluginsFolderName, outputDLLFolderName);

            // Compile runtime sources first,
            // since editor sources will always be dependent on runtime sources.
            var runtimeGenSuccess = CompileSources(true, runtimeSources, dllName, pathToPluginsFolder, runtimeAssemblyReferences, scriptingSymbols);

            List<string> extraAssembliesToReference = null;
            if (runtimeSources.Count > 0 && runtimeGenSuccess)
            {
                extraAssembliesToReference = new List<string>
                {
                    GetPathForDLL(true, dllName, pathToPluginsFolder)
                };
                if (editorAssemblyReferences != null)
                {
                    extraAssembliesToReference.AddRange(editorAssemblyReferences);
                }
            }
            else if (editorAssemblyReferences != null)
            {
                extraAssembliesToReference = new List<string>(editorAssemblyReferences);
            }

            var editorGenSuccess = CompileSources(false, editorSources, dllName, pathToPluginsFolder, extraAssembliesToReference, scriptingSymbols);

            //clean up if either generation failed
            if (!(runtimeGenSuccess && editorGenSuccess))
            {
                if (Directory.Exists(pathToPluginsFolder))
                {
                    Directory.Delete(pathToPluginsFolder, recursive: true);
                }
            }

            return runtimeGenSuccess && editorGenSuccess;
        }

        /// <summary>
        ///     Separates source files into editor-scoped and runtime-scoped collections based on their using statements.
        /// </summary>
        /// <param name="sources">Collection of source files to categorize</param>
        /// <param name="editorSources">Output collection for source files containing editor-specific using statements</param>
        /// <param name="runtimeSources">Output collection for source files without editor-specific using statements</param>
        private static void GroupSourcesByLifetime(IEnumerable<SourceFile> sources, List<SourceFile> editorSources, List<SourceFile> runtimeSources)
        {
            foreach (var sourceFile in sources)
            {
                var usingStatements = GetUsingStatementsFromSource(sourceFile.SourceContent);
                if (usingStatements.Length == 0)
                {
                    Debug.Log($"No using statements found in source file: {sourceFile.FilePath}");
                }

                var isEditorScoped = usingStatements.Any(statement => statement.Contains("Editor", StringComparison.InvariantCultureIgnoreCase));
                if (isEditorScoped)
                {
                    editorSources.Add(sourceFile);
                }
                else
                {
                    runtimeSources.Add(sourceFile);
                }
            }
        }

        /// <summary>
        ///     Extracts using statements from C# source code using Roslyn syntax tree analysis.
        /// </summary>
        /// <param name="sourceCode">C# source code as string</param>
        /// <returns>Array of using directive namespace names</returns>
        private static string[] GetUsingStatementsFromSource(string sourceCode)
        {
            if (string.IsNullOrEmpty(sourceCode))
            {
                return Array.Empty<string>();
            }

            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = syntaxTree.GetCompilationUnitRoot();

                // Get all using directives and return their namespace names
                var usingDirectives = root.Usings;
                return usingDirectives.Select(u => u.Name.ToString()).ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static bool CompileSources(bool compileForRuntime, List<SourceFile> sources,
            string dllName, string pathToPluginsFolder, List<string> extraReferenceAssemblies = null, List<string> scriptingSymbols = null)
        {
            if (sources.Count == 0)
            {
                return true;
            }

            var pathToDLL = GetPathForDLL(compileForRuntime, dllName, pathToPluginsFolder);

            var generationParameters = new DLLGenerationParameters
            (
                sources,
                pathToDLL,
                extraReferenceAssemblies,
                scriptingSymbols
            );

            var dllGenerator = new DLLGenerator();
            return RecursivelyTryCompilation(dllGenerator, generationParameters);
        }


        private static bool RecursivelyTryCompilation(DLLGenerator dllGenerator, DLLGenerationParameters generationParameters, int maxTries = 3, int currentTry = 0)
        {
            var compilationResult = dllGenerator.GenerateDLL(generationParameters, out var errorDetails);

            if (!compilationResult && currentTry < maxTries)
            {
                currentTry++;
                if (errorDetails.ErrorCount == 0)
                {
                    // Compilation failed but not due to code errors
                    LogCompilationErrors(errorDetails, generationParameters);

                    return false;
                }

                var loadedAssemblies = CurrentAssemblies.GetLoadedAssemblies();
                var missingAssemblyReferenceErrorFound = false;
                foreach (var errorDiagnostic in errorDetails.Diagnostics)
                {
                    // The type 'type' is defined in an assembly that is not referenced. You must add a reference to assembly 'assembly'.
                    // here we can just find the assembly name from the error message and add it to the extra references
                    if (errorDiagnostic.Id != "CS0012")
                    {
                        continue;
                    }

                    var match = AssemblyNameFromCS0012.Match(errorDiagnostic.Message);
                    if (!match.Success)
                    {
                        continue;
                    }

                    missingAssemblyReferenceErrorFound = true;
                    var assemblyName = match.Groups[1].Value;
                    var assembly = loadedAssemblies.FirstOrDefault
                    (a =>
                        string.Equals
                        (
                            a.GetName().Name,
                            assemblyName,
                            StringComparison.Ordinal
                        )
                    );

                    if (assembly != null && !string.IsNullOrEmpty(assembly.GetLoadedAssemblyPath()))
                    {
                        generationParameters.ExtraAssembliesToReference.Add(assembly.Location);
                    }
                }

                // This helper can only fix missing assembly reference errors
                // If no missing assembly reference errors were found, return false
                if (!missingAssemblyReferenceErrorFound)
                {
                    LogCompilationErrors(errorDetails, generationParameters);
                    return false;
                }

                return RecursivelyTryCompilation(dllGenerator, generationParameters, maxTries, currentTry);
            }

            if (!compilationResult)
            {
                LogCompilationErrors(errorDetails, generationParameters);
            }

            return compilationResult;
        }

        private static void LogCompilationErrors(DLLGenerationErrorDetails errorDetails, DLLGenerationParameters generationParameters)
        {
            var message = new System.Text.StringBuilder();
            message.AppendLine($"[DLLGeneration] Compilation failed with {errorDetails.ErrorCount} error(s):");
            foreach (var diagnostic in errorDetails.Diagnostics)
            {
                message.AppendLine($"  [{diagnostic.Id}] {diagnostic.File}({diagnostic.Line},{diagnostic.Column}): {diagnostic.Message}");
            }
            Debug.LogError(message.ToString());

            Debug.Log($"[DLLGeneration] Writing input files to temp cache - {Application.temporaryCachePath}");
            foreach (var files in generationParameters.Sources)
            {
                File.WriteAllText(Path.Combine(Application.temporaryCachePath, Path.GetFileName(files.FilePath)), files.SourceContent);
            }
        }

        /// <summary>
        ///     Constructs the full file path for a DLL based on its scope (runtime or editor).
        /// </summary>
        /// <param name="generateForRuntime">True for runtime DLL path; false for editor DLL path</param>
        /// <param name="dllName">Name of the DLL file</param>
        /// <param name="pathToPluginsFolder">Path to the plugins folder</param>
        /// <returns>Full path to the DLL file</returns>
        private static string GetPathForDLL(bool generateForRuntime, string dllName, string pathToPluginsFolder)
        {
            var dllFolder = Path.Combine(pathToPluginsFolder, generateForRuntime ? RuntimeFolderAddress : EditorFolderAddress);
            if (!Directory.Exists(dllFolder))
            {
                Directory.CreateDirectory(dllFolder);
            }

            var fileName = string.Concat(dllName, generateForRuntime ? RuntimeDLLExtension : EditorDLLSuffixAndExtension);
            return Path.Combine(dllFolder, fileName);
        }
    }
}