using System;
using System.Collections.Generic;

namespace QBS.Core.Editor
{
    /// <summary>
    ///     The contents of a <c>LogChannels.json</c>: the channels one package, or the project, adds to
    ///     <c>LogChannel</c>, pinned to bits. Channel <c>i</c> sits at <see cref="StartBit" /> plus or minus
    ///     <c>i</c> as <see cref="Direction" /> says, so the list only grows: a retired channel stays behind
    ///     as an empty string, holding its bit.
    /// </summary>
    [Serializable]
    public class LogChannelManifest
    {
        public const string FileName = "LogChannels.json";

        public int StartBit;
        public string Direction = nameof(SegmentDirection.Up);
        //Bits an upward manifest keeps for itself whether it uses them yet or not, so a package can grow
        //without landing on the first bit of whatever was built on top of it. Zero reserves nothing.
        public int Capacity;
        public List<string> Channels = new();
        public List<EnumGeneratorComponent.FlagCombinationEntry> Combinations = new();
    }
}
