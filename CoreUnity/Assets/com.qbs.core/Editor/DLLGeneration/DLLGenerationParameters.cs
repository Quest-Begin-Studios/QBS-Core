using System.Collections.Generic;

namespace QBS.Editor
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
		public bool KeepSources { get; }
		public bool IsSourceForEditor { get; }

		public DLLGenerationParameters(List<SourceFile> sources, string outputDLLPath, bool keepSources, bool isSourceForEditor)
		{
			Sources = sources;
			KeepSources = keepSources;
			IsSourceForEditor = isSourceForEditor;
			OutputDLLPath = outputDLLPath;
		}
	}

}