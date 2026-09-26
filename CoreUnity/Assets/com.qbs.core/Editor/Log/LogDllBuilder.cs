using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace QBS.Core.Editor
{
    /// <summary>
    ///     Builds <c>Log.dll</c> and <c>Log-Editor.dll</c> from <c>RawSource~/LogSource</c> into this package's
    ///     <c>Plugins/Log</c>. Nothing in either DLL depends on the project using it, so core builds them when the
    ///     log sources change and commits the result; a project that installs the package never builds them.
    /// </summary>
    /// <remarks>
    ///     The sources stay compiled into DLLs so the console's double-click lands on the caller rather than on
    ///     <c>UnitySink</c>, and always with <c>ENABLE_LOGS</c>, which the private <c>[Conditional]</c> helpers rely on.
    /// </remarks>
    public static class LogDllBuilder
    {
        private const string MenuPath = "Tools/Logs/Build Log DLLs";
        private const string AssemblyReferencesJson = "AssemblyReferences.json";
        private const string DllName = "Log";
        private const string EditorAssemblyName = "QBS.Editor";

        [MenuItem(MenuPath)]
        private static void BuildFromMenu()
        {
            if (Build())
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                EditorUtility.RequestScriptReload();
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool CanBuildFromMenu()
        {
            return TryGetWritablePackageRoot(out _);
        }

        /// <summary>For CI: <c>-executeMethod QBS.Core.Editor.LogDllBuilder.BuildHeadless</c>.</summary>
        public static void BuildHeadless()
        {
            if (!Build())
            {
                throw new Exception("[QBS] Building the Log DLLs failed; the errors above say why.");
            }
        }

        public static bool Build()
        {
            if (!TryGetWritablePackageRoot(out var packageRoot))
            {
                Debug.LogError("[QBS] The Log DLLs can only be built where com.qbs.core is embedded or local; an installed copy ships them prebuilt.");
                return false;
            }

            var rawSourceFolder = Path.Combine(packageRoot, "RawSource~", "LogSource");
            if (!TryReadSources(rawSourceFolder, out var sources, out var runtimeReferences, out var editorReferences))
            {
                return false;
            }

            //Built in a temp folder and copied over: DLLGenerationHelper deletes its output folder when a
            //compilation fails, which here would take the committed DLLs and their .meta files with it.
            var buildFolder = Path.Combine(Path.GetTempPath(), "QBS-LogBuild");
            if (Directory.Exists(buildFolder))
            {
                Directory.Delete(buildFolder, recursive: true);
            }

            var built = DLLGenerationHelper.TryGeneratingDLLAt
            (
                sources,
                buildFolder,
                DllName,
                runtimeReferences,
                editorReferences,
                new List<string> { "ENABLE_LOGS" }
            );

            if (!built)
            {
                return false;
            }

            var pluginsFolder = Path.Combine(packageRoot, "Plugins", "Log");
            CopyBuiltFiles(Path.Combine(buildFolder, "Runtime"), Path.Combine(pluginsFolder, "Runtime"));
            CopyBuiltFiles(Path.Combine(buildFolder, "Editor"), Path.Combine(pluginsFolder, "Editor"));
            Directory.Delete(buildFolder, recursive: true);

            Debug.Log($"[QBS] Built the Log DLLs into {pluginsFolder}.");
            return true;
        }

        /// <summary>
        ///     The package's folder, when it is one this project may write to: an embedded or local package, or
        ///     the package's source kept under Assets as in core's own repository.
        /// </summary>
        private static bool TryGetWritablePackageRoot(out string packageRoot)
        {
            packageRoot = null;

            var package = PackageInfo.FindForAssembly(typeof(LogDllBuilder).Assembly);
            if (package != null)
            {
                if (package.source != PackageSource.Embedded && package.source != PackageSource.Local)
                {
                    return false;
                }

                packageRoot = package.resolvedPath;
                return true;
            }

            var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(EditorAssemblyName);
            if (string.IsNullOrEmpty(asmdefPath))
            {
                return false;
            }

            //<package>/Editor/QBS.Core.Editor.asmdef
            packageRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(asmdefPath)!, ".."));
            return true;
        }

        private static bool TryReadSources(string rawSourceFolder, out List<SourceFile> sources,
            out List<string> runtimeReferences, out List<string> editorReferences)
        {
            sources = new List<SourceFile>();
            runtimeReferences = new List<string>();
            editorReferences = new List<string>();

            if (!Directory.Exists(rawSourceFolder))
            {
                Debug.LogError($"[QBS] Log sources not found at {rawSourceFolder}.");
                return false;
            }

            foreach (var file in Directory.GetFiles(rawSourceFolder, "*.txt", SearchOption.AllDirectories))
            {
                sources.Add(new SourceFile(File.ReadAllText(file), Path.GetFileNameWithoutExtension(file) + ".cs"));
            }

            var jsonFile = Path.Combine(rawSourceFolder, AssemblyReferencesJson);
            if (!File.Exists(jsonFile))
            {
                Debug.LogError($"[QBS] {AssemblyReferencesJson} not found at {jsonFile}.");
                return false;
            }

            var references = JsonUtility.FromJson<AssemblyReferencesData>(File.ReadAllText(jsonFile));
            if (references == null)
            {
                Debug.LogError($"[QBS] {jsonFile} could not be read.");
                return false;
            }

            runtimeReferences.AddRange(ResolveAssemblyPaths(references.RuntimeAssemblies));
            editorReferences.AddRange(ResolveAssemblyPaths(references.EditorAssemblies));
            return true;
        }

        private static List<string> ResolveAssemblyPaths(string[] assemblyNames)
        {
            var resolvedPaths = new List<string>();
            if (assemblyNames == null)
            {
                return resolvedPaths;
            }

            var loadedAssemblies = AssemblyCompat.GetLoadedAssemblies().ToList();

            foreach (var assemblyName in assemblyNames)
            {
                var assembly = loadedAssemblies.FirstOrDefault
                (a =>
                    a.GetName().Name.Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase)
                );

                if (assembly != null)
                {
                    resolvedPaths.Add(AssemblyCompat.GetAssemblyPath(assembly));
                }
                else
                {
                    //Not fatal: the Log assembly itself is listed for the editor build but is compiled in the
                    //same run, and DLLGenerationHelper adds it by path.
                    Debug.Log($"[QBS] Assembly '{assemblyName}' isn't loaded; leaving it out of the references.");
                }
            }

            return resolvedPaths;
        }

        private static void CopyBuiltFiles(string from, string to)
        {
            if (!Directory.Exists(from))
            {
                return;
            }

            Directory.CreateDirectory(to);
            foreach (var file in Directory.GetFiles(from))
            {
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), overwrite: true);
            }
        }

        [Serializable]
        private class AssemblyReferencesData
        {
            public string[] RuntimeAssemblies;
            public string[] EditorAssemblies;
        }
    }
}
