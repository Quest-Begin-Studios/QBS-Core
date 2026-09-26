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
		// Flags pinned to bits instead of numbered by position; BaseEnumKeys then holds their names
		public IReadOnlyList<FlagSegment> FlagSegments { get; }

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

		
		/// <summary>
		///     Initializes a new instance of the <see cref="EnumGenParams{T}" /> struct for a flags enum whose base
		///     values sit on fixed bits, laid out by <paramref name="flagSegments" /> rather than by position.
		/// </summary>
		/// <param name="enumName">The name of the enum to generate.</param>
		/// <param name="attributes">The attributes to apply to the enum. Must include Flags.</param>
		/// <param name="flagSegments">
		///     The runs of base flags and the bits they start from. Two segments may name the same flag on the
		///     same bit, which is then written once.
		/// </param>
		/// <param name="namespace">The namespace the enum needs to be in.</param>
		/// <param name="flagCombinations">
		///     Optional dictionary mapping combination flag names to sets of base flag names they combine.
		/// </param>
		/// <exception cref="ArgumentException">
		///     Thrown when Flags is not set, when the backing type is not an integral type, when a flag lands
		///     outside the backing type or on a bit another flag holds, or when a flag combination references
		///     a non-existent value.
		/// </exception>
		public EnumGenParams(string enumName, EnumAttributeFlags attributes, IReadOnlyList<FlagSegment> flagSegments,
			string @namespace = null, Dictionary<string, HashSet<string>> flagCombinations = null) : this()
		{
			if (!attributes.HasFlag(EnumAttributeFlags.Flags))
			{
				throw new ArgumentException("Flag segments are only supported for Enums with the Flags attribute");
			}

			EnumName = enumName;
			Namespace = @namespace;
			BackingType = typeof(T);
			EnumHasCustomValues = false;

			Attributes = attributes;
			FlagSegments = flagSegments;
			BaseEnumKeys = new HashSet<string>();
			FlagCombinations = flagCombinations;

			if (!ValidateType())
			{
				throw new ArgumentException("Value must be of a integral type");
			}

			ValidateFlagSegments();
			ValidateFlagCombinations();
		}

		private void ValidateFlagSegments()
		{
			// Same reasoning as ValidateEnumSize: the top bit of a signed backing field is its sign.
			var highestBit = (Marshal.SizeOf<T>() * 8) - 2;
			var flagsByBit = new Dictionary<int, string>();
			var bitsByFlag = new Dictionary<string, int>();

			foreach (var segment in FlagSegments)
			{
				for (var i = 0; i < segment.Keys.Count; i++)
				{
					var key = segment.Keys[i];
					var isRetired = string.IsNullOrWhiteSpace(key);
					var label = isRetired ? "A retired flag" : key;
					var bit = segment.BitAt(i);

					if (bit < 0 || bit > highestBit)
					{
						throw new ArgumentException($"{label} lands on bit {bit}, outside the 0 to {highestBit} the backing type has room for");
					}

					if (!isRetired && bitsByFlag.TryGetValue(key, out var existingBit))
					{
						// The same flag on the same bit is shared; on two bits the segments contradict each other.
						if (existingBit != bit)
						{
							throw new ArgumentException($"{key} is placed on both bit {existingBit} and bit {bit}");
						}

						continue;
					}

					if (flagsByBit.TryGetValue(bit, out var existingFlag))
					{
						throw new ArgumentException($"{label} and {existingFlag} both claim bit {bit}");
					}

					flagsByBit[bit] = label;
					if (!isRetired)
					{
						bitsByFlag[key] = bit;
						BaseEnumKeys.Add(key);
					}
				}
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