using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Object = UnityEngine.Object;

namespace QBS.Editor
{

	public static class DLLGenerator
	{
		public static bool GenerateDLL(DLLGenerationParameters genParams)
		{
			if (genParams.Sources == null || genParams.Sources.Count == 0)
			{
				throw new ArgumentException("Sources Array is null or empty");
			}

			// Parse all source code into syntax trees
			var syntaxTrees = new List<SyntaxTree>();
			foreach (var sourceFile in genParams.Sources)
			{
				var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);
				var syntaxTree = CSharpSyntaxTree.ParseText(sourceFile.SourceContent, parseOptions, sourceFile.FilePath, Encoding.UTF8);
				syntaxTrees.Add(syntaxTree);
			}

			// Automatically resolve assembly references from loaded assemblies
			var references = ResolveAssemblyReferences(genParams.Sources, genParams.IsSourceForEditor);

			// Determine assembly name from output path
			var assemblyName = Path.GetFileNameWithoutExtension(genParams.OutputDLLPath);

			// Create compilation options
			var compilationOptions = new CSharpCompilationOptions(OutputKind.WindowsRuntimeMetadata)
				.WithOptimizationLevel(OptimizationLevel.Debug)
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
				if (genParams.KeepSources)
				{
					var pdbPath = Path.ChangeExtension(genParams.OutputDLLPath, ".pdb");
					using var pdbStream = new FileStream(pdbPath, FileMode.Create);
					result = compilation.Emit(dllStream, pdbStream, options: emitOptions);
				}
				else
				{
					result = compilation.Emit(dllStream, options: emitOptions);
				}
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

			throw new Exception(sb.ToString());
		}

		private static List<MetadataReference> ResolveAssemblyReferences(List<SourceFile> genParamsSources, bool genParamsIsSourceForEditor)
		{
			var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

			var netStandardAssembly = allAssemblies.FirstOrDefault
				(assembly => assembly.FullName.Contains("netstandard", StringComparison.InvariantCultureIgnoreCase));

			var references = new List<MetadataReference>
			{
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(Object).Assembly.Location),
				MetadataReference.CreateFromFile(netStandardAssembly.Location),
				//MetadataReference.CreateFromFile(),
			};

			return references;
		}
	}
}