using System.Linq;
using UnityEditor;
using UnityEngine;

namespace QBS.Core.Editor
{
	public class LogConfigWindow : EditorWindow
	{
		private bool _logsEnabled;
		private LogLevel _minimumLevel;

		private GUIStyle _headerStyle;
		private GUIStyle _sectionStyle;
		private GUIStyle _boxStyle;
		private GUIStyle _dropdownStyle;

		public static void ShowWindow()
		{
			var window = GetWindow<LogConfigWindow>("Log Configuration");
			window.minSize = new Vector2(450, 700);
			window.Show();
		}

		private void OnEnable()
		{
			// Activate all log channels in case a preference isn't set.
			Log.EnabledChannels = (LogChannel)EditorPrefs.GetInt(LogEditorConstants.ActiveChannels, (int)LogChannel.All);
			_minimumLevel = (LogLevel)EditorPrefs.GetInt(LogEditorConstants.MinimumLevel, 0);
			_logsEnabled = LogEditorUtility.AreLogsEnabled();
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

			if (_dropdownStyle == null)
			{
				_dropdownStyle = new GUIStyle(EditorStyles.popup)
				{
					fixedHeight = 40,
					fontSize = 14,
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
			EditorGUILayout.LabelField("Current Level:", GUILayout.Width(100), GUILayout.Height(25));

			var levelColor = GetLevelColor(_minimumLevel);
			var originalColor = GUI.backgroundColor;
			GUI.backgroundColor = levelColor;

			var newLevel = (LogLevel)EditorGUILayout.EnumPopup(_minimumLevel, _dropdownStyle);

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
				SetStateOfAllChannels(true);
			}
			if (GUILayout.Button("✗ Disable All", GUILayout.Height(25)))
			{
				SetStateOfAllChannels(false);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(8);
			EditorGUILayout.LabelField(
				$"Active Channels Count: {Log.EnabledChannels.GetActiveLogChannelCount().ToString()}",
				new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter });
			Log.EnabledChannels = (LogChannel)EditorGUILayout.EnumFlagsField(Log.EnabledChannels, _dropdownStyle);
			EditorGUILayout.Space(5);

			EditorGUILayout.EndVertical();
		}

		private Color GetLevelColor(LogLevel level)
		{
			return level switch
			{
				LogLevel.Trace => new Color(0.7f, 0.7f, 0.7f),
				LogLevel.Debug => new Color(0.6f, 0.8f, 1f),
				LogLevel.Info => new Color(0.6f, 1f, 0.6f),
				LogLevel.Warning => new Color(1f, 0.9f, 0.4f),
				LogLevel.Error => new Color(1f, 0.5f, 0.4f),
				LogLevel.Fatal => new Color(1f, 0.3f, 0.3f),
				_ => Color.white,
			};
		}

		private void ToggleEnableLogs()
		{
			var defines = LogEditorUtility.GetCurrentBuildProfileDefines(out var namedBuildTarget);
			var definesList = defines.Split(';').ToList();

			if (_logsEnabled)
			{
				if (!definesList.Contains(LogEditorConstants.LogsEnabledSymbol))
				{
					definesList.Add(LogEditorConstants.LogsEnabledSymbol);
				}
			}
			else
			{
				definesList.Remove(LogEditorConstants.LogsEnabledSymbol);
			}

			var newDefines = string.Join(";", definesList);
			PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newDefines);
			Debug.Log($"[QBS] Logging {(_logsEnabled ? "enabled" : "disabled")}");
		}

		private void SetMinimumLevel(LogLevel level)
		{
			Log.MinimumLevel = level;
			EditorPrefs.SetInt(LogEditorConstants.MinimumLevel, (int)level);
			Debug.Log($"[QBS] Minimum log level set to: {level}");
		}

		private void SetStateOfAllChannels(bool enabled)
		{
			Log.SetChannelEnabled(LogChannel.All, enabled);
			EditorPrefs.SetInt(LogEditorConstants.ActiveChannels, enabled ? ~0 : 0);
		}

		private void SaveChannelState(LogChannel channel, bool enabled)
		{
			var key = LogEditorConstants.GetChannelKey(channel);
			EditorPrefs.SetBool(key, enabled);
		}
	}
}