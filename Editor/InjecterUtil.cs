using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace GameEvent
{
    internal static class InjecterUtil
    {
        internal static bool NeedInjectEvent(TypeDefinition targetType, bool isStatic)
        {
            foreach (var method in targetType.Methods)
            {
                if (isStatic == false && method.IsStatic) continue;
                if (isStatic && method.IsStatic == false) continue;
                if (method.IsConstructor) continue;
                if (method.Parameters.Count != 1) continue;

                foreach (var attri in method.CustomAttributes)
                {
                    if (attri.AttributeType.FullName == typeof(GameEvent.GameEventAttribute).FullName)
                    {
                        var paramDef = method.Parameters[0];
                        var paramTypeDef = paramDef.ParameterType.Resolve();
                        foreach (var iface in paramTypeDef.Interfaces)
                        {
                            if (iface.InterfaceType.FullName == typeof(GameEvent.IGameEvent).FullName)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        internal static bool NeedInjectTask(TypeDefinition targetType, bool isStatic)
        {
            foreach (var method in targetType.Methods)
            {
                if (isStatic == false && method.IsStatic) continue;
                if (isStatic && method.IsStatic == false) continue;
                if (method.IsConstructor) continue;
                if (method.Parameters.Count != 1) continue;

                foreach (var attri in method.CustomAttributes)
                {
                    if (attri.AttributeType.FullName == typeof(GameEvent.GameEventAttribute).FullName)
                    {
                        var paramDef = method.Parameters[0];
                        var paramTypeDef = paramDef.ParameterType.Resolve();
                        foreach (var iface in paramTypeDef.Interfaces)
                        {
                            if (iface.InterfaceType.FullName == typeof(GameEvent.IGameTask).FullName)
                            {
                                if (method.ReturnType.FullName.StartsWith(typeof(System.Threading.Tasks.Task).FullName))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        internal static MethodInfo GetRegisterMethod()
        {
            return typeof(GameEvent.GameEventDriver).GetMethod("Register", new[] { typeof(object) });
        }

        internal static DefaultAssemblyResolver CreateAssemblyResolver()
        {
            HashSet<string> searchDir = new HashSet<string>();
            foreach (var path in (from asm in AppDomain.CurrentDomain.GetAssemblies()
                                  select Path.GetDirectoryName(asm.ManifestModule.FullyQualifiedName)).Distinct())
            {
                try
                {
                    // UnityEngine.Debug.Log(path);
                    if (searchDir.Contains(path) == false)
                    {
                        searchDir.Add(path);
                    }
                }
                catch { }
            }

            DefaultAssemblyResolver resole = new DefaultAssemblyResolver();
            foreach (var referenceDir in searchDir)
            {
                resole.AddSearchDirectory(referenceDir);
            }

            return resole;
        }

        internal static bool HasInjected(AssemblyDefinition assemblyDefinition)
        {
            var injected = assemblyDefinition.MainModule.Types.Any((t)
                => t.FullName == $"{GameEventDriver.InjectedNameSpace}.{GameEventDriver.InjectedClazz}");
            return injected;
        }

        internal static bool TypeIsMono(TypeDefinition type)
        {
            var typeIndex = type;
            while (typeIndex.BaseType != null)
            {
                var baseType = typeIndex.BaseType;
                if (baseType.FullName == "UnityEngine.MonoBehaviour")
                {
                    return true;
                }

                typeIndex = baseType.Resolve();
            }

            return false;
        }

        internal static TypeDefinition MarkAsInjected(AssemblyDefinition assemblyDef)
        {
            var injectedTypeDef = new TypeDefinition(GameEventDriver.InjectedNameSpace, GameEventDriver.InjectedClazz, Mono.Cecil.TypeAttributes.Class | Mono.Cecil.TypeAttributes.Public, assemblyDef.MainModule.TypeSystem.Object);

            assemblyDef.MainModule.Types.Add(injectedTypeDef);

            return injectedTypeDef;
        }

        internal static MethodDefinition MarkStaticInterface(AssemblyDefinition assemblyDef, TypeDefinition injectedTypeDef)
        {
            // 添加__Static__Invoker__接口
            var invokerTypeRef = assemblyDef.MainModule.ImportReference(typeof(GameEvent.__Static__Invoker__));
            var invoker = new InterfaceImplementation(invokerTypeRef);
            injectedTypeDef.Interfaces.Add(invoker);

            // 实现构造函数
            var __Constructor__Name = ".ctor";

            var __Constructor__Attris = Mono.Cecil.MethodAttributes.Public;
            __Constructor__Attris |= Mono.Cecil.MethodAttributes.HideBySig;
            __Constructor__Attris |= Mono.Cecil.MethodAttributes.SpecialName;
            __Constructor__Attris |= Mono.Cecil.MethodAttributes.RTSpecialName;

            var __Constructor__Ret = assemblyDef.MainModule.ImportReference(typeof(void));

            var __Constructor__ = new MethodDefinition(__Constructor__Name, __Constructor__Attris, __Constructor__Ret);

            var __Constructor__IL = __Constructor__.Body.GetILProcessor();
            __Constructor__IL.Append(__Constructor__IL.Create(OpCodes.Ldarg_0));
            var obj_ctor = typeof(object).GetConstructor(new Type[] { });
            var obj_ctor_Red = assemblyDef.MainModule.ImportReference(obj_ctor);
            __Constructor__IL.Append(__Constructor__IL.Create(OpCodes.Call, obj_ctor_Red));
            __Constructor__IL.Append(__Constructor__IL.Create(OpCodes.Ret));

            injectedTypeDef.Methods.Add(__Constructor__);

            // 添加__Static__Invoker__接口是实现
            var __Invoke__Name = "__Invoke__";

            var __Invoke__Attris = Mono.Cecil.MethodAttributes.Public;
            __Invoke__Attris |= Mono.Cecil.MethodAttributes.HideBySig;
            __Invoke__Attris |= Mono.Cecil.MethodAttributes.NewSlot;
            __Invoke__Attris |= Mono.Cecil.MethodAttributes.Virtual;
            __Invoke__Attris |= Mono.Cecil.MethodAttributes.Final;

            var __Invoke__Ret = assemblyDef.MainModule.ImportReference(typeof(void));

            var __Invoke__Param_Evt = new ParameterDefinition(assemblyDef.MainModule.ImportReference(typeof(IGameEvent)));
            __Invoke__Param_Evt.Name = "evt";

            var __Invoke__ = new MethodDefinition(__Invoke__Name, __Invoke__Attris, __Invoke__Ret);
            __Invoke__.Parameters.Add(__Invoke__Param_Evt);

            injectedTypeDef.Methods.Add(__Invoke__);

            return __Invoke__;
        }
    }
}