using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace QBS.Core.Editor
{
    public class EnumGeneratorComponent
    {
        private string _enumName = "MyEnum";
        private string _namespace = "MyNamespace";
        private EnumAttributeFlags _attributes = EnumAttributeFlags.None;
        private BackingType _backingType = BackingType.Int;
        private EnumTypeOption _enumType = EnumTypeOption.Simple;


        private ReorderableList _enumKeysList;
        private ReorderableList _customValuesList;
        private ReorderableList _flagCombinationsList;

        private Vector2 _outputScrollPosition;

        public enum BackingType
        {
            Byte,
            SByte,
            Short,
            UShort,
            Int,
            UInt,
            Long,
			ULong,
        }

        public enum EnumTypeOption
        {
            Simple,
            Flags,
			CustomValues,
        }

        [Serializable]
        public class CustomValueEntry
        {
            public string Key = "";
            public string Value = "";
        }

        [Serializable]
        public class FlagCombinationEntry
        {
            public string Name = "";
            public List<string> Flags = new();
        }

        public string GeneratedCode { get; private set; } = "";
        public List<string> EnumKeys { get; } = new();
        public List<FlagCombinationEntry> FlagCombinations { get; } = new();
        public List<CustomValueEntry> CustomValues { get; } = new();

        // Written into every generated enum before EnumKeys and not editable in the window. Other
        // assemblies compile against these names and their values, so the order here is load-bearing.
        public List<string> ReservedKeys { get; } = new();
        public List<FlagCombinationEntry> ReservedFlagCombinations { get; } = new();

        public void ConfigureEnumProperties(string enumName, string namespaceStr, EnumTypeOption enumType, BackingType backingType)
        {
            _enumName = enumName;
            _enumType = enumType;
            _backingType = backingType;
            _namespace = namespaceStr;
            _attributes = enumType == EnumTypeOption.Flags ? EnumAttributeFlags.Flags : EnumAttributeFlags.None;
        }

        public void ConfigureEnumKeys(List<string> enumKeys = null, List<FlagCombinationEntry> flagCombinations = null,
            List<CustomValueEntry> customValueEntries = null)
        {
            if (enumKeys != null)
            {
                EnumKeys.Clear();
                EnumKeys.AddRange(enumKeys);
            }

            if (flagCombinations != null)
            {
                FlagCombinations.Clear();
                FlagCombinations.AddRange(flagCombinations);
            }

            if (customValueEntries != null)
            {
                CustomValues.Clear();
                CustomValues.AddRange(customValueEntries);
            }
        }

        /// <summary>
        ///     Sets the members that are injected into the generated enum whatever the window's own lists hold.
        ///     They are emitted first and in this order, so appending to a reserved set leaves the values of
        ///     the members already in it untouched; inserting or reordering renumbers every member after the
        ///     change and silently reinterprets any mask already serialized against the old numbering.
        /// </summary>
        public void ConfigureReservedKeys(List<string> reservedKeys = null,
            List<FlagCombinationEntry> reservedFlagCombinations = null)
        {
            if (reservedKeys != null)
            {
                ReservedKeys.Clear();
                ReservedKeys.AddRange(reservedKeys);
            }

            if (reservedFlagCombinations != null)
            {
                ReservedFlagCombinations.Clear();
                ReservedFlagCombinations.AddRange(reservedFlagCombinations);
            }
        }

        /// <summary>
        ///     Every member name the next <see cref="GenerateEnum" /> emits, in the order it assigns their
        ///     values: reserved keys, then configured keys, then reserved and configured flag combinations.
        ///     Helper generation reads this, so a caller cannot hand the writers a list the enum disagrees with.
        ///     Meaningless for <see cref="EnumTypeOption.CustomValues" />, which names its members itself.
        /// </summary>
        public List<string> GetGeneratedKeyNames()
        {
            var names = new List<string>(ParseEnumKeys());

            var combinations = ParseFlagCombinations();
            if (combinations != null)
            {
                names.AddRange(combinations.Keys);
            }

            return names;
        }

        public void Initialize()
        {
            _enumKeysList = new ReorderableList(EnumKeys, typeof(string))
            {
                drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Enum Keys"); },
                drawElementCallback = (rect, index, _, _) =>
                {
                    var key = EnumKeys[index];
                    EnumKeys[index] = EditorGUI.TextField(rect, key);
                },
                onAddCallback = _ => { EnumKeys.Add(""); },
                onRemoveCallback = list => { EnumKeys.RemoveAt(list.index); }
            };

            _customValuesList = new ReorderableList(CustomValues, typeof(CustomValueEntry), true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height), "Key");
                    EditorGUI.LabelField
                        (new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.5f, rect.height), "Value");
                },
                drawElementCallback = (rect, index, _, _) =>
                {
                    var entry = CustomValues[index];
                    entry.Key = EditorGUI.TextField
                    (
                        new Rect(rect.x, rect.y, rect.width * 0.48f, EditorGUIUtility.singleLineHeight),
                        entry.Key
                    );
                    entry.Value = EditorGUI.TextField
                    (
                        new Rect(rect.x + rect.width * 0.52f, rect.y, rect.width * 0.48f, EditorGUIUtility.singleLineHeight),
                        entry.Value
                    );
                },
                onAddCallback = _ => { CustomValues.Add(new CustomValueEntry()); },
                onRemoveCallback = list => { CustomValues.RemoveAt(list.index); }
            };

            _flagCombinationsList = new ReorderableList(FlagCombinations, typeof(FlagCombinationEntry), true, true, true, true)
            {
                drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Flag Combinations (Optional)"); },
                elementHeightCallback = index =>
                {
                    var entry = FlagCombinations[index];
                    return EditorGUIUtility.singleLineHeight * 2
                           + (entry.Flags.Count + 1) * (EditorGUIUtility.singleLineHeight + 2) + 10;
                },
                drawElementCallback = (rect, index, _, _) =>
                {
                    var entry = FlagCombinations[index];
                    var y = rect.y + 2;

                    EditorGUI.LabelField(new Rect(rect.x, y, 100, EditorGUIUtility.singleLineHeight), "Name:");
                    entry.Name = EditorGUI.TextField
                    (
                        new Rect(rect.x + 100, y, rect.width - 100, EditorGUIUtility.singleLineHeight),
                        entry.Name
                    );

                    y += EditorGUIUtility.singleLineHeight + 4;

                    EditorGUI.LabelField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Flags:");
                    y += EditorGUIUtility.singleLineHeight + 2;

                    // Collect all available flags
                    // WARN: uses a fuckton of memory, essentially one list per composite flag.
                    var availableFlags = GetAvailableFlags(index);

                    for (var i = 0; i < entry.Flags.Count; i++)
                    {
                        var currentFlag = entry.Flags[i];
                        var selectedIndex = availableFlags.IndexOf(currentFlag);
                        if (selectedIndex == -1 && !string.IsNullOrEmpty(currentFlag))
                        {
                            // If current flag is not in available list, add it temporarily
                            availableFlags.Insert(0, currentFlag);
                            selectedIndex = 0;
                        }
                        else if (selectedIndex == -1)
                        {
                            selectedIndex = 0;
                        }

                        var newIndex = EditorGUI.Popup
                        (
                            new Rect(rect.x + 20, y, rect.width - 60, EditorGUIUtility.singleLineHeight),
                            selectedIndex,
                            availableFlags.ToArray()
                        );

                        if (newIndex >= 0 && newIndex < availableFlags.Count)
                        {
                            entry.Flags[i] = availableFlags[newIndex];
                        }

                        if (GUI.Button(new Rect(rect.x + rect.width - 35, y, 30, EditorGUIUtility.singleLineHeight), "-"))
                        {
                            entry.Flags.RemoveAt(i);
                            break;
                        }

                        y += EditorGUIUtility.singleLineHeight + 2;
                    }

                    if (GUI.Button(new Rect(rect.x + 20, y, 100, EditorGUIUtility.singleLineHeight), "Add Flag"))
                    {
                        entry.Flags.Add(availableFlags.Count > 0 ? availableFlags[0] : "");
                    }
                },
                onAddCallback = _ => { FlagCombinations.Add(new FlagCombinationEntry()); }
            };
        }

        public void DrawConfigurationGUI()
        {
            GUILayout.Label("Enum Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _enumName = EditorGUILayout.TextField("Enum Name", _enumName);
            _namespace = EditorGUILayout.TextField("Namespace", _namespace);
            _backingType = (BackingType) EditorGUILayout.EnumPopup("Backing Type", _backingType);
            _enumType = (EnumTypeOption) EditorGUILayout.EnumPopup("Enum Type", _enumType);

            _attributes = _enumType == EnumTypeOption.Flags ? EnumAttributeFlags.Flags : EnumAttributeFlags.None;

            EditorGUILayout.Space();
        }

        public void DrawEnumListGUI()
        {
            GUILayout.Label("Enum Values", EditorStyles.boldLabel);

            if (_enumType == EnumTypeOption.Simple || _enumType == EnumTypeOption.Flags)
            {
                DrawReservedKeysGUI();
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
        }

        private void DrawReservedKeysGUI()
        {
            if (ReservedKeys.Count == 0 && ReservedFlagCombinations.Count == 0)
            {
                return;
            }

            EditorGUILayout.HelpBox
            (
                "These members are injected into every generated enum, ahead of the keys below and in this "
                + "order. They cannot be edited here: other assemblies are compiled against their names and "
                + "values. Add to the set in code, never insert into or reorder it.",
                MessageType.Info
            );

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginVertical("box");

            var isFlags = _enumType == EnumTypeOption.Flags;
            for (var i = 0; i < ReservedKeys.Count; i++)
            {
                EditorGUILayout.LabelField(isFlags ? $"Bit {i}" : $"{i}", ReservedKeys[i]);
            }

            foreach (var combination in ReservedFlagCombinations)
            {
                EditorGUILayout.LabelField(combination.Name, string.Join(" | ", combination.Flags));
            }

            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
        }

        public void DrawOutputGUI(bool showCopyButton = true)
        {
            if (!string.IsNullOrEmpty(GeneratedCode))
            {
                EditorGUILayout.Space();
                GUILayout.Label("Generated Code:", EditorStyles.boldLabel);

                _outputScrollPosition = EditorGUILayout.BeginScrollView(_outputScrollPosition, GUILayout.Height(200));
                EditorGUILayout.TextArea(GeneratedCode, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                if (showCopyButton && GUILayout.Button("Copy to Clipboard"))
                {
                    EditorGUIUtility.systemCopyBuffer = GeneratedCode;
                    Debug.Log("Enum code copied to clipboard!");
                }
            }
        }

        public bool GenerateEnum()
        {
            try
            {
                GeneratedCode = _backingType switch
                {
                    BackingType.Byte => GenerateEnumWithType<byte>(),
                    BackingType.SByte => GenerateEnumWithType<sbyte>(),
                    BackingType.Short => GenerateEnumWithType<short>(),
                    BackingType.UShort => GenerateEnumWithType<ushort>(),
                    BackingType.Int => GenerateEnumWithType<int>(),
                    BackingType.UInt => GenerateEnumWithType<uint>(),
                    BackingType.Long => GenerateEnumWithType<long>(),
                    BackingType.ULong => GenerateEnumWithType<ulong>(),
					_ => throw new ArgumentException("Invalid backing type"),
                };
            }
            catch (Exception ex)
            {
                GeneratedCode = $"Error generating enum:\n{ex.Message}";
                Debug.LogError($"Enum generation failed: {ex.Message}\n{ex.StackTrace}");
                return false;
            }

            return true;
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

            using var generator = new EnumSourceGenerator();
            return generator.CreateEnumSource(genParams);
        }

        private HashSet<string> ParseEnumKeys()
        {
            // Reserved keys go in first and the set keeps insertion order, which is what pins a reserved
            // member to the same flag value no matter what the window's own list holds.
            var keys = new HashSet<string>();

            foreach (var reservedKey in ReservedKeys)
            {
                var trimmedReservedKey = reservedKey.Trim();
                if (!string.IsNullOrEmpty(trimmedReservedKey))
                {
                    keys.Add(trimmedReservedKey);
                }
            }

            foreach (var key in EnumKeys)
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

            foreach (var entry in CustomValues)
            {
                var key = entry.Key.Trim();
                var valueStr = entry.Value.Trim();

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(valueStr))
                {
                    continue;
                }

                var value = (T) Convert.ChangeType(valueStr, typeof(T));
                customValues[key] = value;
            }

            return customValues;
        }

        private Dictionary<string, HashSet<string>> ParseFlagCombinations()
        {
            if (FlagCombinations.Count == 0 && ReservedFlagCombinations.Count == 0)
            {
                return null;
            }

            var combinations = new Dictionary<string, HashSet<string>>();

            foreach (var entry in ReservedFlagCombinations.Concat(FlagCombinations))
            {
                var combinationName = entry.Name.Trim();
                if (string.IsNullOrEmpty(combinationName))
                {
                    continue;
                }

                var flags = new HashSet<string>();
                foreach (var flag in entry.Flags)
                {
                    var trimmedFlag = flag.Trim();
                    if (!string.IsNullOrEmpty(trimmedFlag))
                    {
                        flags.Add(trimmedFlag);
                    }
                }

                //A reserved combination is read first, so a configured one reusing its name is the
                //duplicate and loses; the reserved definition is the one other assemblies compiled against.
                if (flags.Count > 0 && !combinations.ContainsKey(combinationName))
                {
                    combinations[combinationName] = flags;
                }
            }

            return combinations.Count > 0 ? combinations : null;
        }

        private List<string> GetAvailableFlags(int currentCombinationIndex)
        {
            var availableFlags = new List<string>();

            // Add the reserved flags, which a configured combination is free to build on
            foreach (var reservedKey in ReservedKeys)
            {
                var trimmedReservedKey = reservedKey.Trim();
                if (!string.IsNullOrEmpty(trimmedReservedKey))
                {
                    availableFlags.Add(trimmedReservedKey);
                }
            }

            foreach (var reservedCombination in ReservedFlagCombinations)
            {
                var reservedCombinationName = reservedCombination.Name.Trim();
                if (!string.IsNullOrEmpty(reservedCombinationName))
                {
                    availableFlags.Add(reservedCombinationName);
                }
            }

            // Add all unique flags from EnumKeys
            foreach (var key in EnumKeys)
            {
                var trimmed = key.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    availableFlags.Add(trimmed);
                }
            }

            // Add composite flags from other FlagCombinations
            for (var i = 0; i < FlagCombinations.Count; i++)
            {
                if (i == currentCombinationIndex)
                {
                    continue; // Skip the current combination being edited
                }

                var combinationName = FlagCombinations[i].Name.Trim();
                if (!string.IsNullOrEmpty(combinationName))
                {
                    availableFlags.Add(combinationName);
                }
            }

            return availableFlags;
        }
    }
}