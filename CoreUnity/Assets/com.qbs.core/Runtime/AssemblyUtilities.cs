using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Callbacks;

namespace QBS.Core
{

	[Flags]
	public enum UnityAssemblyType
	{
		None = 0,
		AssemblyCSharp = 1,
		AssemblyCSharpEditor = 2,
		AssemblyCSharpEditorFirstPass = 4,
		AssemblyCSharpFirstPass = 8,
		RuntimeOnly = AssemblyCSharp | AssemblyCSharpFirstPass,
		EditorOnly = AssemblyCSharpEditor | AssemblyCSharpEditorFirstPass,
		All = AssemblyCSharp | AssemblyCSharpEditor | AssemblyCSharpEditorFirstPass | AssemblyCSharpFirstPass,
	}

	/// <summary>
	///     A utility class, PredefinedAssemblyUtil, provides methods to interact with predefined assemblies.
	///     It allows to get all types in the current AppDomain that implement from a specific Interface type.
	///     For more details,
	///     <see href="https://docs.unity3d.com/2023.3/Documentation/Manual/ScriptCompileOrderFolders.html">
	///         visit Unity
	///         Documentation
	///     </see>
	/// </summary>
	public static class AssemblyUtilities
	{
		private static Dictionary<UnityAssemblyType, Assembly> _assemblyMap;


#if UNITY_EDITOR

		/// <summary>
		///     Invalidate cache of assemblies on script reload
		/// </summary>
		[DidReloadScripts]
		private static void OnAssemblyReloaded() => _assemblyMap = null;

#endif

		/// <summary>
		///     Maps the assembly name to the corresponding AssemblyType.
		/// </summary>
		/// <param name="assemblyName">Name of the assembly.</param>
		/// <returns>AssemblyType corresponding to the assembly name, null if no match.</returns>
		/// <remarks>These assemblies are limited to what the developer works with on top of the editor code/framework. 
		///		These will not contain assemblies the Editor itself uses (like System.dll, UnityEngine.dll, etc.). <br/> <br/>
		///     For more details,
		///     <see href="https://docs.unity3d.com/2023.3/Documentation/Manual/ScriptCompileOrderFolders.html">
		///         visit Unity
		///         Documentation
		///     </see>
		/// </remarks>
		private static UnityAssemblyType GetAssemblyType(string assemblyName) => assemblyName switch
		{
			"Assembly-CSharp-firstpass" => UnityAssemblyType.AssemblyCSharpFirstPass,
			"Assembly-CSharp" => UnityAssemblyType.AssemblyCSharp,
			"Assembly-CSharp-Editor-firstpass" => UnityAssemblyType.AssemblyCSharpEditorFirstPass,
			"Assembly-CSharp-Editor" => UnityAssemblyType.AssemblyCSharpEditor,
			_ => UnityAssemblyType.None,
		};

		private static Dictionary<UnityAssemblyType, Assembly> GetAllAssemblies()
		{
			var newAssemblyMap = new Dictionary<UnityAssemblyType, Assembly>();
			var assemblyArray = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblyArray)
			{
				var assemblyType = GetAssemblyType(assembly.GetName().Name);
				if (assemblyType != UnityAssemblyType.None)
				{
					newAssemblyMap.Add(assemblyType, assembly);
				}
			}
			return newAssemblyMap;
		}

		public static List<Type> GetTypes(UnityAssemblyType assemblyTypesToSearch = UnityAssemblyType.All,
			Type baseType = null, bool concreteOnly = true)
		{
			_assemblyMap ??= GetAllAssemblies();
			var filteredAssemblies = new List<Assembly>();
			foreach (var (assemblyType, assembly) in _assemblyMap)
			{
				if ((assemblyTypesToSearch & assemblyType) != 0)
				{
					filteredAssemblies.Add(assembly);
				}
			}

			var types = new List<Type>();
			foreach (var assembly in filteredAssemblies)
			{
				var typesInAssembly = assembly.GetTypes();
				foreach (var type in typesInAssembly)
				{
					// If the baseType filter is null or the current type
					// is not assignable from the baseType filter, skip type
					if (baseType != null)
					{
						if (baseType.IsAssignableFrom(type) && type != baseType)
						{
							continue;
						}
					}

					//TODO: Add better filtering for types of "types", only abstract, enums only etc
					
					// If concreteOnly is true and the current type is abstract or
					// an interface, skip type.
					if (concreteOnly && (type.IsAbstract || type.IsInterface))
					{
						continue;
					}
					types.Add(type);
				}
			}

			return types;
		}

	}
}