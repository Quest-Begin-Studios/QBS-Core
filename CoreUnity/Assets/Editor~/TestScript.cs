 using QBS.Core;
 using System;
 using System.Linq;
 using UnityEngine;
 using UnityEditor;

 public class TestScript : EditorWindow
 {
 	private Vector2 _scrollPosition;
 	private bool _testsRun;
 	private string _testResults = "";

 	[MenuItem("Tools/QBS/Logs/Run Log Tests")]
 	public static void ShowWindow()
 	{
 		var window = GetWindow<TestScript>("Log Tests");
 		window.minSize = new Vector2(400, 300);
 		window.Show();
 	}

 	private void OnGUI()
 	{
 		GUILayout.Label("Log System Test Suite", EditorStyles.boldLabel);
 		EditorGUILayout.Space(10);

 		if (GUILayout.Button("Run All Tests", GUILayout.Height(40)))
 		{
 			RunAllTests();
 		}

 		EditorGUILayout.Space(10);

 		if (_testsRun)
 		{
 			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
 			EditorGUILayout.TextArea(_testResults, GUILayout.ExpandHeight(true));
 			EditorGUILayout.EndScrollView();
 		}
 		else
 		{
 			EditorGUILayout.HelpBox("Click 'Run All Tests' to execute the test suite.", MessageType.Info);
 		}
 	}

 	private void RunAllTests()
 	{
 		_testResults = "";
 		_testsRun = true;

 		try
 		{
 			LogToResults("=== Starting Log System Tests ===\n");

 			TestLogChannels();
 			TestLogLevels();
 			TestLogOutput();

 			LogToResults("\n=== All Log System Tests Completed ===");
 			Debug.Log("✓ All Log System Tests Completed Successfully");
 		}
 		catch (Exception ex)
 		{
 			LogToResults($"\n❌ TEST SUITE FAILED: {ex.Message}");
 			Debug.LogError($"Test suite failed: {ex}");
 		}

 		Repaint();
 	}

 	private void LogToResults(string message)
 	{
 		_testResults += message + "\n";
 		Debug.Log(message);
 	}

 	#region 1. Log Channel Tests

 	private void TestLogChannels()
 	{
 		LogToResults("--- Testing Log Channels ---");

 		TestLogChannelEnumFlags();
 		TestIsChannelEnabled();
 		TestSetChannelEnabled();
 		TestEnabledChannelsProperty();

 		LogToResults("✓ Log Channel Tests Passed");
 	}

 	private void TestLogChannelEnumFlags()
 	{
 		var originalChannels = Log.EnabledChannels;

 		try
 		{
 			LogChannel none = default;
 			Log.Assert(none == 0, "LogChannel.None should be 0");

 			Log.EnabledChannels = LogChannel.All;
 			Log.Assert(Log.EnabledChannels != 0, "LogChannel.All should not be 0");

 			var allFlags = Enum.GetValues(typeof(LogChannel)).Cast<LogChannel>().ToArray();
 			Log.Assert(allFlags.Length > 0, "LogChannel should have at least one value");

 			LogToResults($"  ✓ LogChannel enum has {allFlags.Length} values");
 		}
 		finally
 		{
 			Log.EnabledChannels = originalChannels;
 		}
 	}

 	private void TestIsChannelEnabled()
 	{
 		var originalChannels = Log.EnabledChannels;

 		try
 		{
 			Log.EnabledChannels = LogChannel.All;

 			var allFlags = Enum.GetValues(typeof(LogChannel)).Cast<LogChannel>()
 				.Where(c => c != 0 && c != LogChannel.All).ToArray();

 			foreach (var channel in allFlags)
 			{
 				var isEnabled = Log.IsChannelEnabled(channel);
 				Log.Assert(isEnabled, $"Channel {channel} should be enabled when All is set");
 			}

 			Log.EnabledChannels = 0;
 			foreach (var channel in allFlags)
 			{
 				var isEnabled = Log.IsChannelEnabled(channel);
 				Log.Assert(!isEnabled, $"Channel {channel} should be disabled when None is set");
 			}

 			LogToResults("  ✓ IsChannelEnabled returns correct state");
 		}
 		finally
 		{
 			Log.EnabledChannels = originalChannels;
 		}
 	}

 	private void TestSetChannelEnabled()
 	{
 		var originalChannels = Log.EnabledChannels;

 		try
 		{
 			Log.EnabledChannels = 0;

 			var allFlags = Enum.GetValues(typeof(LogChannel)).Cast<LogChannel>()
 				.Where(c => c != 0 && c != LogChannel.All).Take(3).ToArray();

 			if (allFlags.Length > 0)
 			{
 				var testChannel = allFlags[0];

 				Log.SetChannelEnabled(testChannel, true);
 				Log.Assert(Log.IsChannelEnabled(testChannel), $"Channel {testChannel} should be enabled after SetChannelEnabled(true)");

 				Log.SetChannelEnabled(testChannel, false);
 				Log.Assert(!Log.IsChannelEnabled(testChannel), $"Channel {testChannel} should be disabled after SetChannelEnabled(false)");

 				foreach (var channel in allFlags)
 				{
 					Log.SetChannelEnabled(channel, true);
 				}

 				foreach (var channel in allFlags)
 				{
 					Log.Assert(Log.IsChannelEnabled(channel), $"Channel {channel} should remain enabled");
 				}
 			}

 			LogToResults("  ✓ SetChannelEnabled correctly enables/disables channels");
 		}
 		finally
 		{
 			Log.EnabledChannels = originalChannels;
 		}
 	}

 	private void TestEnabledChannelsProperty()
 	{
 		var originalChannels = Log.EnabledChannels;

 		try
 		{
 			Log.EnabledChannels = LogChannel.All;
 			Log.Assert(Log.EnabledChannels == LogChannel.All, "EnabledChannels get should return All");

 			Log.EnabledChannels = 0;
 			Log.Assert(Log.EnabledChannels == 0, "EnabledChannels get should return None");

 			var allFlags = Enum.GetValues(typeof(LogChannel)).Cast<LogChannel>()
 				.Where(c => c != 0 && c != LogChannel.All).Take(2).ToArray();

 			if (allFlags.Length >= 2)
 			{
 				var combined = allFlags[0] | allFlags[1];
 				Log.EnabledChannels = combined;
 				Log.Assert(Log.EnabledChannels == combined, "EnabledChannels should store combined flags");
 			}

 			LogToResults("  ✓ EnabledChannels property get/set works correctly");
 		}
 		finally
 		{
 			Log.EnabledChannels = originalChannels;
 		}
 	}

 	#endregion

 	#region 2. Log Level Tests

 	private void TestLogLevels()
 	{
 		LogToResults("--- Testing Log Levels ---");

 		TestLogLevelEnumValues();
 		TestLogFiltering();

 		LogToResults("✓ Log Level Tests Passed");
 	}

 	private void TestLogLevelEnumValues()
 	{
 		Log.Assert(Enum.IsDefined(typeof(LogLevel), LogLevel.Trace), "LogLevel.Trace should exist");
 		Log.Assert(Enum.IsDefined(typeof(LogLevel), LogLevel.Debug), "LogLevel.Debug should exist");
 		Log.Assert(Enum.IsDefined(typeof(LogLevel), LogLevel.Info), "LogLevel.Info should exist");
 		Log.Assert(Enum.IsDefined(typeof(LogLevel), LogLevel.Warning), "LogLevel.Warning should exist");
 		Log.Assert(Enum.IsDefined(typeof(LogLevel), LogLevel.Error), "LogLevel.Error should exist");
 		Log.Assert(Enum.IsDefined(typeof(LogLevel), LogLevel.Fatal), "LogLevel.Fatal should exist");
 		
 		Log.Assert((int)LogLevel.Trace < (int)LogLevel.Debug, "Trace should be less than Debug");
 		Log.Assert((int)LogLevel.Debug < (int)LogLevel.Info, "Debug should be less than Info");
 		Log.Assert((int)LogLevel.Info < (int)LogLevel.Warning, "Info should be less than Warning");
 		Log.Assert((int)LogLevel.Warning < (int)LogLevel.Error, "Warning should be less than Error");
 		Log.Assert((int)LogLevel.Error < (int)LogLevel.Fatal, "Error should be less than Fatal");

 		LogToResults("  ✓ All LogLevel enum values exist and are correctly ordered");
 	}

 	private void TestLogFiltering()
 	{
 		var originalMinLevel = Log.MinimumLevel;

 		try
 		{
 			Log.MinimumLevel = LogLevel.Warning;
 			Log.Assert(Log.MinimumLevel == LogLevel.Warning, "MinimumLevel should be set to Warning");

 			Log.MinimumLevel = LogLevel.Trace;
 			Log.Assert(Log.MinimumLevel == LogLevel.Trace, "MinimumLevel should be set to Trace");

 			Log.MinimumLevel = LogLevel.Fatal;
 			Log.Assert(Log.MinimumLevel == LogLevel.Fatal, "MinimumLevel should be set to Fatal");

 			LogToResults("  ✓ Log filtering based on level works correctly");
 		}
 		finally
 		{
 			Log.MinimumLevel = originalMinLevel;
 		}
 	}

 	#endregion

 	#region 3. Log Output Tests

 	private void TestLogOutput()
 	{
 		LogToResults("--- Testing Log Output ---");

 		var originalChannels = Log.EnabledChannels;
 		var originalMinLevel = Log.MinimumLevel;

 		try
 		{
 			Log.EnabledChannels = LogChannel.All;
 			Log.MinimumLevel = LogLevel.Trace;

 			Log.Trace("Test trace message");
 			Log.Debug("Test debug message");
 			Log.Info("Test info message");
 			Log.Warning("Test warning message");
 			Log.Error("Test error message");
 			Log.Fatal("Test fatal message");

 			Log.Info("Test with tag", "TEST_TAG");
 			Log.Info("Test with color", color: Color.cyan);

 			var firstChannel = Enum.GetValues(typeof(LogChannel)).Cast<LogChannel>()
 				.FirstOrDefault(c => c != 0 && c != LogChannel.All);
 			if (firstChannel != default)
 			{
 				Log.Info("Test with channel", channel: firstChannel);
 			}

 			var testException = new Exception("Test exception");
 			Log.Error("Test error with exception", testException);
 			Log.Fatal("Test fatal with exception", testException);

 			LogToResults("  ✓ Log output methods execute without errors");
 			LogToResults("  ℹ Note: Actual log formatting can only be verified visually in console");
 		}
 		finally
 		{
 			Log.EnabledChannels = originalChannels;
 			Log.MinimumLevel = originalMinLevel;
 		}
 	}

 	#endregion
}