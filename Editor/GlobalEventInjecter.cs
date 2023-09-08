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
            var injecter = new Injecter(dllPath);
            injecter.Inject();
            return;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnReload()
        {
            InjectEvent("./Library/ScriptAssemblies/Assembly-CSharp.dll");
        }

        [UnityEditor.Callbacks.PostProcessScene]
        public static void AutoInjectAssemblys()
        {
            if (File.Exists("./Library/PlayerScriptAssemblies/Assembly-CSharp.dll"))
            {
                GameEvent.GlobalEventInjecter.InjectEvent("./Library/PlayerScriptAssemblies/Assembly-CSharp.dll");
            }
        }
    }
}