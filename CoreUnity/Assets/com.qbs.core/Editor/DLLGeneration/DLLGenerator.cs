using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace QBS.Core.Editor
{
	public class DLLGenerator : IDisposable
	{
		private List<string> _assembliesToReference = new();

		public bool GenerateDLL(DLLGenerationParameters genParams)
		{
			if (genParams.Sources == null || genParams.Sources.Count == 0)
			{
				throw new ArgumentException("Sources Array is null or empty");
			}

			if (genParams.ExtraAssembliesToReference != null)
			{
				_assembliesToReference.AddRange(genParams.ExtraAssembliesToReference);
			}

			// Parse all source code into syntax trees
			var syntaxTrees = new List<SyntaxTree>();
			var parseOptions = CSharpParseOptions.Default
				.WithLanguageVersion(LanguageVersion.CSharp9)
				.WithPreprocessorSymbols(genParams.ScriptingSymbols);

			foreach (var sourceFile in genParams.Sources)
			{
				var syntaxTree = CSharpSyntaxTree.ParseText
				(
					sourceFile.SourceContent,
					parseOptions,
					sourceFile.FilePath,
					Encoding.UTF8
				);

				syntaxTrees.Add(syntaxTree);
			}

			// Automatically resolve assembly references from loaded assemblies
			var references = ResolveAssemblyReferences();

			// Determine assembly name from output path
			var assemblyName = Path.GetFileNameWithoutExtension(genParams.OutputDLLPath);

			// Create compilation options
			var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
				.WithOptimizationLevel(OptimizationLevel.Release)
				.WithPlatform(Platform.AnyCpu);

			// Create the compilation
			var compilation = CSharpCompilation.Create
			(
				assemblyName,
				syntaxTrees,
				references,
				compilationOptions
			);

			// Emit the DLL
			var emitOptions = new EmitOptions
			(
				debugInformationFormat: DebugInformationFormat.PortablePdb,
				defaultSourceFileEncoding: Encoding.UTF8
			);

			EmitResult result;
			using (var dllStream = new FileStream(genParams.OutputDLLPath!, FileMode.Create))
			{
				var pdbPath = Path.ChangeExtension(genParams.OutputDLLPath, ".pdb");
				using var pdbStream = new FileStream(pdbPath, FileMode.Create);
				result = compilation.Emit(dllStream, pdbStream, options: emitOptions);
			}

			if (result.Success)
			{
				return true;
			}

			// Handle compilation errors
			var failures = result.Diagnostics.Where
			(diagnostic =>
				diagnostic.IsWarningAsError ||
				diagnostic.Severity == DiagnosticSeverity.Error
			);

			var sb = new StringBuilder();
			var diagnostics = failures as Diagnostic[] ?? failures.ToArray();
			sb.AppendLine($"Compilation failed with {diagnostics.Length} error(s):");
			sb.AppendLine("=".PadRight(80, '='));

			foreach (var diagnostic in diagnostics)
			{
				var lineSpan = diagnostic.Location.GetLineSpan();
				sb.AppendLine($"\n[ERROR] {diagnostic.Id}");
				sb.AppendLine($"File: {lineSpan.Path}");
				sb.AppendLine($"Location: Line {lineSpan.StartLinePosition.Line + 1}, Column {lineSpan.StartLinePosition.Character + 1}");
				sb.AppendLine($"Message: {diagnostic.GetMessage()}");
				sb.AppendLine("-".PadRight(80, '-'));
			}
			
			//Also purge all written files in case of compilation failure
			if (File.Exists(genParams.OutputDLLPath))
				File.Delete(genParams.OutputDLLPath);

			var pdbPathToDelete = Path.ChangeExtension(genParams.OutputDLLPath, ".pdb");
			if (File.Exists(pdbPathToDelete))
				File.Delete(pdbPathToDelete);

			foreach (var files in genParams.Sources)
			{
				File.WriteAllText(Path.Combine(Application.temporaryCachePath, files.FilePath), files.SourceContent);
			}

			Debug.Log($"Compilation failed; Writing generator input to folder: {Application.temporaryCachePath}");
			throw new Exception(sb.ToString());
		}

		private List<MetadataReference> ResolveAssemblyReferences()
		{
			var references = new List<MetadataReference>();
			foreach (var assemblyPath in _assembliesToReference)
			{
				references.Add(MetadataReference.CreateFromFile(assemblyPath));
			}

			return references;
		}

		public void Dispose()
		{
			_assembliesToReference?.Clear();
			_assembliesToReference = null;
		}
	}
}