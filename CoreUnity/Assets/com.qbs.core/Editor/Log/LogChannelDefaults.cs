using System.Collections.Generic;

namespace QBS.Core.Editor
{
	/// <summary>
	///     The channels every generated LogChannel carries. Packages log to these by name against the
	///     consumer's own generated Log.dll, so the set is append-only: a game may add channels after them
	///     and must not remove, insert before or reorder one, which would renumber the rest and repoint
	///     every mask already saved against the old numbering.
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