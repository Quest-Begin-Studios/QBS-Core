using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QBS.Core.Editor
{
	[Flags]
	public enum EnumAttributeFlags
	{
		None = 0,
		Flags = 1 << 0,

		//TODO: Create EnumToStringNoBox Generators
		//StringEnum = 1 << 1,
	}

	public class EnumSourceGenerator : IDisposable
	{
		#region Consts
		
		private const string NamespaceTemplate = "namespace {0}";
		private const string EnumNameTemplate = "public enum {0} : {1}";
		private const string AttributesTemplate = "[{0}]";
		
		private const string EnumKeyTemplate = "{0},";
		private const string EnumKeyValueTemplate = "{0} = {1},";
		
		private const string EnumFlagsNothing = "None = 0,";
		private const string EnumFlagsEverything = "All = ~None,";
		
		#endregion
		
		private IndentedTextWriter _indentedWriter;

		/// <summary>
		/// Generates C# source code for an enum based on the provided parameters.
		/// </summary>
		/// <typeparam name="T">The backing type for the enum (must be an integral type).</typeparam>
		/// <param name="genParams">Parameters defining the enum structure, including name, namespace, attributes, and values.</param>
		/// <returns>A string containing the complete C# enum source code.</returns>
		public string CreateEnumSource<T>(EnumGenParams<T> genParams) where T : struct, IEquatable<T>, IComparable<T>
		{
			// Why is this like this?
			_indentedWriter = new IndentedTextWriter(new StringWriter(new StringBuilder()));
			if (!string.IsNullOrEmpty(genParams.Namespace))
			{
				_indentedWriter.WriteLine(NamespaceTemplate, genParams.Namespace);
				_indentedWriter.WriteLine("{");
				_indentedWriter.Indent++;
			}

			//TODO: Add more attribute support as required
			if ((genParams.Attributes & EnumAttributeFlags.Flags) == EnumAttributeFlags.Flags)
			{
				_indentedWriter.WriteLine(AttributesTemplate, "Flags");
			}
			
			_indentedWriter.WriteLine(EnumNameTemplate, genParams.EnumName, genParams.BackingType.Name);
			_indentedWriter.WriteLine("{");
			_indentedWriter.Indent++;
			
			if (genParams.EnumHasCustomValues)
			{
				WriteEnumWithCustomKeys(genParams.EnumKeysAndCustomValues);
			}
			else if((genParams.Attributes & EnumAttributeFlags.Flags) == EnumAttributeFlags.Flags)
			{
				WriteFlagsEnum(genParams.BaseEnumKeys, genParams.FlagCombinations);
			}
			else
			{
				WriteSimpleEnum(genParams.BaseEnumKeys);
			}
			
			_indentedWriter.Indent--;
			_indentedWriter.WriteLine("}");
			
			if (!string.IsNullOrEmpty(genParams.Namespace))
			{
				_indentedWriter.Indent--;
				_indentedWriter.WriteLine("}");
			}
			
			return _indentedWriter.InnerWriter.ToString();
		}

		private void WriteEnumWithCustomKeys<T>(Dictionary<string, T> enumWithCustomValues)
		{
			foreach (var (key, value) in enumWithCustomValues)
			{
				_indentedWriter.WriteLine(EnumKeyValueTemplate, key, value.ToString());
			}
		}
		
		private void WriteFlagsEnum(HashSet<string> baseEnumKeys, Dictionary<string, HashSet<string>> flagCombinations)
		{
			// Write the "None = 0" entry for flags enum
			_indentedWriter.WriteLine(EnumFlagsNothing);
			
			var index = 0;
			foreach (var baseEnumValues in baseEnumKeys)
			{
				// Calculate flag value as 2^index using bit shift (1 << index)
				var flagVal = 1 << index;
				_indentedWriter.WriteLine(EnumKeyValueTemplate, baseEnumValues, flagVal.ToString());
				index++;
			}
			
			// Write combination flags that combine multiple base flags using bitwise OR
			foreach (var (flagName, flagValues) in flagCombinations)
			{
				_indentedWriter.Write("{0} = ", flagName);
				var combinationCount = flagValues.Count;
				var currentCombinationCount = 0;
				
				foreach (var flagValue in flagValues)
				{
					// Check if this is the last flag in the combination
					if (currentCombinationCount >= combinationCount - 1)
					{
						_indentedWriter.Write("{0}", flagValue);
					}
					else
					{
						_indentedWriter.Write("{0} | ", flagValue);
					}
					currentCombinationCount++;
				}
				_indentedWriter.WriteLine(",");
			}
			
			// Write the "All = ~None" entry to represent all flags combined
			_indentedWriter.WriteLine(EnumFlagsEverything);
		}
		
		private void WriteSimpleEnum(HashSet<string> baseEnumValues)
		{
			foreach (var enumValue in baseEnumValues)
			{
				_indentedWriter.WriteLine(EnumKeyTemplate, enumValue);
			}
		}
		

		public void Dispose()
		{
			_indentedWriter = null;
		}
	}

}