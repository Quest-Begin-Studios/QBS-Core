using System;

namespace QBS.Core
{
	[Flags]
	public enum LogChannel
	{
		None = 0,
		Network = 1 << 1,
		AI = 1 << 2,
		Physics = 1 << 3,
		Setup = 1 << 4,
		UI = 1 << 5,
	}

	public static partial class Log
	{
		public static string ToStringNoBox(this LogChannel channel)
		{
			return channel switch
			{
				LogChannel.None => string.Empty,
				LogChannel.Network => nameof(LogChannel.Network),
				LogChannel.AI => nameof(LogChannel.AI),
				LogChannel.Physics => nameof(LogChannel.Physics),
				LogChannel.Setup => nameof(LogChannel.Setup),
				LogChannel.UI => nameof(LogChannel.UI),
				_ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
			};
		}
	}
}