using System;

namespace QBS.Core.Editor
{

	[Flags]
	public enum EnumUtilities
	{
		None = 0,
		GenerateToStringFast,
		GenerateHasFlagFast,
		GenerateEnumValuesArray,
		All = ~None,
	}
	
	/// <summary>
	/// Marker Attribute for all enums that need a ToStringNoBox method generated
	/// TODO: Create custom value support for each enum key.
	/// </summary>
	public class EnumUtilsAttribute : Attribute
	{
		public EnumUtilities GenerationOptions { get; }


		public EnumUtilsAttribute(EnumUtilities generationOptions = EnumUtilities.GenerateToStringFast)
		{
			GenerationOptions = generationOptions;
		}
	}
}