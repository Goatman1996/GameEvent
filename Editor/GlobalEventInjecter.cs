using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace GameEvent
{
    [InitializeOnLoad]
    public static class GlobalEventInjecter
    {
        static GlobalEventInjecter()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnBeforeAssemblyReload()
        {
            try
            {
                InjectEvent("./Library/ScriptAssemblies", GameEventSettings.Instance.assemblyList.ToArray());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        public static void InjectEvent(string dir, params string[] dllFileArray)
        {
            bool isJumping = false;
            MethodUsageCache usageCache = new MethodUsageCache();

            Injecter.DoBackUpDirCreateOneTime(dir);

            Dictionary<string, Injecter> injectList = new Dictionary<string, Injecter>();
            foreach (var dllFileName in dllFileArray)
            {
                if (injectList.ContainsKey(dllFileName)) continue;

                var dllPath = $"{dir}/{dllFileName}";
                dllPath = Path.ChangeExtension(dllPath, ".dll");
                if (File.Exists(dllPath) == false) continue;

                var injecter = new Injecter(dllPath);
                injectList.Add(dllFileName, injecter);
            }

            foreach (var injecter in injectList.Values)
            {
                injecter.PrepareIo();
            }
            try
            {
                foreach (var injecter in injectList.Values)
                {
                    injecter.CheckInjected();
                }
                bool isAllInjected = true;
                foreach (var injecter in injectList.Values)
                {
                    if (injecter.hasInjected == false)
                    {
                        isAllInjected = false;
                        break;
                    }
                }
                if (isAllInjected)
                {
                    isJumping = true;
                    // Debug.Log("跳过");
                    goto Finish;
                }
                foreach (var injecter in injectList.Values)
                {
                    injecter.BuildEventCache(usageCache);
                }
                foreach (var injecter in injectList.Values)
                {
                    injecter.BuildUsageCache(usageCache);
                }
                foreach (var injecter in injectList.Values)
                {
                    injecter.SetCache(usageCache);
                }
                foreach (var injecter in injectList.Values)
                {
                    injecter.New_InjectAllUsage();
                }
                foreach (var injecter in injectList.Values)
                {
                    injecter.Write();
                }
                foreach (var injecter in injectList.Values)
                {
                    injecter.EnsureClose();
                }
            Finish:
                if (GameEventSettings.Instance.needInjectedLog && !isJumping)
                {
                    var logContent = "[GameEvent] 注入完成".ToColor(Color.green);
                    Debug.Log($"{logContent}\n{usageCache.Print()}");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                foreach (var injecter in injectList.Values)
                {
                    injecter.EnsureClose();
                }
                // Fake Delete
                Injecter.DeleteBackUpOneTime(dir);
            }
            return;
        }

        [UnityEditor.Callbacks.PostProcessScene]
        private static void AutoInjectAssemblys()
        {
            var targetDir = "./Library/PlayerScriptAssemblies";
            if (!Directory.Exists(targetDir))
            {
                targetDir = "./Library/Bee/PlayerScriptAssemblies";
            }
            if (!Directory.Exists(targetDir))
            {
                targetDir = "./Library/ScriptAssemblies";
            }
            GameEvent.GlobalEventInjecter.InjectEvent(targetDir, GameEventSettings.Instance.assemblyList.ToArray());
        }
    }
}
