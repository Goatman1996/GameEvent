using System;
using System.Collections.Generic;
using System.IO;
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

        private static void InternalInitialize(string assemblyName, bool throwOnError)
        {
            if (registerBridge_Assembly.Contains(assemblyName)) return;

            Assembly assembly = null;
            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch (FileNotFoundException e)
            {
                if (throwOnError) throw e;
                else Debug.LogError($"Not Found Assembly {assemblyName}");
                return;
            }
            catch (Exception e)
            {
                if (throwOnError) throw e;
                else Debug.LogException(e);
                return;
            }

            var type = assembly.GetType($"{InjectedNameSpace}.{InjectedClazz}", throwOnError);
            if (type != null)
            {
                var registerBridge = Activator.CreateInstance(type) as IRegisterBridge;
                registerBridge.StaticRegister();
                registerBridge_Assembly.Add(assemblyName);
                registerBridgeList.Add(registerBridge);
            }
        }

        public static void RegisterEvent<T>(Action<T> target) where T : IGameEvent
        {
            GameEvent<T>.Event += target;
        }

        public static void UnregisterEvent<T>(Action<T> target) where T : IGameEvent
        {
            GameEvent<T>.Event -= target;
        }

        public static void RegisterTask<T>(Func<T, Task> target) where T : IGameTask
        {
            GameTask<T>.Event += target;
        }

        public static void UnregisterTask<T>(Func<T, Task> target) where T : IGameTask
        {
            GameTask<T>.Event -= target;
        }

        public static void Invoke<T>(this T arg) where T : IGameEvent
        {
            GameEvent<T>.Invoke(arg);
        }

        public static async Task InvokeTask<T>(this T arg) where T : IGameTask
        {
            await GameTask<T>.InvokeAsync(arg);
        }

        public static bool IsSceneObj(MonoBehaviour mono)
        {
            return !mono.gameObject.scene.isLoaded;
        }
    }
}