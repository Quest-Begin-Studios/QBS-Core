using QBS.Core.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace QBS.Editor
{
	public class LogSourceCompiler : EditorWindow
	{
		private enum Tab
		{
			SourceFetch,
			EnumGeneration,
		}

		private Tab _currentTab;
		private EnumGeneratorComponent _enumGenerator;
		private Vector2 _scrollPosition;
		private string _selectedFolderPath = "";

		private List<string> _sourceStrings;
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
			_sourceStrings = new List<string>();
			_enumGenerator = new EnumGeneratorComponent();
			_enumGenerator.Initialize();
			_enumGenerator.ConfigureEnumProperties
			(
				"LogChannel",
				"QBS.Core",
				EnumGeneratorComponent.EnumTypeOption.Flags,
				EnumGeneratorComponent.BackingType.Int
			);
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
				case Tab.EnumGeneration:
				{
					DrawEnumGenerationTab();
					break;
				}
				case Tab.SourceFetch:
				{
					DrawSourceFetchTab();
					break;
				}
			}

			EditorGUILayout.EndScrollView();
		}

		private void DrawTabBar()
		{
			if (_sourceStrings == null || _sourceStrings.Count == 0)
			{
				return;
			}
			
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Toggle(_currentTab == Tab.SourceFetch, "Source Compilation", EditorStyles.toolbarButton))
			{
				_currentTab = Tab.SourceFetch;
			}

			if (GUILayout.Toggle(_currentTab == Tab.EnumGeneration, "Enum Generation", EditorStyles.toolbarButton))
			{
				_currentTab = Tab.EnumGeneration;
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
				_sourceStrings.Add(_capturedEnumCode);
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
				}
			}

			EditorGUILayout.Space(10);

			if (_sourceStrings.Count > 0)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label($"Read {_sourceStrings.Count} file(s)", EditorStyles.largeLabel);
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
			_sourceStrings.Clear();
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
				_sourceStrings.Add(reader.ReadToEnd());
			}
		}
	}
}