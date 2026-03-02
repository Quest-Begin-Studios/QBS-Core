using System.Collections.Generic;
using UnityEditor.Compilation;

namespace QBS.Editor
{
	public class DLLGenerationParameters
	{
		public string[] Sources { get; set; }
		public List<string> AssemblyLocations { get; set; }
		public string OutputDLLPath { get; set; }
		public bool Debug { get; set; }
	}

}