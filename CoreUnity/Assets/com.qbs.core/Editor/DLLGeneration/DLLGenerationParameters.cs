using JetBrains.Annotations;
using System.Collections.Generic;

namespace QBS.Core.Editor
{
	public struct SourceFile
	{
		public string SourceContent { get; }
		public string FilePath { get; }

		public SourceFile(string sourceContent, string filePath)
		{
			SourceContent = sourceContent;
			FilePath = filePath;
		}
	}

	public class DLLGenerationParameters
	{
		public List<SourceFile> Sources { get; }
		public string OutputDLLPath { get; }
		public List<string> ExtraAssembliesToReference { get; }
		[CanBeNull]
		public List<string> ScriptingSymbols { get; }

		public DLLGenerationParameters(List<SourceFile> sources, string outputDLLPath, List<string> extraAssembliesToReference = null, List<string> scriptingSymbols = null)
		{
			Sources = sources;
			OutputDLLPath = outputDLLPath;
			ExtraAssembliesToReference = extraAssembliesToReference ?? new List<string>();
			ScriptingSymbols = scriptingSymbols;
		}
	}

	public class DLLGenerationDiagnostic
	{
		public string Id { get; }
		public string File { get; }
		public int Line { get; }
		public int Column { get; }
		public string Message { get; }

		public DLLGenerationDiagnostic(string id, string file, int line, int column, string message)
		{
			Id = id;
			File = file;
			Line = line;
			Column = column;
			Message = message;
		}
	}

	public class DLLGenerationErrorDetails
	{
		public int ErrorCount { get; }
		public IReadOnlyList<DLLGenerationDiagnostic> Diagnostics { get; }

		public DLLGenerationErrorDetails(int errorCount, IList<DLLGenerationDiagnostic> diagnostics)
		{
			ErrorCount = errorCount;
			Diagnostics = (IReadOnlyList<DLLGenerationDiagnostic>)diagnostics;
		}
	}

}