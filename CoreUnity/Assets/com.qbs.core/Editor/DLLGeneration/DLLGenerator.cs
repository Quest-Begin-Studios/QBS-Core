using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;

namespace QBS.Editor
{

	public static class DLLGenerator
	{
		public static bool PackSourcesIntoDLL(DLLGenerationParameters parameters)
		{
			if (parameters.Sources == null || parameters.Sources.Length == 0)
			{
				throw new ArgumentException("Sources Array is null or empty");
			}

			var providerOptions = new Dictionary<string, string>
			{
				//Using version 9.0 as Unity only supports C# 9.0 out of the box
				{ "LangVersion", "9.0" },
				{ "CompilerVersion", "v4.0" },
			};

			var provider = new CSharpCodeProvider(providerOptions);
			var compilerParameters = new CompilerParameters
			{
				GenerateExecutable = false,
				OutputAssembly = parameters.OutputDLLPath,
				
				GenerateInMemory = parameters.Debug,
				TreatWarningsAsErrors = parameters.Debug,
			};
			
			foreach (var assemblyLocation in parameters.AssemblyLocations)
			{
				compilerParameters.ReferencedAssemblies.Add(assemblyLocation);
			}

			var compilationResults = provider.CompileAssemblyFromSource(compilerParameters, parameters.Sources);

			if (compilationResults.Errors.Count <= 0)
			{
				return true;
			}
			
			var sb = new StringBuilder();
			foreach (CompilerError error in compilationResults.Errors)
			{
				sb.AppendLine($"{error.ErrorNumber}: {error.ErrorText} \n");
			}
				
			throw new Exception($"Compilation failed:\n{sb}");
		}
	}
}