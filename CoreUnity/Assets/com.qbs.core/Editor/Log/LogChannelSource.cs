namespace QBS.Core.Editor
{
    /// <summary>
    ///     One <c>LogChannels.json</c> and where it came from.
    /// </summary>
    public class LogChannelSource
    {
        /// <summary>The owning package's name, or <see cref="LogChannelResolver.ProjectOwner" />.</summary>
        public string Owner { get; set; }

        /// <summary>The path as the window shows it.</summary>
        public string DisplayPath { get; set; }

        /// <summary>The absolute path the manifest is read from and saved to.</summary>
        public string FullPath { get; set; }

        public bool IsPackage { get; set; }

        /// <summary>
        ///     Whether this project can change it: the project's own, or an embedded or local package's.
        ///     A package installed from a registry, git or a tarball is read-only here.
        /// </summary>
        public bool IsEditable { get; set; }

        public LogChannelManifest Manifest { get; set; }

        /// <summary>Changed in the window and not yet saved.</summary>
        public bool IsDirty { get; set; }
    }
}
