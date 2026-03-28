using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace QBS.Core.Editor
{
	public class LogSourceCompiler : EditorWindow
	{
		private const string NamespaceStr = "QBS.Core";
		private const string LogChannelsName = "LogChannel";
		private const string AssemblyReferencesJson = "AssemblyReferences.json";

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
		private List<string> _runtimeAssemblyReferences;
		private List<string> _editorAssemblyReferences;
		private List<string> _scriptingSymbols = new() { "ENABLE_LOGS" };

		[MenuItem("Tools/QBS/Logs/Log Source Compiler")]
		public static void ShowWindow()
		{
			var window = GetWindow<LogSourceCompiler>("Log Source Compiler");
			window.minSize = new Vector2(600, 500);
		}

		private void OnEnable()
		{
			_sourceFiles = new List<SourceFile>();
			_editorAssemblyReferences = new List<string>();
			_runtimeAssemblyReferences = new List<string>();

			_stringEnumGenerator = new StringEnumGenerator();
			InitializeAndConfigureEnumGenerator();
		}

		private void InitializeAndConfigureEnumGenerator()
		{
			_enumGenerator = new EnumGeneratorComponent();

			_enumGenerator.Initialize();
			_enumGenerator.ConfigureEnumProperties
			(
				LogChannelsName,
				NamespaceStr,
				EnumGeneratorComponent.EnumTypeOption.Flags,
				EnumGeneratorComponent.BackingType.Int
			);
			_enumGenerator.ConfigureEnumKeys
			(
				LogChannelDefaults.BaseKeys,
				LogChannelDefaults.FlagCombinations
			);
		}

		private void OnDisable()
		{
			_sourceFiles = null;
			_enumGenerator = null;
			_stringEnumGenerator = null;

			_capturedEnumCode = "";
			_runtimeAssemblyReferences = null;
			_editorAssemblyReferences = null;
			_scriptingSymbols = null;
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
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("Scripting Symbols", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);

			for (var i = 0; i < _scriptingSymbols.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				_scriptingSymbols[i] = EditorGUILayout.TextField($"Symbol {i + 1}", _scriptingSymbols[i]);
				if (GUILayout.Button("Remove", GUILayout.Width(70)))
				{
					_scriptingSymbols.RemoveAt(i);
					break;
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Add Scripting Symbol", GUILayout.Height(25)))
			{
				_scriptingSymbols.Add("");
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Space(10);

			if (GUILayout.Button("Compile", GUILayout.Height(50)))
			{
				var dllGenerationSuccess = DLLGenerationHelper.TryGeneratingDLL
				(
					_sourceFiles,
					"Log",
					"Log",
					_runtimeAssemblyReferences,
					_editorAssemblyReferences,
					_scriptingSymbols
				);

				if (dllGenerationSuccess)
				{
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
					EditorUtility.RequestScriptReload();
				}

			}
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

			if (GUILayout.Button("Generate Enum", GUILayout.Height(30)))
			{
				var enumGenSuccess = _enumGenerator.GenerateEnum();

				if (enumGenSuccess)
				{
					_capturedEnumCode = _enumGenerator.GeneratedCode;
					//Also generate the relevant ToStringNoBox methods:
					GenerateEnumHelpers();

					_currentTab = Tab.SourceCompilation;
				}
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

		private void GenerateEnumHelpers()
		{
			var enumKeys = new List<string>();
			enumKeys.AddRange(_enumGenerator.EnumKeys);
			enumKeys.AddRange(_enumGenerator.FlagCombinations.Select(combination => combination.Name));
			var toStringNoBoxSource = _stringEnumGenerator.GenerateNoBoxStringsFromSource(LogChannelsName, enumKeys, NamespaceStr);

			_sourceFiles.Add(new SourceFile(_capturedEnumCode, "LogChannels.cs"));
			_sourceFiles.Add(new SourceFile(toStringNoBoxSource, "LogChannelsStringUtils.cs"));
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

			if (GUILayout.Button("Select Source Folder", GUILayout.Height(30)))
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
			_runtimeAssemblyReferences.Clear();
			_editorAssemblyReferences.Clear();
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
				using var reader = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read));
				var sourceContent = reader.ReadToEnd();

				var fileName = Path.GetFileName(file);
				var extension = Path.GetExtension(fileName);

				if (extension != ".cs")
				{
					fileName = Path.GetFileNameWithoutExtension(fileName) + ".cs";
				}

				_sourceFiles.Add(new SourceFile(sourceContent, fileName));
			}

			// Read JSON file for assembly references
			var jsonFile = Path.Combine(_selectedFolderPath, AssemblyReferencesJson);
			if (!File.Exists(jsonFile))
			{
				Debug.LogWarning($"AssemblyReferences.json not found at: {jsonFile}");
				return;
			}
			
			try
			{
				var jsonContent = File.ReadAllText(jsonFile);
				var assemblyReferences = JsonUtility.FromJson<AssemblyReferencesData>(jsonContent);
				if (assemblyReferences != null)
				{
					if (assemblyReferences.RuntimeAssemblies is { Length: > 0 })
					{
						_runtimeAssemblyReferences.AddRange(ResolveAssemblyPaths(assemblyReferences.RuntimeAssemblies));
						Debug.Log($"Loaded {assemblyReferences.RuntimeAssemblies.Length} runtime assembly references");
					}
					if (assemblyReferences.EditorAssemblies is { Length: > 0 })
					{
						_editorAssemblyReferences.AddRange(ResolveAssemblyPaths(assemblyReferences.EditorAssemblies));
						Debug.Log($"Loaded {assemblyReferences.EditorAssemblies.Length} editor assembly references");
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Failed to parse AssemblyReferences.json: {e.Message}");
			}
		}

		private List<string> ResolveAssemblyPaths(string[] assemblyNames)
		{
			var resolvedPaths = new List<string>();
			var loadedAssemblies = AssemblyUtilities.GetLoadedAssemblies();

			foreach (var assemblyName in assemblyNames)
			{
				var assembly = loadedAssemblies.FirstOrDefault
				(a =>
					a.GetName().Name.Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase)
				);

				if (assembly != null)
				{
					resolvedPaths.Add(assembly.Location);
					Debug.Log($"Resolved assembly '{assemblyName}' to: {assembly.Location}");
				}
				else
				{
					Debug.LogWarning($"Could not resolve assembly: {assemblyName}");
				}
			}

			return resolvedPaths;
		}

		[Serializable]
		private class AssemblyReferencesData
		{
			public string[] RuntimeAssemblies;
			public string[] EditorAssemblies;
		}
	}

}