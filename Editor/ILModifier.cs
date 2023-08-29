using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace GameEvent
{
    internal class ILModifier
    {
        internal static void ModifyType(TypeDefinition type, AssemblyDefinition assemblyDef)
        {
            // 添加 接口__Instance_Invoker__
            Import_Invoker(type, assemblyDef);
            //  实现 接口__Instance_Invoker__
            Implement_Invoker_Method(type, assemblyDef);
        }

        private static void Import_Invoker(TypeDefinition type, AssemblyDefinition assemblyDef)
        {
            var invokerTypeRef = assemblyDef.MainModule.ImportReference(typeof(GameEvent.__Instance_Invoker__));
            var invoker = new InterfaceImplementation(invokerTypeRef);
            type.Interfaces.Add(invoker);
        }

        private static void Implement_Invoker_Method(TypeDefinition type, AssemblyDefinition assemblyDef)
        {
            bool isMono = InjecterUtil.TypeIsMono(type);

            var import_Method_Dic = new Dictionary<string, ILMethod>();

            foreach (var method in type.Methods)
            {
                if (method.IsStatic) continue;
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
                            // =====新的·
                            var evtName = paramDef.ParameterType.FullName;
                            if (import_Method_Dic.ContainsKey(evtName) == false)
                            {
                                var iLMethod = new ILMethod();
                                iLMethod.paramDef = paramDef;
                                iLMethod.methodDef_List = new List<MethodDefinition>();
                                iLMethod.methodDef_NeedEnable_List = new List<MethodDefinition>();
                                import_Method_Dic.Add(evtName, iLMethod);
                            }

                            var needEnable = (bool)attri.ConstructorArguments[0].Value;
                            if (needEnable)
                            {
                                import_Method_Dic[evtName].methodDef_NeedEnable_List.Add(method);
                            }
                            else
                            {
                                import_Method_Dic[evtName].methodDef_List.Add(method);
                            }
                            // =====新的·
                        }
                    }
                }
            }

            var __Invoke__Name = "__Invoke__";

            var __Invoke__Attris = MethodAttributes.Public;
            __Invoke__Attris |= MethodAttributes.HideBySig;
            __Invoke__Attris |= MethodAttributes.NewSlot;
            __Invoke__Attris |= MethodAttributes.Virtual;
            __Invoke__Attris |= MethodAttributes.Final;

            var __Invoke__Ret = assemblyDef.MainModule.ImportReference(typeof(bool));

            var __Invoke__Param_Evt = new ParameterDefinition(assemblyDef.MainModule.ImportReference(typeof(IGameEvent)));
            __Invoke__Param_Evt.Name = "evt";

            var __Invoke__Param_isActive = new ParameterDefinition(assemblyDef.MainModule.ImportReference(typeof(bool)));
            __Invoke__Param_isActive.Name = "isActiveAndEnabled";

            var __Invoke__ = new MethodDefinition(__Invoke__Name, __Invoke__Attris, __Invoke__Ret);
            __Invoke__.Parameters.Add(__Invoke__Param_Evt);
            __Invoke__.Parameters.Add(__Invoke__Param_isActive);

            var PreserveCtor = typeof(UnityEngine.Scripting.PreserveAttribute).GetConstructors()[0];
            var Preserve = new CustomAttribute(assemblyDef.MainModule.ImportReference(PreserveCtor));
            __Invoke__.CustomAttributes.Add(Preserve);

            var __Invoke__IL = __Invoke__.Body.GetILProcessor();

            // =====新的·
            var final_False = __Invoke__IL.Create(OpCodes.Ldc_I4_0);
            var final_Ret = __Invoke__IL.Create(OpCodes.Ret);

            var next_IL = __Invoke__IL.Create(OpCodes.Ldarg_1);
            if (isMono)
            {
                __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_0));
                var monoIsNull = typeof(UnityEngine.Object).GetMethod("op_Implicit");
                var monoIsNull_Ref = assemblyDef.MainModule.ImportReference(monoIsNull);
                __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Call, monoIsNull_Ref));
                __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Brtrue_S, next_IL));

                __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldc_I4_1));
                __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ret));
            }

            var iLMethodCounter = 0;
            foreach (var iLMethod in import_Method_Dic.Values)
            {
                __Invoke__IL.Append(next_IL);
                {
                    // 生成新的 写一个块的第一句
                    next_IL = __Invoke__IL.Create(OpCodes.Ldarg_1);
                }
                __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Isinst, iLMethod.paramDef.ParameterType));
                // 失败后跳转
                if (iLMethodCounter == import_Method_Dic.Count - 1)
                {
                    // 这是最后一个块
                    __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Brfalse_S, final_False));
                }
                else
                {
                    // 这不是最后一个块 ，后面还有，所以跳转下一个块
                    __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Brfalse_S, next_IL));
                }

                var block_False = __Invoke__IL.Create(OpCodes.Ldc_I4_0);
                var block_Ret = __Invoke__IL.Create(OpCodes.Ret);

                // 调用 事件
                foreach (var methodDef in iLMethod.methodDef_List)
                {
                    if (methodDef.Parameters[0].ParameterType.IsValueType)
                    {
                        __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_0));
                        __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_1));
                        __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Unbox_Any, iLMethod.paramDef.ParameterType));
                        __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Call, methodDef));
                    }
                    else
                    {
                        __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_0));
                        __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_1));
                        __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Isinst, iLMethod.paramDef.ParameterType));
                        __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Call, methodDef));
                    }
                }

                if (iLMethod.methodDef_NeedEnable_List.Count != 0)
                {
                    // 这是有Mono Enable事件的调用
                    __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_0));
                    var getMethod = typeof(UnityEngine.Behaviour).GetProperty("isActiveAndEnabled").GetMethod;
                    var getMethodRef = assemblyDef.MainModule.ImportReference(getMethod);
                    __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Call, getMethodRef));
                    __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Starg_S, __Invoke__Param_isActive));

                    // 当这个Mono Disable的时候，跳转至此Block的Return;
                    __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_2));
                    __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Brfalse_S, block_False));

                    foreach (var methodDef in iLMethod.methodDef_NeedEnable_List)
                    {
                        if (methodDef.Parameters[0].ParameterType.IsValueType)
                        {
                            __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_0));
                            __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_1));
                            __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Unbox_Any, iLMethod.paramDef.ParameterType));
                            __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Call, methodDef));
                        }
                        else
                        {
                            __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_0));
                            __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_1));
                            __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Isinst, iLMethod.paramDef.ParameterType));
                            __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Call, methodDef));
                        }
                    }
                }

                __Invoke__IL.Append(block_False);
                __Invoke__IL.Append(block_Ret);

                iLMethodCounter++;
            }
            __Invoke__IL.Append(final_False);
            __Invoke__IL.Append(final_Ret);
            // =====新的·

            type.Methods.Add(__Invoke__);
        }

        internal static void ModifyConstructor(TypeDefinition type, AssemblyDefinition assemblyDef)
        {
            foreach (var m in type.Methods)
            {
                if (m.IsConstructor)
                {
                    if (m.IsStatic) continue;

                    if (m.Body == null)
                    {
                        continue;
                    }

                    var ilProcesser = m.Body.GetILProcessor();
                    var firstLine = m.Body.Instructions[0];
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
                    var registerMethod = InjecterUtil.GetRegisterMethod();
                    var methodReference = assemblyDef.MainModule.ImportReference(registerMethod);
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, methodReference));
                }
            }
        }
    }
}