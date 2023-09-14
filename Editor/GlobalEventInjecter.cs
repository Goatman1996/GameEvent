using System.Collections.Generic;
using UnityEngine;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using System.Linq;
using System;
using System.IO;
using System.Text;
using Mono.Cecil.Pdb;

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
            InjectEvent("./Library/ScriptAssemblies", GameEventSettings.Instance.assemblyList.ToArray());
        }

        private static StringBuilder reportSb;
        public static void InjectEvent(string dir, params string[] dllFileArray)
        {
            bool isJumping = false;
            MethodUsageCache usageCache = new MethodUsageCache();

            Injecter.DoBackUpDirCreateOneTime(dir);
            List<Func<TypeDefinition, EventModifier>> ModifierProviderList = new List<Func<TypeDefinition, EventModifier>>();

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
                // UnityEngine.Debug.Log(usageCache.Print());
                foreach (var injecter in injectList.Values)
                {
                    var modifierProvider = injecter.BuildEventModifier();
                    ModifierProviderList.Add(modifierProvider);
                }
                foreach (var injecter in injectList.Values)
                {
                    injecter.BuildRegisterBridge();
                }
                foreach (var injecter in injectList.Values)
                {
                    injecter.InjectUsage((t) =>
                    {
                        foreach (var provider in ModifierProviderList)
                        {
                            var modifier = provider?.Invoke(t);
                            if (modifier != null) return modifier;
                        }
                        return null;
                    });
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
                    Debug.Log("[GameEvent] 注入完成");
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
        public static void AutoInjectAssemblys()
        {
            var targetDir = "./Library/PlayerScriptAssemblies";
            if (Directory.Exists(targetDir))
            {
                GameEvent.GlobalEventInjecter.InjectEvent(targetDir, GameEventSettings.Instance.assemblyList.ToArray());
            }
        }
    }
}
