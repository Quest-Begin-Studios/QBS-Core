using System.Collections.Generic;

namespace QBS.Core.Editor
{
	public enum SegmentDirection
	{
		Up,
		Down,
	}

	/// <summary>
	///     A run of flags laid out from a fixed bit, one bit per key in list order, counting up or down.
	///     A blank key keeps its bit but emits no member, so retiring a flag never moves the ones after it.
	/// </summary>
	public readonly struct FlagSegment
	{
		public int StartBit { get; }
		public SegmentDirection Direction { get; }
		public IReadOnlyList<string> Keys { get; }

		public FlagSegment(int startBit, SegmentDirection direction, IReadOnlyList<string> keys)
		{
			StartBit = startBit;
			Direction = direction;
			Keys = keys;
		}

		public int BitAt(int index)
		{
			return Direction == SegmentDirection.Up ? StartBit + index : StartBit - index;
		}
	}
}
