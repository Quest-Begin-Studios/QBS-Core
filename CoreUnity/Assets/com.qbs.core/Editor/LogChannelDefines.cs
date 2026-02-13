using UnityEngine;

namespace QBS.Core.Editor
{
	[CreateAssetMenu(fileName = "LogChannelDefines")]
	public class LogChannelDefines : ScriptableObject
	{
		public string[] Channels;

		private void OnValidate()
		{
			if (Channels.Length == 0)
			{
				Channels = new[] { LogEditorConstants.DefaultLogChannel };
			}
		}
	}
}