using System.Collections.Generic;

namespace QBS.Core.Editor
{
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