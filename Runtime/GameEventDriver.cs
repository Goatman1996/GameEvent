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

        private static List<string> registerBridge_Assembly = new List<string>();
        private static List<IRegisterBridge> registerBridgeList = new List<IRegisterBridge>();

        [Obsolete("Use Initialize(string assemblyName, bool throwOnError) Instead", true)]
        public static void Initialize(string assemblyName)
        {
            InternalInitialize(assemblyName, true);
        }

        public static void Initialize(string assemblyName, bool throwOnError)
        {
            InternalInitialize(assemblyName, throwOnError);
        }

        public static void InternalInitialize(string assemblyName, bool throwOnError)
        {
            if (registerBridge_Assembly.Contains(assemblyName)) return;

            var assembly = Assembly.Load(assemblyName);
            var type = assembly.GetType($"{InjectedNameSpace}.{InjectedClazz}", throwOnError);
            if (type != null)
            {
                var registerBridge = Activator.CreateInstance(type) as IRegisterBridge;
                registerBridge.StaticRegister();
                registerBridge_Assembly.Add(assemblyName);
                registerBridgeList.Add(registerBridge);
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
                for (int j = 0; j < registerBridgeList.Count; j++)
                {
                    var registerBridge = registerBridgeList[j];
                    registerBridge.Register(target);
                }
            }
            unCheckAssetList.Clear();
            needCheckAsset = false;
        }

        private static void DoRemove()
        {
            for (int i = 0; i < willRemoveList.Count; i++)
            {
                var target = willRemoveList[i];
                for (int j = 0; j < registerBridgeList.Count; j++)
                {
                    var registerBridge = registerBridgeList[j];
                    registerBridge.Unregister(target);
                }
            }
            willRemoveList.Clear();
            hasWilRemove = false;
        }

        private static bool isTasking = false;
        public static bool IsTasking { get => isTasking; }
        public static List<Task> taskList = new List<Task>();
        public static async Task InvokeTask<T>(this T arg) where T : IGameTask
        {
            if (isTasking)
            {
                throw new Exception($"[GameEvent] Not Allow [InvokeTask] When Still Has Running Task.");
            }
            isTasking = true;
            taskList.Clear();

            if (needCheckAsset) CheckAssetAndRegister();

            isEventing = true;
            arg.ToString();

            if (hasWilRemove) DoRemove();

            await Task.WhenAll(taskList);
            isTasking = false;
        }
    }
}