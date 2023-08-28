using Mono.Cecil;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using System.Threading.Tasks;

namespace GameEvent
{
    internal class ILModifier_Static
    {
        internal static void InjectStatic(Dictionary<string, ILMethod_Static_Task> evtCollection, MethodDefinition invoker, AssemblyDefinition assemblyDef)
        {
            var invoker_IL = invoker.Body.GetILProcessor();

            Instruction final_Ret = invoker_IL.Create(OpCodes.Ret);
            Instruction next_Line = invoker_IL.Create(OpCodes.Ldarg_2);
            var count = 0;
            foreach (var iLMethod in evtCollection.Values)
            {
                invoker_IL.Append(next_Line);
                {
                    // 生成新的 写一个块的第一句
                    next_Line = invoker_IL.Create(OpCodes.Ldarg_2);
                }
                invoker_IL.Append(invoker_IL.Create(OpCodes.Isinst, iLMethod.paramDef.ParameterType));
                // 失败后跳转
                if (count == evtCollection.Count - 1)
                {
                    // 这是最后一个块
                    invoker_IL.Append(invoker_IL.Create(OpCodes.Brfalse_S, final_Ret));
                }
                else
                {
                    // 这不是最后一个块 ，后面还有，所以跳转下一个块
                    invoker_IL.Append(invoker_IL.Create(OpCodes.Brfalse_S, next_Line));
                }
                foreach (var method in iLMethod.methodDef_Public_List)
                {
                    if (method.Parameters[0].ParameterType.IsValueType)
                    {
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Ldarg_1));
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Ldarg_2));
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Unbox_Any, iLMethod.paramDef.ParameterType));
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Call, method));
                        var addMethod = typeof(List<Task>).GetMethod("Add", new[] { typeof(Task) });
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Callvirt, assemblyDef.MainModule.ImportReference(addMethod)));
                    }
                    else
                    {
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Ldarg_1));
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Ldarg_2));
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Isinst, iLMethod.paramDef.ParameterType));
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Call, method));
                        var addMethod = typeof(List<Task>).GetMethod("Add", new[] { typeof(Task) });
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Callvirt, assemblyDef.MainModule.ImportReference(addMethod)));
                    }
                }
                invoker_IL.Append(invoker_IL.Create(OpCodes.Ret));
                count++;
            }
            invoker_IL.Append(final_Ret);
        }

        internal static void InjectStatic(Dictionary<string, ILMethod_Static> evtCollection, MethodDefinition invoker, AssemblyDefinition assemblyDef)
        {
            var invoker_IL = invoker.Body.GetILProcessor();

            Instruction final_Ret = invoker_IL.Create(OpCodes.Ret);
            Instruction next_Line = invoker_IL.Create(OpCodes.Ldarg_1);
            var count = 0;
            foreach (var iLMethod in evtCollection.Values)
            {
                invoker_IL.Append(next_Line);
                {
                    // 生成新的 写一个块的第一句
                    next_Line = invoker_IL.Create(OpCodes.Ldarg_1);
                }
                invoker_IL.Append(invoker_IL.Create(OpCodes.Isinst, iLMethod.paramDef.ParameterType));
                // 失败后跳转
                if (count == evtCollection.Count - 1)
                {
                    // 这是最后一个块
                    invoker_IL.Append(invoker_IL.Create(OpCodes.Brfalse_S, final_Ret));
                }
                else
                {
                    // 这不是最后一个块 ，后面还有，所以跳转下一个块
                    invoker_IL.Append(invoker_IL.Create(OpCodes.Brfalse_S, next_Line));
                }
                foreach (var method in iLMethod.methodDef_Public_List)
                {
                    if (method.Parameters[0].ParameterType.IsValueType)
                    {
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Ldarg_1));
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Unbox_Any, iLMethod.paramDef.ParameterType));
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Call, method));
                    }
                    else
                    {
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Ldarg_1));
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Isinst, iLMethod.paramDef.ParameterType));
                        invoker_IL.Append(invoker_IL.Create(OpCodes.Call, method));
                    }
                }
                invoker_IL.Append(invoker_IL.Create(OpCodes.Ret));
                count++;
            }
            invoker_IL.Append(final_Ret);
        }

        internal static Dictionary<string, ILMethod_Static> CollectingEvt(AssemblyDefinition assemblyDef)
        {
            Dictionary<string, ILMethod_Static> collection = new Dictionary<string, ILMethod_Static>();

            foreach (var type in assemblyDef.MainModule.Types)
            {
                CollectingEvtInternal(type, collection);
            }
            foreach (var iLmethod in collection.Values)
            {
                iLmethod.GenerateILCode(assemblyDef);
            }

            return collection;
        }

        internal static Dictionary<string, ILMethod_Static_Task> CollectingTask(AssemblyDefinition assemblyDef)
        {
            Dictionary<string, ILMethod_Static_Task> collection = new Dictionary<string, ILMethod_Static_Task>();

            foreach (var type in assemblyDef.MainModule.Types)
            {
                CollectingTaskInternal(type, collection);
            }
            foreach (var iLmethod in collection.Values)
            {
                iLmethod.GenerateILCode(assemblyDef);
            }

            return collection;
        }

        private static void CollectingEvtInternal(TypeDefinition type, Dictionary<string, ILMethod_Static> collection)
        {
            foreach (var nestedType in type.NestedTypes)
            {
                CollectingEvtInternal(nestedType, collection);
            }
            if (type.IsClass == false) return;

            if (InjecterUtil.NeedInjectEvent(type, true) == false)
            {
                return;
            }

            foreach (var method in type.Methods)
            {
                if (method.IsStatic == false) continue;
                if (method.IsConstructor) continue;
                if (method.Parameters.Count != 1) continue;

                foreach (var attri in method.CustomAttributes)
                {
                    if (attri.AttributeType.FullName == typeof(GameEvent.GameEventAttribute).FullName)
                    {
                        var paramDef = method.Parameters[0];
                        var paramTypeDef = paramDef.ParameterType.Resolve();
                        bool paramIsGameEvent = false;
                        foreach (var iface in paramTypeDef.Interfaces)
                        {
                            if (iface.InterfaceType.FullName == typeof(GameEvent.IGameEvent).FullName)
                            {
                                paramIsGameEvent = true;
                                break;
                            }
                        }

                        if (paramIsGameEvent)
                        {
                            var evtName = paramDef.ParameterType.FullName;
                            if (collection.ContainsKey(evtName) == false)
                            {
                                var iLMethod = new ILMethod_Static();
                                iLMethod.paramDef = paramDef;
                                iLMethod.methodDef_List = new List<MethodDefinition>();
                                iLMethod.methodDef_Public_List = new List<MethodDefinition>();
                                collection.Add(evtName, iLMethod);
                            }
                            collection[evtName].methodDef_List.Add(method);
                        }
                    }
                }
            }
        }

        private static void CollectingTaskInternal(TypeDefinition type, Dictionary<string, ILMethod_Static_Task> collection)
        {
            foreach (var nestedType in type.NestedTypes)
            {
                CollectingTaskInternal(nestedType, collection);
            }
            if (type.IsClass == false) return;
            if (InjecterUtil.NeedInjectTask(type, true) == false)
            {
                return;
            }
            foreach (var method in type.Methods)
            {

                if (method.IsStatic == false) continue;
                if (method.IsConstructor) continue;
                if (method.Parameters.Count != 1) continue;

                foreach (var attri in method.CustomAttributes)
                {
                    if (attri.AttributeType.FullName == typeof(GameEvent.GameEventAttribute).FullName)
                    {
                        var paramDef = method.Parameters[0];
                        var paramTypeDef = paramDef.ParameterType.Resolve();
                        bool paramIsGameTask = false;
                        foreach (var iface in paramTypeDef.Interfaces)
                        {
                            if (iface.InterfaceType.FullName == typeof(GameEvent.IGameTask).FullName)
                            {
                                if (method.ReturnType.FullName.StartsWith(typeof(System.Threading.Tasks.Task).FullName))
                                {
                                    paramIsGameTask = true;
                                    break;
                                }
                            }
                        }

                        if (paramIsGameTask)
                        {
                            var evtName = paramDef.ParameterType.FullName;
                            if (collection.ContainsKey(evtName) == false)
                            {
                                var iLMethod = new ILMethod_Static_Task();
                                iLMethod.paramDef = paramDef;
                                iLMethod.methodDef_List = new List<MethodDefinition>();
                                iLMethod.methodDef_Public_List = new List<MethodDefinition>();
                                collection.Add(evtName, iLMethod);
                            }
                            collection[evtName].methodDef_List.Add(method);
                        }
                    }
                }
            }
        }
    }
}