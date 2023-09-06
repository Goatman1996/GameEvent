using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace GameEvent
{
    public static class GameEventDriver
    {
        public static bool isEventing = false;

        public const string InjectedNameSpace = "GameEvent";
        public const string InjectedClazz = "___Injected___";

        private static bool Initialized = false;
        private static IRegisterBridge registerBridge;

        public static void Initialize(string assemblyName)
        {
            var assembly = Assembly.Load(assemblyName);
            var type = assembly.GetType($"{InjectedNameSpace}.{InjectedClazz}", true);
            if (type != null)
            {
                registerBridge = Activator.CreateInstance(type) as IRegisterBridge;
                Initialized = true;
            }
        }

        private static List<object> unCheckAssetList = new List<object>(32);
        private static bool needCheckAsset = false;

        private static List<object> willRemoveList = new List<object>(32);
        private static bool hasWilRemove = false;

        public static void Register(object target)
        {
            unCheckAssetList.Add(target);
            needCheckAsset = true;
        }

        public static void Unregister(object target)
        {
            var removed = unCheckAssetList.Remove(target);
            if (removed == false)
            {
                hasWilRemove = true;
                willRemoveList.Add(target);
            }
        }

        public static void Invoke<T>(this T arg) where T : IGameEvent
        {
            if (needCheckAsset) CheckAssetAndRegister();

            isEventing = true;
            arg.ToString();

            if (hasWilRemove) DoRemove();
        }

        private static void CheckAssetAndRegister()
        {
            for (int i = 0; i < unCheckAssetList.Count; i++)
            {
                var target = unCheckAssetList[i];
                if (target is MonoBehaviour)
                {
                    var mono = target as MonoBehaviour;
                    if (mono == null) continue;
                    if (mono.gameObject.scene.isLoaded == false) continue;
                }
                registerBridge.Register(target);
            }
            unCheckAssetList.Clear();
            needCheckAsset = false;
        }

        private static void DoRemove()
        {
            for (int i = 0; i < willRemoveList.Count; i++)
            {
                var target = willRemoveList[i];
                registerBridge.Unregister(target);
            }
            willRemoveList.Clear();
            hasWilRemove = false;
        }
    }
}