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
    public class GlobalEventInjecter
    {


        private static StringBuilder reportSb;
        public static void InjectEvent(string dllPath)
        {
            reportSb = new StringBuilder();
            reportSb.AppendLine($"[GameEvent] 开始注入");

            // 备份，并使用备份来注入
            var bakeDllPath = InjecterIo.BakeDll(dllPath);

            FileStream dllStream = InjecterIo.CreateDllReadWriteStream(bakeDllPath);
            AssemblyDefinition assemblyDefinition = InjecterIo.ReadAssembly(dllStream);
            try
            {
                var injected = InjecterUtil.HasInjected(assemblyDefinition);
                if (injected)
                {
                    InjecterIo.EnsureIoClose(assemblyDefinition, dllStream);
                    InjecterIo.DeleteBake(dllPath);
                    return;
                }

                foreach (var type in assemblyDefinition.MainModule.Types)
                {
                    TryInjectType(type, assemblyDefinition);
                }

                var mark_Type_As_Static_Invoker = InjecterUtil.MarkAsInjected(assemblyDefinition);
                var staticInvoker = InjecterUtil.MarkStaticInterface(assemblyDefinition, mark_Type_As_Static_Invoker);
                var staticEvtCollection = ILModifier_Static.CollectingEvt(assemblyDefinition);
                ILModifier_Static.InjectStatic(staticEvtCollection, staticInvoker, assemblyDefinition);

                InjecterIo.WriteAssembly(dllPath, assemblyDefinition);

                reportSb.AppendLine($"[GameEvent] 注入完成");
                Debug.Log(reportSb);
            }
            catch (Exception e)
            {
                Debug.LogError(reportSb);
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                InjecterIo.EnsureIoClose(assemblyDefinition, dllStream);
                InjecterIo.DeleteBake(dllPath);
            }
        }

        private static void TryInjectType(TypeDefinition type, AssemblyDefinition assemblyDefinition)
        {
            foreach (var nestedType in type.NestedTypes)
            {
                TryInjectType(nestedType, assemblyDefinition);
            }
            if (type.IsClass == false) return;
            if (type.IsValueType == true) return;
            if (InjecterUtil.NeedInjectClass(type, false) == false)
            {
                return;
            }
            else
            {
                ILModifier.ModifyType(type, assemblyDefinition);
                reportSb.AppendLine($"[GameEvent] 注入{type.FullName}");
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnReload()
        {
            InjectEvent("./Library/ScriptAssemblies/Assembly-CSharp.dll");
        }

    }
}