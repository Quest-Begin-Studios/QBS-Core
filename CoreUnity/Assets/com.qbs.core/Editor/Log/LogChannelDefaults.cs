using System.Collections.Generic;

namespace QBS.Core.Editor
{
	/// <summary>
	///     The channels a generated LogChannel starts with. Treat the list as append-only: packages log to
	///     these by name against each consumer's own generated Log.dll, and removing, inserting before or
	///     reordering one renumbers the rest, repointing every channel mask already saved.
	/// </summary>
	public static class LogChannelDefaults
	{
		public static readonly List<string> BaseKeys = new()
		{
			"Network",
			"AI",
			"Physics",
			"UI",
			"Input",
			"SaveLoad",
			"Loading",
			"Gameplay",
			"Audio",
			"Rendering",
			"Auth",
			"Bridge",
			"Sfs",
			"Http",
			"Build",
		};

		public static readonly List<EnumGeneratorComponent.FlagCombinationEntry> FlagCombinations = new()
		{
			new EnumGeneratorComponent.FlagCombinationEntry
			{
				Name = "Core",
				Flags = new List<string> { "Input", "Gameplay" },
			},
			new EnumGeneratorComponent.FlagCombinationEntry
			{
				Name = "Presentation",
				Flags = new List<string> { "UI", "Rendering", "Audio" },
			},
			new EnumGeneratorComponent.FlagCombinationEntry
			{
				Name = "Simulation",
				Flags = new List<string> { "AI", "Physics", "Core" },
			},
		};
	}
}