using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace QBS.Core.Editor
{
	public static class DLLGenerationHelper
	{
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
		public static bool TryGeneratingDLL(IEnumerable<SourceFile> sources, string outputDLLFolderName, string dllName, List<string> runtimeAssemblyReferences = null, List<string> editorAssemblyReferences = null, List<string> scriptingSymbols = null)
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
					GetPathForDLL(true, dllName, pathToPluginsFolder),
					typeof(FontStyle).Assembly.Location,
					typeof(Enumerable).Assembly.Location,
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

			var generationParameters = new DLLGenerationParameters(sources, pathToDLL, extraReferenceAssemblies, scriptingSymbols);

			var dllGenerator = new DLLGenerator(compileForRuntime);
			return dllGenerator.GenerateDLL(generationParameters);
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