using QBS.Core.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace QBS.Editor
{
	public class LogSourceCompiler : EditorWindow
	{
		private const string LogChannelsName = "LogChannel";
		private const string NamespaceStr = "QBS.Core";

		private enum Tab
		{
			SourceFetch,
			EnumGeneration,
			SourceCompilation,
		}

		private EnumGeneratorComponent _enumGenerator;
		private StringEnumGenerator _stringEnumGenerator;
		private Tab _currentTab;
		private Vector2 _scrollPosition;
		private string _selectedFolderPath = "";

		private List<SourceFile> _sourceFiles;
		private string _capturedEnumCode = "";
		private Vector2 _fileListScrollPosition;

		[MenuItem("Tools/QBS/Logs/Log Source Compiler")]
		public static void ShowWindow()
		{
			var window = GetWindow<LogSourceCompiler>("Log Source Compiler");
			window.minSize = new Vector2(600, 500);
		}

		private void OnEnable()
		{
			_sourceFiles = new List<SourceFile>();
			_enumGenerator = new EnumGeneratorComponent();
			_enumGenerator.Initialize();
			_enumGenerator.ConfigureEnumProperties
			(
				LogChannelsName,
				NamespaceStr,
				EnumGeneratorComponent.EnumTypeOption.Flags,
				EnumGeneratorComponent.BackingType.Int
			);
			_stringEnumGenerator = new StringEnumGenerator();
		}

		private void OnDisable()
		{
			_sourceFiles = null;
			_enumGenerator = null;
			_capturedEnumCode = "";
		}

		private void OnGUI()
		{
			GUILayout.Label("Log Source Compiler", EditorStyles.largeLabel);

			EditorGUILayout.Space(5);

			DrawTabBar();

			EditorGUILayout.Space(10);

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

			switch (_currentTab)
			{
				case Tab.SourceFetch:
				{
					DrawSourceFetchTab();
					break;
				}
				case Tab.EnumGeneration:
				{
					DrawEnumGenerationTab();
					break;
				}
				case Tab.SourceCompilation:
				{
					DrawSourceCompilationTab();
					break;
				}
			}

			EditorGUILayout.EndScrollView();
		}

		private void DrawSourceCompilationTab()
		{
			if (GUILayout.Button("Compile", GUILayout.Height(50)))
			{
				try
				{
					WriteSourceFilesToTemp();
					DLLGenerationHelper.TryGeneratingDLL(_sourceFiles, "Log", "Log");
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		private void WriteSourceFilesToTemp()
		{
			var tempPath = Path.Combine(Path.GetTempPath(), "QBS_LogSourceCompilation");
			
			if (Directory.Exists(tempPath))
			{
				Directory.Delete(tempPath, true);
			}
			
			Directory.CreateDirectory(tempPath);
			
			foreach (var sourceFile in _sourceFiles)
			{
				var filePath = Path.Combine(tempPath, sourceFile.FilePath);
				File.WriteAllText(filePath, sourceFile.SourceContent);
			}
			
			Debug.Log($"Source files written to: {tempPath}");
		}

		private void DrawTabBar()
		{
			if (_sourceFiles == null || _sourceFiles.Count == 0)
			{
				_currentTab = Tab.SourceFetch;
				return;
			}
			
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Toggle(_currentTab == Tab.SourceFetch, "Fetch Source", EditorStyles.toolbarButton))
			{
				_currentTab = Tab.SourceFetch;
			}

			if (GUILayout.Toggle(_currentTab == Tab.EnumGeneration, "Enum Generation", EditorStyles.toolbarButton))
			{
				_currentTab = Tab.EnumGeneration;
			}

			if (!string.IsNullOrEmpty(_capturedEnumCode))
			{
				if (GUILayout.Toggle(_currentTab == Tab.SourceCompilation, "Source Generation", EditorStyles.toolbarButton))
				{
					_currentTab = Tab.SourceCompilation;
				}
			}

			EditorGUILayout.EndHorizontal();
		}

		private void DrawEnumGenerationTab()
		{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("Enum Generation", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("Create the LogChannel enum here.", MessageType.Info);
			EditorGUILayout.Space();

			EditorGUI.BeginDisabledGroup(true);
			_enumGenerator.DrawConfigurationGUI();
			EditorGUI.EndDisabledGroup();

			_enumGenerator.DrawEnumListGUI();
			if (_enumGenerator.DrawGenerateEnumButton())
			{
				_capturedEnumCode = _enumGenerator.GeneratedCode;
				
				//Also generate the relevant ToStringNoBox methods:
				var enumKeys = new List<string>();
				enumKeys.AddRange(_enumGenerator.EnumKeys);
				enumKeys.AddRange(_enumGenerator.FlagCombinations.Select(combination => combination.Name));
				var toStringNoBoxSource = _stringEnumGenerator.GenerateNoBoxStringsFromSource(LogChannelsName, enumKeys, NamespaceStr);
				
				_sourceFiles.Add(new SourceFile(_capturedEnumCode, "LogChannels.cs"));
				_sourceFiles.Add(new SourceFile(toStringNoBoxSource, "LogChannelsStringUtils.cs"));
				_currentTab = Tab.SourceCompilation;
			}
			_enumGenerator.DrawOutputGUI(showCopyButton: true);

			EditorGUILayout.EndVertical();

			EditorGUILayout.Space(20);

			if (!string.IsNullOrEmpty(_capturedEnumCode))
			{
				EditorGUILayout.BeginVertical("box");
				GUILayout.Label("Captured Enum for Processing", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox
				(
					$"Enum code captured! You can now use this in other functions.\nLength: {_capturedEnumCode.Length} characters",
					MessageType.Info
				);

				if (GUILayout.Button("Clear Captured Enum"))
				{
					_capturedEnumCode = "";
				}

				EditorGUILayout.EndVertical();
			}
		}

		private void DrawSourceFetchTab()
		{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("Source Compilation", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			EditorGUILayout.HelpBox("Configure and compile log sources here.", MessageType.Info);

			EditorGUILayout.Space(10);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Selected Folder:", GUILayout.Width(100));
			EditorGUILayout.LabelField(string.IsNullOrEmpty(_selectedFolderPath) ? "None" : _selectedFolderPath);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(5);

			if (GUILayout.Button("Select Folder", GUILayout.Height(30)))
			{
				var selectedPath = EditorUtility.OpenFolderPanel("Select Folder to Crawl for Source Files", _selectedFolderPath, "");
				if (!string.IsNullOrEmpty(selectedPath))
				{
					_selectedFolderPath = selectedPath;
					ReadAllFilesInFolder();
					_currentTab = Tab.EnumGeneration;
				}
			}

			EditorGUILayout.Space(10);

			if (_sourceFiles.Count > 0)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label($"Read {_sourceFiles.Count} file(s)", EditorStyles.largeLabel);
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
			}
			else if (!string.IsNullOrEmpty(_selectedFolderPath))
			{
				EditorGUILayout.HelpBox("No .cs or .txt files found in the selected folder.", MessageType.Warning);
			}

			EditorGUILayout.EndVertical();
		}

		private void ReadAllFilesInFolder()
		{
			_sourceFiles.Clear();
			if (string.IsNullOrEmpty(_selectedFolderPath) || !Directory.Exists(_selectedFolderPath))
			{
				return;
			}

			var fileList = new List<string>();
			
			var csFiles = Directory.GetFiles(_selectedFolderPath, "*.cs", SearchOption.AllDirectories);
			fileList.AddRange(csFiles);

			var txtFiles = Directory.GetFiles(_selectedFolderPath, "*.txt", SearchOption.AllDirectories);
			fileList.AddRange(txtFiles);
			
			foreach (var file in fileList)
			{
				using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
				using var reader = new StreamReader(fs);
				var sourceContent = reader.ReadToEnd();
				
				var fileName = Path.GetFileName(file);
				var extension = Path.GetExtension(fileName);
				
				if (extension != ".cs")
				{
					fileName = Path.GetFileNameWithoutExtension(fileName) + ".cs";
				}
				
				_sourceFiles.Add(new SourceFile(sourceContent, fileName));
			}
		}
	}
}