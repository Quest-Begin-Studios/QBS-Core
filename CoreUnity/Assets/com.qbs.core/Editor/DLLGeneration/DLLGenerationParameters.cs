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

		[CanBeNull]
		public List<string> ExtraAssembliesToReference { get; }
		
		[CanBeNull]
		public List<string> ScriptingSymbols { get; }

		public DLLGenerationParameters(List<SourceFile> sources, string outputDLLPath, List<string> extraAssembliesToReference = null, List<string> scriptingSymbols = null)
		{
			Sources = sources;
			OutputDLLPath = outputDLLPath;
			ExtraAssembliesToReference = extraAssembliesToReference;
			ScriptingSymbols = scriptingSymbols;
		}
	}

}