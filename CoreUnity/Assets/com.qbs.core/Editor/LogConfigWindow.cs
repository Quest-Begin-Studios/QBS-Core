using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace QBS.Core.Editor
{
	public class LogConfigWindow : EditorWindow
	{
		private Vector2 _scrollPosition;
		private bool _logsEnabled;
		private LogLevel _minimumLevel;

		private const string EnableLogsSymbol = "ENABLE_LOGS";

		private GUIStyle _headerStyle;
		private GUIStyle _sectionStyle;
		private GUIStyle _boxStyle;

		public static void ShowWindow()
		{
			var window = GetWindow<LogConfigWindow>("Log Configuration");
			window.minSize = new Vector2(450, 700);
			window.Show();
		}

		private void OnEnable()
		{
			// TODO: Read from LogChannelDefines 

			// for (var i = 0; i < _channels.Length; i++)
			// {
			// 	_channelStates[i] = Log.IsChannelEnabled(_channels[i]);
			// }

			_minimumLevel = Log.MinimumLevel;
			_logsEnabled = AreLogsEnabled();
		}

		private void InitializeStyles()
		{
			if (_headerStyle == null)
			{
				_headerStyle = new GUIStyle(EditorStyles.largeLabel)
				{
					fontSize = 18,
					fontStyle = FontStyle.Bold,
					alignment = TextAnchor.MiddleCenter,
					margin = new RectOffset(0, 0, 10, 10),
				};
			}

			if (_sectionStyle == null)
			{
				_sectionStyle = new GUIStyle(EditorStyles.boldLabel)
				{
					fontSize = 13,
					margin = new RectOffset(5, 0, 5, 5),
				};
			}

			if (_boxStyle == null)
			{
				_boxStyle = new GUIStyle(GUI.skin.box)
				{
					padding = new RectOffset(10, 10, 10, 10),
					margin = new RectOffset(5, 5, 5, 5),
				};
			}
		}

		private void OnGUI()
		{
			InitializeStyles();

			EditorGUILayout.Space(15);
			EditorGUILayout.LabelField("QBS Log Configuration", _headerStyle);
			DrawSeparator();
			EditorGUILayout.Space(10);

			DrawEnableLogsSection();
			EditorGUILayout.Space(15);
			DrawMinimumLevelSection();
			EditorGUILayout.Space(15);
			DrawChannelsSection();
			EditorGUILayout.Space(10);
		}

		private void DrawSeparator()
		{
			var rect = EditorGUILayout.GetControlRect(false, 1);
			EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
		}

		private void DrawEnableLogsSection()
		{
			EditorGUILayout.BeginVertical(_boxStyle);

			EditorGUILayout.LabelField("⚙ Enable Logs", _sectionStyle);
			EditorGUILayout.Space(3);

			EditorGUILayout.HelpBox("Toggle the ENABLE_LOGS scripting define symbol. Logs are compiled out when disabled.",
				MessageType.Info);

			EditorGUILayout.Space(5);
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			var toggleColor = _logsEnabled ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
			var originalColor = GUI.backgroundColor;
			GUI.backgroundColor = toggleColor;

			var newLogsEnabled = GUILayout.Toggle(_logsEnabled,
				_logsEnabled ? "✓ Enabled" : "✗ Disabled",
				GUI.skin.button,
				GUILayout.Width(120),
				GUILayout.Height(30));

			GUI.backgroundColor = originalColor;

			if (newLogsEnabled != _logsEnabled)
			{
				_logsEnabled = newLogsEnabled;
				ToggleEnableLogs();
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space(5);

			EditorGUILayout.EndVertical();
		}

		private void DrawMinimumLevelSection()
		{
			EditorGUILayout.BeginVertical(_boxStyle);

			EditorGUILayout.LabelField("📊 Minimum Log Level", _sectionStyle);
			EditorGUILayout.Space(3);

			EditorGUILayout.HelpBox("Only logs at or above this level will be displayed.", MessageType.Info);

			EditorGUILayout.Space(5);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Current Level:", GUILayout.Width(100));

			var levelColor = GetLevelColor(_minimumLevel);
			var originalColor = GUI.backgroundColor;
			GUI.backgroundColor = levelColor;

			var newLevel = (LogLevel)EditorGUILayout.EnumPopup(_minimumLevel, GUILayout.Height(25));

			GUI.backgroundColor = originalColor;

			if (newLevel != _minimumLevel)
			{
				_minimumLevel = newLevel;
				SetMinimumLevel(newLevel);
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space(5);

			EditorGUILayout.EndVertical();
		}

		private void DrawChannelsSection()
		{
			EditorGUILayout.BeginVertical(_boxStyle);

			EditorGUILayout.LabelField("📡 Log Channels", _sectionStyle);
			EditorGUILayout.Space(3);

			EditorGUILayout.HelpBox("Enable or disable specific log channels. Disabled channels will not output logs.",
				MessageType.Info);

			EditorGUILayout.Space(5);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("✓ Enable All", GUILayout.Height(25)))
			{
				EnableAllChannels(true);
			}
			if (GUILayout.Button("✗ Disable All", GUILayout.Height(25)))
			{
				EnableAllChannels(false);
			}
			if (GUILayout.Button("↻ Reset", GUILayout.Height(25)))
			{
				ResetToDefaults();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(8);

			var scrollViewStyle = new GUIStyle(GUI.skin.scrollView)
			{
				padding = new RectOffset(5, 5, 5, 5),
			};

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, scrollViewStyle, GUILayout.Height(220));

			var enabledCount = 0;
			// for (var i = 0; i < _channels.Length; i++)
			// {
			// 	var channel = _channels[i];
			// 	var isEnabled = _channelStates[i];
			//
			// 	if (isEnabled)
			// 	{
			// 		enabledCount++;
			// 	}
			//
			// 	EditorGUILayout.BeginHorizontal();
			//
			// 	var channelColor = isEnabled ? Color.white : new Color(0.7f, 0.7f, 0.7f);
			// 	var originalColor = GUI.contentColor;
			// 	GUI.contentColor = channelColor;
			//
			// 	var icon = isEnabled ? "✓" : "○";
			// 	var newState = EditorGUILayout.ToggleLeft($"{icon} {channel}", isEnabled);
			//
			// 	GUI.contentColor = originalColor;
			//
			// 	if (newState != isEnabled)
			// 	{
			// 		_channelStates[i] = newState;
			// 		Log.SetChannelEnabled(channel, newState);
			// 		SaveChannelState(channel, newState);
			// 	}
			//
			// 	EditorGUILayout.EndHorizontal();
			// }

			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space(5);
			//EditorGUILayout.LabelField($"Active Channels: {enabledCount}/{_channels.Length}", EditorStyles.miniLabel);

			EditorGUILayout.EndVertical();
		}

		private Color GetLevelColor(LogLevel level) => level switch
		{
			LogLevel.Trace => new Color(0.7f, 0.7f, 0.7f),
			LogLevel.Debug => new Color(0.6f, 0.8f, 1f),
			LogLevel.Info => new Color(0.6f, 1f, 0.6f),
			LogLevel.Warning => new Color(1f, 0.9f, 0.4f),
			LogLevel.Error => new Color(1f, 0.5f, 0.4f),
			LogLevel.Fatal => new Color(1f, 0.3f, 0.3f),
			_ => Color.white,
		};

		private void ToggleEnableLogs()
		{
			var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
			var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
			var definesList = defines.Split(';').ToList();

			if (_logsEnabled)
			{
				if (!definesList.Contains(EnableLogsSymbol))
				{
					definesList.Add(EnableLogsSymbol);
				}
			}
			else
			{
				definesList.Remove(EnableLogsSymbol);
			}

			var newDefines = string.Join(";", definesList);
			PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newDefines);
			Debug.Log($"[QBS] Logging {(_logsEnabled ? "enabled" : "disabled")}");
		}

		public static bool AreLogsEnabled()
		{
			var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
			var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
			return defines.Contains(EnableLogsSymbol);
		}

		private void SetMinimumLevel(LogLevel level)
		{
			Log.MinimumLevel = level;
			EditorPrefs.SetInt(LogEditorConstants.MinimumLevel, (int)level);
			Debug.Log($"[QBS] Minimum log level set to: {level}");
		}

		private void EnableAllChannels(bool enabled)
		{
			// for (var i = 0; i < _channels.Length; i++)
			// {
			// 	_channelStates[i] = enabled;
			// 	Log.SetChannelEnabled(_channels[i], enabled);
			// 	SaveChannelState(_channels[i], enabled);
			// }
		}

		private void ResetToDefaults()
		{
			// for (var i = 0; i < _channels.Length; i++)
			// {
			// 	_channelStates[i] = true;
			// 	Log.SetChannelEnabled(_channels[i], true);
			// 	var key = LogEditorConstants.GetChannelKey(_channels[i]);
			// 	EditorPrefs.DeleteKey(key);
			// }
		}

		private void SaveChannelState(LogChannel channel, bool enabled)
		{
			var key = LogEditorConstants.GetChannelKey(channel);
			EditorPrefs.SetBool(key, enabled);
		}
	}
}