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
		private const string RawSourceFolderPath = @"Assets\com.qbs.core\RawSource~\LogSource";

		private EnumGeneratorComponent _enumGenerator;
		private EnumUtilsGenerator _stringEnumGenerator;
		private Vector2 _scrollPosition;
		private Vector2 _enumScrollPosition;

		private List<SourceFile> _sourceFiles;
		private string _capturedEnumCode = "";
		private List<string> _runtimeAssemblyReferences;
		private List<string> _editorAssemblyReferences;
		private readonly List<string> _scriptingSymbols = new() { "ENABLE_LOGS" };

		[MenuItem("Tools/QBS/Logs/Log Source Compiler")]
		public static void ShowWindow()
		{
			var window = GetWindow<LogSourceCompiler>("Log Source Compiler");
			window.minSize = new Vector2(600, 1050);
		}

		private void OnEnable()
		{
			_sourceFiles = new List<SourceFile>();
			_editorAssemblyReferences = new List<string>();
			_runtimeAssemblyReferences = new List<string>();

			_stringEnumGenerator = new EnumUtilsGenerator();
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
		}

		private void OnGUI()
		{
			GUILayout.Label("Log Source Compiler", EditorStyles.largeLabel);
			EditorGUILayout.Space(5);

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
			var helpBoxStyle = new GUIStyle(EditorStyles.helpBox);
			helpBoxStyle.fontSize = 13;
			helpBoxStyle.padding = new RectOffset(10, 10, 10, 10);
			EditorGUILayout.LabelField
			(
				"This tool compiles log sources into a DLL.\n\n" +
				"Steps:\n" +
				"1. Configure and add LogChannel enum keys below\n" +
				"2. Click 'Generate Enum' to create the enum and read sources from RawSource~ folder\n" +
				"3. Click 'Generate DLL' to compile the final DLL\n\n" +
				"Output: The compiled DLL will be placed in the Plugins folder.",
				helpBoxStyle
			);
			EditorGUILayout.EndVertical();

			EditorGUILayout.Space(10);

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

			if (!string.IsNullOrEmpty(_capturedEnumCode))
			{
				EditorGUILayout.Space(20);
				DrawDLLGenerationSection();
			}

			DrawEnumGenerationSection();

			EditorGUILayout.EndScrollView();
		}

		private void DrawDLLGenerationSection()
		{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("DLL Generation", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);
			EditorGUILayout.HelpBox("Enum generated successfully! Click below to compile the DLL.\nScripting symbols: ENABLE_LOGS", MessageType.Info);
			EditorGUILayout.Space(10);

			var originalColor = GUI.backgroundColor;
			GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
			if (GUILayout.Button("Generate DLL", GUILayout.Height(50)))
			{
				GUI.backgroundColor = originalColor;
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
			GUI.backgroundColor = originalColor;
			EditorGUILayout.EndVertical();
		}

		private void DrawEnumGenerationSection()
		{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("Enum Generation", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("Create the LogChannel enum here.", MessageType.Info);
			EditorGUILayout.Space();

			_enumScrollPosition = EditorGUILayout.BeginScrollView(_enumScrollPosition, GUILayout.Height(500));

			EditorGUI.BeginDisabledGroup(true);
			_enumGenerator.DrawConfigurationGUI();
			EditorGUI.EndDisabledGroup();

			_enumGenerator.DrawEnumListGUI();

			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space(5);

			if (GUILayout.Button("Generate Enum", GUILayout.Height(30)))
			{
				ReadSourcesFromRawSourceFolder();

				var enumGenSuccess = _enumGenerator.GenerateEnum();

				if (enumGenSuccess)
				{
					_capturedEnumCode = _enumGenerator.GeneratedCode;
					//Also generate the relevant ToStringNoBox methods:
					GenerateEnumHelpers();
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
			var toStringNoBoxSource = _stringEnumGenerator.Generate
			(
				EnumUtilities.GenerateToStringFast,
				LogChannelsName,
				enumKeys,
				NamespaceStr
			);

			_sourceFiles.Add(new SourceFile(_capturedEnumCode, "LogChannels.cs"));
			_sourceFiles.Add(new SourceFile(toStringNoBoxSource, "LogChannelsStringUtils.cs"));
		}

		private void ReadSourcesFromRawSourceFolder()
		{
			_sourceFiles.Clear();
			_runtimeAssemblyReferences.Clear();
			_editorAssemblyReferences.Clear();

			var packagePath = "Packages/com.qbs.core";
			var rawSourceFolder = Path.Combine(packagePath, "RawSource~/LogSource");

			if (!Directory.Exists(rawSourceFolder))
			{
				Debug.LogWarning($"RawSource~ folder not found at package path: {rawSourceFolder}");
				rawSourceFolder = RawSourceFolderPath;

				if (!Directory.Exists(rawSourceFolder))
				{
					Debug.LogError($"RawSource~ folder not found at fallback path: {rawSourceFolder}");
					return;
				}

				Debug.Log($"Using fallback RawSource~ folder at: {rawSourceFolder}");
			}

			var fileList = new List<string>();

			var csFiles = Directory.GetFiles(rawSourceFolder, "*.cs", SearchOption.AllDirectories);
			fileList.AddRange(csFiles);

			var txtFiles = Directory.GetFiles(rawSourceFolder, "*.txt", SearchOption.AllDirectories);
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
			var jsonFile = Path.Combine(rawSourceFolder, AssemblyReferencesJson);
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