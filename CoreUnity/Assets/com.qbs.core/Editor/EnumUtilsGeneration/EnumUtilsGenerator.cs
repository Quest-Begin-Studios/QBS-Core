using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QBS.Core.Editor
{
	public class EnumUtilsGenerator
	{
		public string GenerateFromType<T>(EnumUtilities generationOptions) where T : Enum
		{
			return Generate(generationOptions, typeof(T).Name, Enum.GetNames(typeof(T)), typeof(T).Namespace);
		}

		public string Generate(EnumUtilities generationOptions, string enumTypeName, IEnumerable<string> enumKeys, string namespaceName = null)
		{
			
			using var textWriter = new IndentedTextWriter(new StringWriter());

			if (!string.IsNullOrEmpty(namespaceName))
			{
				textWriter.WriteLine($"namespace {namespaceName}");
				textWriter.WriteLine("{");
				textWriter.Indent++;
			}

			// Class declaration
			var className = $"{enumTypeName}StringUtils";
			textWriter.WriteLine($"public static class {className}");
			textWriter.WriteLine("{");
			textWriter.Indent++;

			var enumKeyArray = enumKeys as string[] ?? enumKeys.ToArray();
			if (generationOptions.HasFlagFast(EnumUtilities.GenerateEnumValuesArray))
			{
				WriteEnumValuesArray(textWriter, enumTypeName, enumKeyArray);
			}
			if (generationOptions.HasFlagFast(EnumUtilities.GenerateToStringFast))
			{
				WriteToStringFast(textWriter, enumTypeName, enumKeyArray);
			}
			if (generationOptions.HasFlagFast(EnumUtilities.GenerateHasFlagFast))
			{
				WriteHasFlagFast(textWriter, enumTypeName, enumKeyArray);
			}

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

		private static void WriteToStringFast(IndentedTextWriter writer, string enumTypeName, IEnumerable<string> enumKeys)
		{
			const string paramName = "value";
			
			// Extension method
			writer.WriteLine($"public static string ToStringFast(this {enumTypeName} {paramName})");
			writer.WriteLine("{");
			writer.Indent++;
				
			// Switch expression
			writer.WriteLine($"return {paramName} switch");
			writer.WriteLine("{");
			writer.Indent++;
				
			foreach (var enumName in enumKeys)
			{
				writer.WriteLine($"{enumTypeName}.{enumName} => \"{enumName}\",");
			}
			
			writer.WriteLine($"_ => throw new System.ArgumentException($\"Invalid {enumTypeName} value: {{value}}\"),");
			writer.Indent--;
			writer.WriteLine("};");
				
			// Close method
			writer.Indent--;
			writer.WriteLine("}");
		}


		private void WriteEnumValuesArray(IndentedTextWriter textWriter, string enumTypeName, IEnumerable<string> enumKeys)
		{
			
		}
		
		private void WriteHasFlagFast(IndentedTextWriter textWriter, string enumTypeName, string[] enumKeyArray)
		{
			
		}
	}
	
	public static class EnumUtilitiesExtensions
	{
		public static bool HasFlagFast(this EnumUtilities generationOptions, EnumUtilities flag)
		{
			return (generationOptions & flag) == flag;
		}

		// public static readonly ImmutableArray<EnumUtilities> EnumUtilitiesArray = new()
		// {
		// 	EnumUtilities.None, 
		// 	EnumUtilities.GenerateToStringFast, 
		// 	EnumUtilities.GenerateHasFlagFast, 
		// 	EnumUtilities.GenerateEnumValuesArray,
		// 	EnumUtilities.All,
		// };

		public static string ToStringFast(this EnumUtilities value)
		{
			return value switch
			{
				EnumUtilities.None => nameof(EnumUtilities.None),
				EnumUtilities.GenerateToStringFast => nameof(EnumUtilities.GenerateToStringFast),
				EnumUtilities.GenerateHasFlagFast => nameof(EnumUtilities.GenerateHasFlagFast),
				EnumUtilities.GenerateEnumValuesArray => nameof(EnumUtilities.GenerateEnumValuesArray),
				EnumUtilities.All => nameof(EnumUtilities.All),
				_ => throw new ArgumentException($"Invalid EnumUtilities value: {value}"),
			};
		}
	}
}