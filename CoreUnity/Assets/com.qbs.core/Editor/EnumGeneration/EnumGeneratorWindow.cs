using UnityEditor;
using UnityEngine;

namespace QBS.Core.Editor
{
	public class EnumGeneratorWindow : EditorWindow
	{
		private EnumGeneratorComponent _enumGenerator;
		private Vector2 _scrollPosition;

		[MenuItem("Tools/QBS/Enum Generator")]
		public static void ShowWindow()
		{
			var window = GetWindow<EnumGeneratorWindow>("Enum Generator");
			window.minSize = new Vector2(600, 500);
		}

		private void OnEnable()
		{
			_enumGenerator = new EnumGeneratorComponent();
			_enumGenerator.Initialize();
		}

		private void OnGUI()
		{
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

			_enumGenerator.DrawConfigurationGUI();
			_enumGenerator.DrawEnumListGUI();
			if (GUILayout.Button("Generate Enum", GUILayout.Height(30)))
			{
				_enumGenerator.GenerateEnum();
			}
			_enumGenerator.DrawOutputGUI();

			EditorGUILayout.EndScrollView();
		}

	}
}