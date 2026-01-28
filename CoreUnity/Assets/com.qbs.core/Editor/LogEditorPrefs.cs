namespace QBS.Core.Editor
{
	public static class LogEditorPrefs
	{
		public const string MinimumLevel = "QBS_Log_MinimumLevel";
		public const string ChannelPrefix = "QBS_Log_Channel_";
		public const string LogsDisabledWarningShown = "QBS_Logs_Disabled_Warning_Shown";
		
		public static string GetChannelKey(LogChannel channel) => $"{ChannelPrefix}{channel}";
	}
}
