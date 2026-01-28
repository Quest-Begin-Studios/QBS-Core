using QBS.Core;
using UnityEngine;

namespace QBS.Examples
{
	public class LogUsageExample : MonoBehaviour
	{
		private void Start()
		{
			BasicLogging();
			ChannelLogging();
			TaggedLogging();
			InfoSnipeExample();
			AssertExample();
		}

		private void BasicLogging()
		{
			Log.Info("This is an info message");
			Log.Warning("This is a warning message");
			Log.Error("This is an error message");
		}

		private void ChannelLogging()
		{
			Log.WithChannel(LogChannel.Network).Info("Connected to server");
			Log.WithChannel(LogChannel.Physics).Warning("Collision detected");
		}

		private void TaggedLogging()
		{
			Log.Info("Player spawned", context: this, tag: "Gameplay");
			Log.Warning("Low health", context: this, tag: "Combat");
		}

		private void InfoSnipeExample() => Log.InfoSnipe("IMPORTANT: This message stands out!");

		private void AssertExample()
		{
			var health = 100;
			Log.Assert(health > 0, "Health should be positive");
		}

		private void FluentAPIExample() => Log.WithContext(this)
			.WithChannel(LogChannel.Save)
			.WithTag("Critical")
			.Error("Player died");

		private void RuntimeFilteringExample()
		{
			Log.MinimumLevel = LogLevel.Warning;

			Log.SetChannelEnabled(LogChannel.Network, false);
		}
	}
}