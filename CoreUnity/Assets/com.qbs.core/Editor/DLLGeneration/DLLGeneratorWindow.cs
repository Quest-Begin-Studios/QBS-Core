using QBS.Core;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace QBS.Editor
{
	public class DLLGeneratorWindow : EditorWindow
	{
		private string _folderPath = "C:/Users/varun/Desktop/Projects/QBS-Core/CoreUnity/Assets/com.qbs.core/Runtime/DLLTest";
		private string _outputDLLFolder = "C:/Users/varun/Desktop/Projects/QBS-Core/CoreUnity/Assets/Plugins/";
		private string _outputDLLFileName = "DLLCheck";
		private const string DLLFileExtension = ".dll";

		private bool _tryResolveDependencies;
		private int _recursionCount = 10;
		private bool _debugMode;
		private Vector2 _scrollPosition;
		private readonly List<string> _foundFiles = new();
		private string _statusMessage = "";
		private MessageType _statusType = MessageType.Info;

		private readonly List<string> _referencedAssemblies = new();

		[MenuItem("Tools/QBS/DLL Generator")]
		public static void ShowWindow()
		{
			var window = GetWindow<DLLGeneratorWindow>("DLL Generator");
			window.minSize = new Vector2(400, 300);
		}
		
		private void OnEnable()
		{
			_referencedAssemblies.Clear();
		}

		private void OnGUI()
		{
			DrawPathConfiguration();
			DrawCompilationSettings();
			
			DrawActionButtons();
			DrawFoundFilesList();
			
			DrawGenerateButton();
			DrawStatusMessage();
		}

		private void DrawPathConfiguration()
		{
			GUILayout.Label("DLL Generator", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Source Folder:", GUILayout.Width(100));
			_folderPath = EditorGUILayout.TextField(_folderPath);
			
			if (GUILayout.Button("Browse", GUILayout.Width(70)))
			{
				var selectedPath = EditorUtility.OpenFolderPanel("Select Source Folder", _folderPath, "");
				if (!string.IsNullOrEmpty(selectedPath))
				{
					_folderPath = selectedPath;
					ScanFolder();
				}
			}
			EditorGUILayout.EndHorizontal();
			

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Output Folder:", GUILayout.Width(100));
			_outputDLLFolder = EditorGUILayout.TextField(_outputDLLFolder);
			
			if (GUILayout.Button("Browse", GUILayout.Width(70)))
			{
				var selectedPath = EditorUtility.OpenFolderPanel("Select Output Folder", _outputDLLFolder, "");
				if (!string.IsNullOrEmpty(selectedPath))
				{
					_outputDLLFolder = selectedPath;
				}
			}
			EditorGUILayout.EndHorizontal();
			
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("DLL Filename:", GUILayout.Width(100));
			
			_outputDLLFileName = EditorGUILayout.TextField(_outputDLLFileName);
			EditorGUILayout.LabelField(DLLFileExtension, GUILayout.Width(30));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
		}

		private void DrawCompilationSettings()
		{
			_debugMode = EditorGUILayout.Toggle("Debug Mode", _debugMode, GUILayout.Width(200));
		
			EditorGUILayout.BeginHorizontal();
			_tryResolveDependencies = EditorGUILayout.Toggle("Try Resolve Dependencies", _tryResolveDependencies, GUILayout.Width(200));
		
			if (_tryResolveDependencies)
			{
				EditorGUILayout.LabelField("Recursion Count:", GUILayout.Width(100));
				_recursionCount = EditorGUILayout.IntField(_recursionCount);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
		}

		private void DrawActionButtons()
		{
			if (GUILayout.Button("Scan Folder for C# Files", GUILayout.Height(30)))
			{
				ScanFolder();
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Log Current AppDomain Assemblies", GUILayout.Height(30)))
			{
				LogCurrentAssemblies();
			}

			EditorGUILayout.Space();
		}

		private void DrawFoundFilesList()
		{
			if (_foundFiles.Count > 0)
			{
				EditorGUILayout.LabelField($"Found {_foundFiles.Count} C# file(s):", EditorStyles.boldLabel);
				_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
				foreach (var file in _foundFiles)
				{
					EditorGUILayout.LabelField(Path.GetFileName(file), EditorStyles.miniLabel);
				}
				EditorGUILayout.EndScrollView();
			}

			EditorGUILayout.Space();
		}

		private void DrawGenerateButton()
		{
			GUI.enabled = _foundFiles.Count > 0 && !string.IsNullOrEmpty(_outputDLLFolder) && !string.IsNullOrEmpty(_outputDLLFileName);
			if (GUILayout.Button("Generate DLL", GUILayout.Height(40)))
			{
				GenerateDLL();
			}
			GUI.enabled = true;
		}

		private void DrawStatusMessage()
		{
			if (!string.IsNullOrEmpty(_statusMessage))
			{
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox(_statusMessage, _statusType);
			}
		}

		private void ScanFolder()
		{
			_foundFiles.Clear();
			_statusMessage = "";

			if (!AreIOPathsSafe())
			{
				return;
			}

			try
			{
				var csFiles = Directory.GetFiles(_folderPath, "*.cs", SearchOption.AllDirectories);
				_foundFiles.AddRange(csFiles);

				if (_foundFiles.Count == 0)
				{
					_statusMessage = "No C# files found in the selected folder.";
					_statusType = MessageType.Warning;
				}
				else
				{
					_statusMessage = $"Successfully found {_foundFiles.Count} C# file(s).";
					_statusType = MessageType.Info;
				}
			}
			catch (Exception ex)
			{
				_statusMessage = $"Error scanning folder: {ex.Message}";
				_statusType = MessageType.Error;
			}
		}

		private void GenerateDLL()
		{
			if (!AreIOPathsSafe())
			{
				return;
			}
			
			if (_foundFiles.Count == 0)
			{
				_statusMessage = "No C# files to compile.";
				_statusType = MessageType.Warning;
				return;
			}

			try
			{
				// Combine folder and filename with extension
				var fullOutputPath = Path.Combine(_outputDLLFolder, _outputDLLFileName + DLLFileExtension);

				// Read all source files
				var sources = new string[_foundFiles.Count];
				for (var i = 0; i < _foundFiles.Count; i++)
				{
					sources[i] = File.ReadAllText(_foundFiles[i]);
				}

				// Generate DLL using DLLGenerator
				var parameters = new DLLGenerationParameters
				{
					Sources = sources,
					AssemblyLocations = _referencedAssemblies,
					OutputDLLPath = fullOutputPath,
					Debug = _debugMode
				};
				
				var success = DLLGenerator.PackSourcesIntoDLL(parameters);

				if (success)
				{
					_statusMessage = $"DLL successfully generated at: {fullOutputPath}";
					_statusType = MessageType.Info;
					AssetDatabase.Refresh();
				}
				else
				{
					_statusMessage = "DLL generation failed.";
					_statusType = MessageType.Error;
				}
			}
			catch (Exception ex)
			{
				_statusMessage = $"Error generating DLL: {ex.Message}";
				_statusType = MessageType.Error;
				Debug.LogError($"DLL Generation Error: {ex}");
			}
		}

		private bool AreIOPathsSafe()
		{
			if (string.IsNullOrEmpty(_folderPath))
			{
				_statusMessage = "Please select a folder path.";
				_statusType = MessageType.Warning;
				return false;
			}

			if (!Directory.Exists(_folderPath))
			{
				_statusMessage = "Selected folder does not exist.";
				_statusType = MessageType.Error;
				return false;
			}

			if (string.IsNullOrEmpty(_outputDLLFolder))
			{
				_statusMessage = "Please specify an output folder.";
				_statusType = MessageType.Warning;
				return false;
			}

			if (string.IsNullOrEmpty(_outputDLLFileName))
			{
				_statusMessage = "Please specify a DLL filename.";
				_statusType = MessageType.Warning;
				return false;
			}
			return true;
		}

		private void LogCurrentAssemblies()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Debug.Log($"Found {assemblies.Length} assemblies in current AppDomain:");
			
			foreach (var assembly in assemblies)
			{
				try
				{
					Debug.Log($"- {assembly.GetName().Name}: {assembly.Location}");
				}
				catch (Exception ex)
				{
					Debug.LogWarning($"  - {assembly.GetName().Name}: (Location unavailable - {ex.Message})");
				}
			}
			
			_statusMessage = $"Logged {assemblies.Length} assemblies to console.";
			_statusType = MessageType.Info;
		}
	}
}
