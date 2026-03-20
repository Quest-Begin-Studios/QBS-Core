using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

namespace QBS.Core.Editor
{
	public class StringEnumGenerator
	{
		public string GenerateNoBoxStringsFromType<T>() where T : Enum
		{
			var enumTypeName = typeof(T).Name;
			var enumValues = Enum.GetNames(typeof(T));
			
			return GenerateNoBoxStringsFromSource(enumTypeName, enumValues, typeof(T).Namespace);
		}

		public string GenerateNoBoxStringsFromSource(string enumTypeName, IEnumerable<string> enumKeys, string namespaceName = null)
		{
			const string paramName = "value";
			var className = $"{enumTypeName}StringUtils";
			
			using var textWriter = new IndentedTextWriter(new StringWriter());
			
			if (!string.IsNullOrEmpty(namespaceName))
			{
				textWriter.WriteLine($"namespace {namespaceName}");
				textWriter.WriteLine("{");
				textWriter.Indent++;
			}
				
			// Class declaration
			textWriter.WriteLine($"public static class {className}");
			textWriter.WriteLine("{");
			textWriter.Indent++;
				
			// Extension method
			textWriter.WriteLine($"public static string ToStringNoBox(this {enumTypeName} {paramName})");
			textWriter.WriteLine("{");
			textWriter.Indent++;
				
			// Switch expression
			textWriter.WriteLine($"return {paramName} switch");
			textWriter.WriteLine("{");
			textWriter.Indent++;
				
			foreach (var enumName in enumKeys)
			{
				textWriter.WriteLine($"{enumTypeName}.{enumName} => \"{enumName}\",");
			}
				
			textWriter.WriteLine("_ => \"\",");
			textWriter.Indent--;
			textWriter.WriteLine("};");
				
			// Close method
			textWriter.Indent--;
			textWriter.WriteLine("}");
				
			// Close class
			textWriter.Indent--;
			textWriter.WriteLine("}");
				
			// Close namespace
			if (!string.IsNullOrEmpty(namespaceName))
			{
				textWriter.Indent--;
				textWriter.WriteLine("}");
			}
				
			return textWriter.InnerWriter.ToString();
		}
	}
}