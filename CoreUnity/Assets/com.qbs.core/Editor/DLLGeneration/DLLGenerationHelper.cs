using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace QBS.Editor
{
	public static class DLLGenerationHelper
	{
		private const string EditorFolderAddress = "Editor";
		private const string RuntimeFolderAddress = "Runtime";

		private const string DLLExtension = ".dll";
		private const string EditorDLLSuffix = "-Editor";
		private const string PluginsFolderName = "Plugins";

		/// <summary>
		/// </summary>
		/// <param name="sources"></param>
		/// <param name="outputDLLFolderName">Folder created under the Plugins folder to house the generated DLLs</param>
		/// <param name="dllName"></param>
		/// <returns></returns>
		public static bool TryGeneratingDLL(IEnumerable<SourceFile> sources, string outputDLLFolderName, string dllName)
		{
			var editorSources = new List<SourceFile>();
			var runtimeSources = new List<SourceFile>();

			var pathToPluginsFolder = Path.Combine(Application.dataPath, PluginsFolderName, outputDLLFolderName);
			var pathToRuntimeFolder = Path.Combine(pathToPluginsFolder, RuntimeFolderAddress);
			var pathToEditorFolder = Path.Combine(pathToPluginsFolder, EditorFolderAddress);

			// Group sources by lifetime: runtime or editor.
			foreach (var sourceFile in sources)
			{
				var usingStatements = GetUsingStatementsFromSource(sourceFile.SourceContent);
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

			if (!Directory.Exists(pathToRuntimeFolder))
			{
				Directory.CreateDirectory(pathToRuntimeFolder);
			}

			if (!Directory.Exists(pathToEditorFolder))
			{
				Directory.CreateDirectory(pathToEditorFolder);
			}

			// Compile runtime sources first,
			// since editor sources will always be dependent on runtime sources.
			var runtimeDllName = string.Concat(dllName, DLLExtension);
			var editorDllName = dllName.Concat(EditorDLLSuffix).Concat(DLLExtension).ToString();

			var runtimeAssemblyLocation = Path.Combine(pathToPluginsFolder, RuntimeFolderAddress, runtimeDllName);
			var runtimeGenParameters = new DLLGenerationParameters(runtimeSources, runtimeAssemblyLocation, true, false);

			return DLLGenerator.GenerateDLL(runtimeGenParameters);
		}

		/// <summary>
		///     Extracts using statements from a C# file using Roslyn syntax tree analysis.
		/// </summary>
		/// <param name="filePath">Path to the C# file</param>
		/// <returns>Array of using directive namespace names</returns>
		public static string[] GetUsingStatementsFromFile(string filePath)
		{
			if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
			{
				return Array.Empty<string>();
			}

			var sourceCode = File.ReadAllText(filePath);
			return GetUsingStatementsFromSource(sourceCode);
		}

		/// <summary>
		///     Extracts using statements from C# source code using Roslyn syntax tree analysis.
		/// </summary>
		/// <param name="sourceCode">C# source code as string</param>
		/// <returns>Array of using directive namespace names</returns>
		public static string[] GetUsingStatementsFromSource(string sourceCode)
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
	}
}