using System;

namespace QBS.Core
{
	[Flags]
	public enum LogChannel
	{
		None = 0,
		Network = 1 << 0,
		AI = 1 << 1,
		Physics = 1 << 2,
		Setup = 1 << 3,
		UI = 1 << 4,
		All = ~0,
	}
	
	public static partial class Log
	{
		public static int UniqueChannelsCount => 5;
		
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
		
		// Brian Kernighan's algorithm
		public static int GetActiveLogChannelCount(this LogChannel channel)
		{
			var count = 0;
			var intVal = (uint) channel;
			while (intVal != 0)
			{
				// Clears the lowest set bit each iteration.
				intVal &= intVal - 1; 
				count++;
			}

			return Math.Min(count, UniqueChannelsCount);
		}
	}
}