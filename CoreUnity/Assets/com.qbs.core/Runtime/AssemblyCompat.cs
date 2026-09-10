using System.Collections.Generic;
using System.Reflection;
#if UNITY_6000_4_OR_NEWER
using UnityEngine;
using UnityEngine.Assemblies;
#else
using System;
#endif

namespace QBS.Core
{
    /// <summary>
    ///     Wraps the assembly lookup APIs that differ across Unity versions.
    ///     Unity 6000.4 added <c>UnityEngine.Assemblies.CurrentAssemblies</c> and <c>Assembly.GetLoadedAssemblyPath()</c>,
    ///     which are required under CoreCLR where assemblies can be loaded from memory and <c>Assembly.Location</c> is empty.
    ///     Earlier versions fall back to the AppDomain equivalents.
    /// </summary>
    public static class AssemblyCompat
    {
        /// <summary>
        ///     Gets the assemblies loaded into the current execution context.
        /// </summary>
        /// <returns>A collection of loaded assemblies</returns>
        public static IEnumerable<Assembly> GetLoadedAssemblies()
        {
#if UNITY_6000_4_OR_NEWER
            return CurrentAssemblies.GetLoadedAssemblies();
#else
            return AppDomain.CurrentDomain.GetAssemblies();
#endif
        }

        /// <summary>
        ///     Gets the location of the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly to look up the location of</param>
        /// <returns>The path to the assembly, or null/empty when the path is not known</returns>
        public static string GetAssemblyPath(Assembly assembly)
        {
#if UNITY_6000_4_OR_NEWER
            return assembly.GetLoadedAssemblyPath();
#else
            return assembly.Location;
#endif
        }
    }
}
