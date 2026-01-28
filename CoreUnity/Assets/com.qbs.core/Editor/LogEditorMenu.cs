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
			RestoreMinimumLogLevel();
			RestoreActiveLogChannels();
			WarnUserAboutInactiveLogs();
			
			EditorApplication.quitting -= OnQuit;
			EditorApplication.quitting += OnQuit;
		}

		private static void WarnUserAboutInactiveLogs()
		{

			if (!LogConfigWindow.AreLogsEnabled() && !EditorPrefs.GetBool(LogEditorPrefs.LogsDisabledWarningShown, false))
			{
				Debug.LogError(
					"QBS Logging is disabled. Enable it in the "
					+ "Log Configuration window to utilize the logging utility."
					+ " \nTools -> QBS -> Logging -> Configure Logging ");
				
				EditorPrefs.SetBool(LogEditorPrefs.LogsDisabledWarningShown, true);
			}
		}

		private static void RestoreActiveLogChannels()
		{
			var channelCount = Enum.GetValues(typeof(LogChannel)).Length;
			for (var i = 0; i < channelCount; i++)
			{
				var channel = (LogChannel)i;
				var key = LogEditorPrefs.GetChannelKey(channel);
				if (EditorPrefs.HasKey(key))
				{
					var enabled = EditorPrefs.GetBool(key);
					Log.SetChannelEnabled(channel, enabled);
				}
			}
		}

		private static void RestoreMinimumLogLevel()
		{
			if (EditorPrefs.HasKey(LogEditorPrefs.MinimumLevel))
			{
				var level = (LogLevel)EditorPrefs.GetInt(LogEditorPrefs.MinimumLevel);
				Log.MinimumLevel = level;
			}
		}

		private static void OnQuit()
		{
			EditorPrefs.SetBool(LogEditorPrefs.LogsDisabledWarningShown, false);
		}
	}
}