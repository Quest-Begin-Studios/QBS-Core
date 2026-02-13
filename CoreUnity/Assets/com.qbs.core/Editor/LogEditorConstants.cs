namespace QBS.Core.Editor
{
	public static class LogEditorConstants
	{
		public const string MinimumLevel = "QBS_Log_MinimumLevel";
		public const string LogsDisabledWarningShown = "QBS_Logs_Disabled_Warning_Shown";
		
		public static string GetChannelKey(LogChannel channel) => $"QBS_Log_Channel_{channel}";

		public static string DefaultLogChannel = "Default";
		//To be used when no channels have been setup by the user
		public static string[] DefaultChannels =
		{
			"Default",
			"Gameplay",
			"Physics",
			"Animation",
			"Audio",
			"UI",
		};
	}
}
