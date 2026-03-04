using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace QBS.Core.Editor
{
	public class EnumGeneratorWindow : EditorWindow
	{
		private string _enumName = "MyEnum";
		private string _namespace = "MyNamespace";
		private EnumAttributeFlags _attributes = EnumAttributeFlags.None;
		private BackingTypeOption _backingType = BackingTypeOption.Int;
		private EnumTypeOption _enumType = EnumTypeOption.Simple;
		
		private List<string> _enumKeys = new List<string> { "Value1", "Value2", "Value3" };
		private List<CustomValueEntry> _customValues = new List<CustomValueEntry> 
		{ 
			new CustomValueEntry { Key = "Value1", Value = "10" },
			new CustomValueEntry { Key = "Value2", Value = "20" },
			new CustomValueEntry { Key = "Value3", Value = "30" }
		};
		private List<FlagCombinationEntry> _flagCombinations = new List<FlagCombinationEntry>
		{
			new FlagCombinationEntry { Name = "ReadWrite", Flags = new List<string> { "Read", "Write" } }
		};
		
		private ReorderableList _enumKeysList;
		private ReorderableList _customValuesList;
		private ReorderableList _flagCombinationsList;
		
		private string _generatedCode = "";
		private Vector2 _scrollPosition;
		private Vector2 _outputScrollPosition;
		
		private enum BackingTypeOption
		{
			Byte,
			SByte,
			Short,
			UShort,
			Int,
			UInt,
			Long,
			ULong
		}
		
		private enum EnumTypeOption
		{
			Simple,
			Flags,
			CustomValues
		}
		
		[Serializable]
		private class CustomValueEntry
		{
			public string Key = "";
			public string Value = "";
		}
		
		[Serializable]
		private class FlagCombinationEntry
		{
			public string Name = "";
			public List<string> Flags = new List<string>();
		}
		
		[MenuItem("Tools/QBS/Enum Generator")]
		public static void ShowWindow()
		{
			var window = GetWindow<EnumGeneratorWindow>("Enum Generator");
			window.minSize = new Vector2(600, 500);
		}
		
		private void OnEnable()
		{
			_enumKeysList = new ReorderableList(_enumKeys, typeof(string));
			_enumKeysList.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Enum Keys");
			};
			_enumKeysList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				var key = _enumKeys[index];
				_enumKeys[index] = EditorGUI.TextField(rect, key);
			};
			_enumKeysList.onAddCallback = (ReorderableList list) =>
			{
				_enumKeys.Add("");
			};
			_enumKeysList.onRemoveCallback = (ReorderableList list) =>
			{
				_enumKeys.RemoveAt(list.index);
			};
			
			_customValuesList = new ReorderableList(_customValues, typeof(CustomValueEntry), true, true, true, true);
			_customValuesList.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height), "Key");
				EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.5f, rect.height), "Value");
			};
			_customValuesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				var entry = _customValues[index];
				entry.Key = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width * 0.48f, EditorGUIUtility.singleLineHeight), entry.Key);
				entry.Value = EditorGUI.TextField(new Rect(rect.x + rect.width * 0.52f, rect.y, rect.width * 0.48f, EditorGUIUtility.singleLineHeight), entry.Value);
			};
			_customValuesList.onAddCallback = (ReorderableList list) =>
			{
				_customValues.Add(new CustomValueEntry());
			};
			_customValuesList.onRemoveCallback = (ReorderableList list) =>
			{
				_customValues.RemoveAt(list.index);
			};
			
			_flagCombinationsList = new ReorderableList(_flagCombinations, typeof(FlagCombinationEntry), true, true, true, true);
			_flagCombinationsList.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Flag Combinations (Optional)");
			};
			_flagCombinationsList.elementHeightCallback = (int index) =>
			{
				var entry = _flagCombinations[index];
				return EditorGUIUtility.singleLineHeight * 2 + (entry.Flags.Count + 1) * (EditorGUIUtility.singleLineHeight + 2) + 10;
			};
			_flagCombinationsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				var entry = _flagCombinations[index];
				var y = rect.y + 2;
				
				EditorGUI.LabelField(new Rect(rect.x, y, 100, EditorGUIUtility.singleLineHeight), "Name:");
				entry.Name = EditorGUI.TextField(new Rect(rect.x + 100, y, rect.width - 100, EditorGUIUtility.singleLineHeight), entry.Name);
				y += EditorGUIUtility.singleLineHeight + 4;
				
				EditorGUI.LabelField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Flags:");
				y += EditorGUIUtility.singleLineHeight + 2;
				
				for (int i = 0; i < entry.Flags.Count; i++)
				{
					entry.Flags[i] = EditorGUI.TextField(new Rect(rect.x + 20, y, rect.width - 60, EditorGUIUtility.singleLineHeight), entry.Flags[i]);
					if (GUI.Button(new Rect(rect.x + rect.width - 35, y, 30, EditorGUIUtility.singleLineHeight), "-"))
					{
						entry.Flags.RemoveAt(i);
						break;
					}
					y += EditorGUIUtility.singleLineHeight + 2;
				}
				
				if (GUI.Button(new Rect(rect.x + 20, y, 100, EditorGUIUtility.singleLineHeight), "Add Flag"))
				{
					entry.Flags.Add("");
				}
			};
			_flagCombinationsList.onAddCallback = (ReorderableList list) =>
			{
				_flagCombinations.Add(new FlagCombinationEntry());
			};
		}
		
		private void OnGUI()
		{
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
			
			GUILayout.Label("Enum Configuration", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			
			_enumName = EditorGUILayout.TextField("Enum Name", _enumName);
			_namespace = EditorGUILayout.TextField("Namespace", _namespace);
			_backingType = (BackingTypeOption)EditorGUILayout.EnumPopup("Backing Type", _backingType);
			_enumType = (EnumTypeOption)EditorGUILayout.EnumPopup("Enum Type", _enumType);
			
			if (_enumType == EnumTypeOption.Flags)
			{
				_attributes = EnumAttributeFlags.Flags;
			}
			else
			{
				_attributes = EnumAttributeFlags.None;
			}
			
			EditorGUILayout.Space();
			GUILayout.Label("Enum Values", EditorStyles.boldLabel);
			
			if (_enumType == EnumTypeOption.Simple || _enumType == EnumTypeOption.Flags)
			{
				_enumKeysList.DoLayoutList();
				
				if (_enumType == EnumTypeOption.Flags)
				{
					EditorGUILayout.Space();
					_flagCombinationsList.DoLayoutList();
				}
			}
			else if (_enumType == EnumTypeOption.CustomValues)
			{
				_customValuesList.DoLayoutList();
			}
			
			EditorGUILayout.Space();
			
			if (GUILayout.Button("Generate Enum", GUILayout.Height(30)))
			{
				GenerateEnum();
			}
			
			if (!string.IsNullOrEmpty(_generatedCode))
			{
				EditorGUILayout.Space();
				GUILayout.Label("Generated Code:", EditorStyles.boldLabel);
				
				_outputScrollPosition = EditorGUILayout.BeginScrollView(_outputScrollPosition, GUILayout.Height(200));
				EditorGUILayout.TextArea(_generatedCode, GUILayout.ExpandHeight(true));
				EditorGUILayout.EndScrollView();
				
				if (GUILayout.Button("Copy to Clipboard"))
				{
					EditorGUIUtility.systemCopyBuffer = _generatedCode;
					Debug.Log("Enum code copied to clipboard!");
				}
			}
			
			EditorGUILayout.EndScrollView();
		}
		
		private void GenerateEnum()
		{
			try
			{
				_generatedCode = _backingType switch
				{
					BackingTypeOption.Byte => GenerateEnumWithType<byte>(),
					BackingTypeOption.SByte => GenerateEnumWithType<sbyte>(),
					BackingTypeOption.Short => GenerateEnumWithType<short>(),
					BackingTypeOption.UShort => GenerateEnumWithType<ushort>(),
					BackingTypeOption.Int => GenerateEnumWithType<int>(),
					BackingTypeOption.UInt => GenerateEnumWithType<uint>(),
					BackingTypeOption.Long => GenerateEnumWithType<long>(),
					BackingTypeOption.ULong => GenerateEnumWithType<ulong>(),
					_ => throw new ArgumentException("Invalid backing type")
				};
			}
			catch (Exception ex)
			{
				_generatedCode = $"Error generating enum:\n{ex.Message}";
				Debug.LogError($"Enum generation failed: {ex.Message}");
			}
		}
		
		private string GenerateEnumWithType<T>() where T : struct, IEquatable<T>, IComparable<T>
		{
			EnumGenParams<T> genParams;
			
			if (_enumType == EnumTypeOption.CustomValues)
			{
				var customValues = ParseCustomValues<T>();
				genParams = new EnumGenParams<T>(_enumName, _attributes, customValues, _namespace);
			}
			else
			{
				var baseKeys = ParseEnumKeys();
				Dictionary<string, HashSet<string>> flagCombinations = null;
				
				if (_enumType == EnumTypeOption.Flags)
				{
					flagCombinations = ParseFlagCombinations();
				}
				
				genParams = new EnumGenParams<T>(_enumName, _attributes, baseKeys, _namespace, flagCombinations);
			}
			
			using (var generator = new EnumSourceGenerator())
			{
				return generator.CreateEnumSource(genParams);
			}
		}
		
		private HashSet<string> ParseEnumKeys()
		{
			var keys = new HashSet<string>();
			
			foreach (var key in _enumKeys)
			{
				var trimmed = key.Trim();
				if (!string.IsNullOrEmpty(trimmed))
				{
					keys.Add(trimmed);
				}
			}
			
			return keys;
		}
		
		private Dictionary<string, T> ParseCustomValues<T>() where T : struct, IEquatable<T>, IComparable<T>
		{
			var customValues = new Dictionary<string, T>();
			
			foreach (var entry in _customValues)
			{
				var key = entry.Key.Trim();
				var valueStr = entry.Value.Trim();
				
				if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(valueStr))
					continue;
				
				var value = (T)Convert.ChangeType(valueStr, typeof(T));
				customValues[key] = value;
			}
			
			return customValues;
		}
		
		private Dictionary<string, HashSet<string>> ParseFlagCombinations()
		{
			if (_flagCombinations == null || _flagCombinations.Count == 0)
			{
				return null;
			}
			
			var combinations = new Dictionary<string, HashSet<string>>();
			
			foreach (var entry in _flagCombinations)
			{
				var combinationName = entry.Name.Trim();
				if (string.IsNullOrEmpty(combinationName)) continue;
				
				var flags = new HashSet<string>();
				foreach (var flag in entry.Flags)
				{
					var trimmedFlag = flag.Trim();
					if (!string.IsNullOrEmpty(trimmedFlag))
					{
						flags.Add(trimmedFlag);
					}
				}
				
				if (flags.Count > 0)
				{
					combinations[combinationName] = flags;
				}
			}
			
			return combinations.Count > 0 ? combinations : null;
		}
	}
}
