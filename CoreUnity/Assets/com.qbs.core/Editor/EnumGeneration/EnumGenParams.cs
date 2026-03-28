using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace QBS.Core.Editor
{
	public struct EnumGenParams<T> where T : struct
	{
		public string EnumName { get; }
		public Type BackingType { get; }
		public string Namespace { get; }
		public bool EnumHasCustomValues { get; }
		public EnumAttributeFlags Attributes { get; }
		
		// Collection for generic enum without custom values; can support Flags 
		public HashSet<string> BaseEnumKeys { get; }
		// Flag Combinations
		public Dictionary<string, HashSet<string>> FlagCombinations { get; }
		
		// Collection for generic enum with custom values; cannot support Flags
		public Dictionary<string, T> EnumKeysAndCustomValues { get; }

		/// <summary>
		///     Initializes a new instance of the <see cref="EnumGenParams{T}" /> struct that can support enums with custom values.
		/// </summary>
		/// <param name="enumName">The name of the enum to generate.</param>
		/// <param name="attributes">The attributes to apply to the enum. Flags attribute is not supported for custom values.</param>
		/// <param name="enumKeysAndCustomValues">A dictionary mapping enum member names to their custom values.</param>
		/// <param name="namespace">The namespace to apply to the enum.</param>
		/// <exception cref="ArgumentException">
		///     Thrown when Flags attribute is specified or when the backing type is not an
		///     integral type.
		/// </exception>
		public EnumGenParams(string enumName, EnumAttributeFlags attributes, Dictionary<string, T> enumKeysAndCustomValues, string @namespace = null) : this()
		{
			if (attributes.HasFlag(EnumAttributeFlags.Flags))
			{
				throw new ArgumentException("Flags attribute is not supported for Enums with custom values");
			}

			EnumName = enumName;
			Namespace = @namespace;
			BackingType = typeof(T);
			EnumHasCustomValues = true;
			
			Attributes = attributes;
			EnumKeysAndCustomValues = enumKeysAndCustomValues;
			
			if (!ValidateType())
			{
				throw new ArgumentException("Value must be of a integral type");
			}
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="EnumGenParams{T}" /> struct with base enum values and optional flag
		///     combinations.
		/// </summary>
		/// <param name="enumName">The name of the enum to generate.</param>
		/// <param name="attributes">The attributes to apply to the enum.</param>
		/// <param name="baseEnumKeys">A set of base enum member names.</param>
		/// <param name="namespace">The namespace the enum needs to be in.</param>
		/// <param name="flagCombinations">
		///     Optional dictionary mapping combination flag names to sets of base flag names they
		///     combine. Only used when Flags attribute is set.
		/// </param>
		/// <exception cref="ArgumentException">
		///     Thrown when the backing type is not an integral type or when flag combinations
		///     reference non-existent base values.
		/// </exception>
		public EnumGenParams(string enumName, EnumAttributeFlags attributes, HashSet<string> baseEnumKeys, string @namespace = null,
			Dictionary<string, HashSet<string>> flagCombinations = null) : this()
		{
			EnumName = enumName;
			Namespace = @namespace;
			BackingType = typeof(T);
			EnumHasCustomValues = false;
			
			Attributes = attributes;
			BaseEnumKeys = baseEnumKeys;

			if (attributes.HasFlag(EnumAttributeFlags.Flags))
			{
				FlagCombinations = flagCombinations;
				ValidateFlagCombinations();
				ValidateEnumSize();
			}
			else
			{
				FlagCombinations = null;
			}

			if (!ValidateType())
			{
				throw new ArgumentException("Value must be of a integral type");
			}
		}

		
		private void ValidateEnumSize()
		{
			// We're subtracting -1 because if the backing field is signed,
			// the last bit is counted as negative; This leads to some bad maths.
			var totalBitsAvailable = (Marshal.SizeOf<T>() * 8) - 1;
			var totalFlags = BaseEnumKeys.Count;
			if (totalFlags > totalBitsAvailable)
			{
				throw new ArgumentException("Too many flags for the backing type");
			}
		}

		private bool ValidateFlagCombinations()
		{
			if (FlagCombinations == null)
			{
				return true;
			}

			foreach (var (flagName, combination) in FlagCombinations)
			{
				foreach (var enumValue in combination)
				{
					if (string.CompareOrdinal(flagName, enumValue) == 0)
					{
						throw new ArgumentException("A composite flag cannot be defined as a combination of itself ");
					}
					
					if (!BaseEnumKeys.Contains(enumValue) && !FlagCombinations.ContainsKey(enumValue))
					{
						throw new ArgumentException( @$"A combination flag definition {flagName} is trying to reference 
							{enumValue}, but it does not exist in the base flags or other combination flags definitions");
					}
				}
			}

			return true;
		}

		private bool ValidateType()
		{
			return typeof(T) == typeof(int) ||
				typeof(T) == typeof(uint) ||
				typeof(T) == typeof(long) ||
				typeof(T) == typeof(ulong) ||
				typeof(T) == typeof(short) ||
				typeof(T) == typeof(ushort) ||
				typeof(T) == typeof(byte) ||
				typeof(T) == typeof(sbyte);
		}
	}
}