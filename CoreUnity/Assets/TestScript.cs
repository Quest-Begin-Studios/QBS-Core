using System;
using UnityEngine;

public class TestScript : MonoBehaviour
{
	private void Start()
	{
		LogLoadedAssemblyDetails();
	}

	private void LogLoadedAssemblyDetails()
	{
		var assemblyArray = AppDomain.CurrentDomain.GetAssemblies();
		Debug.Log($"Total Assemblies Loaded: {assemblyArray.Length}");
		Debug.Log("=== Assembly Details ===");
		
		foreach (var assembly in assemblyArray)
		{
			Debug.Log($"<color=cyan>Name: {assembly.GetName().Name}</color>");
			Debug.Log($"  Full Name: {assembly.FullName}");
			Debug.Log($"  Location: {(assembly.IsDynamic ? "Dynamic/In-Memory" : assembly.Location)}");
			Debug.Log($"  Types Count: {assembly.GetTypes().Length}");
			Debug.Log("---");
		}
	}
}
