using System;
using UnityEditor;
using UnityEngine;

namespace QBS.Core.Editor
{
	public static class LogEditorMenu
	{
		private const string MenuRoot = "Tools/QBS/Logging/";
		private const string ConfigureMenu = MenuRoot + "Configure Logging";

		[MenuItem(ConfigureMenu, false, 1)]
		private static void OpenLogConfiguration() => LogConfigWindow.ShowWindow();

		[InitializeOnLoadMethod]
		private static void InitializeLogSettings()
		{
			if (EditorPrefs.HasKey("QBS_Log_MinimumLevel"))
			{
				var level = (LogLevel)EditorPrefs.GetInt("QBS_Log_MinimumLevel");
				Log.MinimumLevel = level;
			}

			var channelCount = Enum.GetValues(typeof(LogChannel)).Length;
			for (var i = 0; i < channelCount; i++)
			{
				var channel = (LogChannel)i;
				var key = $"QBS_Log_Channel_{channel}";
				if (EditorPrefs.HasKey(key))
				{
					var enabled = EditorPrefs.GetBool(key);
					Log.SetChannelEnabled(channel, enabled);
				}
			}

			if (!LogConfigWindow.AreLogsEnabled())
			{
				Debug.LogError("QBS Logging is disabled. Please enable it in the Log Configuration window. \n Tools -> QBS -> Logging -> Configure Logging ");
			}
		}
	}
}