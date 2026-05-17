using System.IO;
using UnityEditor;
using UnityEngine;

namespace QBS.Core.Editor
{
    [InitializeOnLoad]
    public static class LogPackageAutoSetup
    {
        static LogPackageAutoSetup()
        {
            if (!LogDLLsExist())
                EditorApplication.delayCall += RunAutoSetup;
        }

        private static bool LogDLLsExist()
        {
            var runtimeDll = Path.Combine(Application.dataPath, "Plugins", "Log", "Runtime", "Log.dll");
            return File.Exists(runtimeDll);
        }

        private static void RunAutoSetup()
        {
            Debug.Log("[QBS] Log DLLs not found — running auto-setup.");

            var success = LogSourceCompiler.GenerateLogDLLsHeadless();

            if (success)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                EditorUtility.RequestScriptReload();
                Debug.Log("[QBS] Log DLLs generated successfully.");
            }
            else
            {
                Debug.LogError("[QBS] Log DLL auto-setup failed. Use Tools > QBS > Logs > Log Source Compiler to generate manually.");
            }
        }
    }
}