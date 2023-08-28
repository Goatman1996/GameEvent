using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GameEvent
{
    public static class GameEventDriver
    {
        public const string InjectedNameSpace = "GameEvent";
        public const string InjectedClazz = "___Injected___";

        private static bool hasStaticInvoker = false;
        private static __Static__Invoker__ staticInvoker;

        public static void Initialize(string assemblyName)
        {
            var assembly = Assembly.Load(assemblyName);
            var type = assembly.GetType($"{InjectedNameSpace}.{InjectedClazz}");
            if (type != null)
            {
                staticInvoker = Activator.CreateInstance(type) as __Static__Invoker__;
                hasStaticInvoker = true;
            }

        }


        private static List<__Instance_Invoker__> unCheckAssetList = new List<__Instance_Invoker__>(32);
        private static List<__Instance_Invoker__> managedList = new List<__Instance_Invoker__>(32);
        private static bool needCheckAsset = false;

        public static void Register(object target)
        {
            if (target is __Instance_Invoker__)
            {
                unCheckAssetList.Add(target as __Instance_Invoker__);
                needCheckAsset = true;
            }
        }

        public static void Unregister(object target)
        {
            unCheckAssetList.Remove(target as __Instance_Invoker__);
            managedList.Remove(target as __Instance_Invoker__);
        }

        private static bool hasWilRemove = false;
        private static List<int> willRemoveList = new List<int>(32);
        public static void Invoke<T>(this T arg) where T : IGameEvent
        {
            if (hasStaticInvoker) staticInvoker.__Invoke__(arg);

            if (needCheckAsset) CheckAsset();

            var listCount = managedList.Count;
            for (int i = 0; i < listCount; i++)
            {
                var invoker = managedList[i];

                if (invoker.__Invoke__(arg))
                {
                    willRemoveList.Add(i);
                    hasWilRemove = true;
                }
            }

            if (hasWilRemove) RemoveList();
        }

        private static void RemoveList()
        {
            for (int i = willRemoveList.Count - 1; i >= 0; i--)
            {
                var removeIndex = willRemoveList[i];
                managedList.RemoveAt(removeIndex);
            }
            willRemoveList.Clear();
            hasWilRemove = false;
        }

        private static void CheckAsset()
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
                managedList.Add(target as __Instance_Invoker__);
            }
            unCheckAssetList.Clear();
            needCheckAsset = false;
        }
    }
}