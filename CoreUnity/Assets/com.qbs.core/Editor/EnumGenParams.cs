using System;
using System.Collections.Generic;

namespace QBS.Core.Editor
{
	public struct EnumGenParams<T> where T : struct
	{
		
		private Type _backingType;
		private string _enumName;
		private EnumAttributeFlags _attributes;
		private string _namespace;

		// Collection for generic enum without custom values; can support Flags 
		private readonly HashSet<string> _baseEnumValues;
		// Collection for generic enum with custom values; cannot support Flags
		private readonly Dictionary<string, T> _enumCustomValues;
		// Flag Combinations
		private readonly Dictionary<string, HashSet<string>> _flagCombinations;
		
		#region Public Accessors
		
		public Type BackingType => _backingType;
		public string EnumName => _enumName;
		public EnumAttributeFlags Attributes => _attributes;
		public string Namespace => _namespace;
		public HashSet<string> BaseEnumValues => _baseEnumValues;
		public Dictionary<string, T> EnumCustomValues => _enumCustomValues;
		public Dictionary<string, HashSet<string>> FlagCombinations => _flagCombinations;
		
		#endregion

		/// <summary>
		///     Initializes a new instance of the <see cref="EnumGenParams{T}" /> struct that can support enums with custom values.
		/// </summary>
		/// <param name="enumName">The name of the enum to generate.</param>
		/// <param name="attributes">The attributes to apply to the enum. Flags attribute is not supported for custom values.</param>
		/// <param name="enumCustomValues">A dictionary mapping enum member names to their custom values.</param>
		/// <param name="namespace">The namespace to apply to the enum.</param>
		/// <exception cref="ArgumentException">
		///     Thrown when Flags attribute is specified or when the backing type is not an
		///     integral type.
		/// </exception>
		public EnumGenParams(string enumName, EnumAttributeFlags attributes, Dictionary<string, T> enumCustomValues, string @namespace = null) : this()
		{
			if (attributes.HasFlag(EnumAttributeFlags.Flags))
			{
				throw new ArgumentException("Flags attribute is not supported for Enums with custom values");
			}

			_enumName = enumName;
			_namespace = @namespace;
			_backingType = typeof(T);
			_attributes = attributes;
			_enumCustomValues = enumCustomValues;

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
		/// <param name="baseEnumValues">A set of base enum member names.</param>
		/// <param name="flagCombinations">
		///     Optional dictionary mapping combination flag names to sets of base flag names they
		///     combine. Only used when Flags attribute is set.
		/// </param>
		/// <exception cref="ArgumentException">
		///     Thrown when the backing type is not an integral type or when flag combinations
		///     reference non-existent base values.
		/// </exception>
		public EnumGenParams(string enumName, EnumAttributeFlags attributes, HashSet<string> baseEnumValues, string @namespace = null, Dictionary<string, HashSet<string>> flagCombinations = null) : this()
		{
			_enumName = enumName;
			_namespace = @namespace;
			_attributes = attributes;
			
			_baseEnumValues = baseEnumValues;

			_backingType = typeof(T);
			if (attributes.HasFlag(EnumAttributeFlags.Flags))
			{
				_flagCombinations = flagCombinations;
				ValidateFlagCombinations();
			}
			else
			{
				_flagCombinations = null;
			}

			if (!ValidateType())
			{
				throw new ArgumentException("Value must be of a integral type");
			}
		}

		private bool ValidateFlagCombinations()
		{
			if (_flagCombinations == null)
			{
				return true;
			}

			foreach (var (flagName, combination) in _flagCombinations)
			{
				foreach (var enumValue in combination)
				{
					if (!_baseEnumValues.Contains(enumValue))
					{
						throw new ArgumentException(
							$"A combination flag definition {flagName} is trying to reference {enumValue}, but it does not exist in the base flags definition");
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