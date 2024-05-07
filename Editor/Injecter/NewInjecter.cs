using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace GameEvent
{
    public partial class Injecter
    {
        private bool injectedBridge = false;

        public void New_InjectAllUsage()
        {
            this.NewInjectBridge();

            foreach (var kv in this.usageCache.userType_EventUsage_Collection)
            {
                var gameEventUsage = kv.Value;
                if (gameEventUsage.usageType.Module != this.assemblyDefinition.MainModule)
                {
                    continue;
                }
                NewPrefixMonoMethod(gameEventUsage);
                NewInjectStaticMethod(gameEventUsage);

                NewInjectRegisterToCTOR(gameEventUsage);
            }
        }

        private void NewPrefixMonoMethod(GameEventUsage gameEventUsage)
        {
            if (!gameEventUsage.isMono) return;

            var sceneCheckerField = this.InjectSceneChecker(gameEventUsage);

            foreach (var kv in gameEventUsage.event_Instance_Usage_Cache)
            {
                var eventType = kv.Key;
                var methodList = kv.Value;
                foreach (var method in methodList)
                {
                    InjectMonoPrefixMethod(method, false, gameEventUsage.Customize_op_Implicit_, sceneCheckerField, false);
                }
            }
            foreach (var kv in gameEventUsage.event_Mono_Enable_Usage_Cache)
            {
                var eventType = kv.Key;
                var methodList = kv.Value;
                foreach (var method in methodList)
                {
                    InjectMonoPrefixMethod(method, true, gameEventUsage.Customize_op_Implicit_, sceneCheckerField, false);
                }
            }

            foreach (var kv in gameEventUsage.task_Instance_Usage_Cache)
            {
                var eventType = kv.Key;
                var methodList = kv.Value;
                foreach (var method in methodList)
                {
                    InjectMonoPrefixMethod(method, false, gameEventUsage.Customize_op_Implicit_, sceneCheckerField, true);
                }
            }
            foreach (var kv in gameEventUsage.task_Mono_Enable_Usage_Cache)
            {
                var eventType = kv.Key;
                var methodList = kv.Value;
                foreach (var method in methodList)
                {
                    InjectMonoPrefixMethod(method, true, gameEventUsage.Customize_op_Implicit_, sceneCheckerField, true);
                }
            }
        }

        private FieldDefinition InjectSceneChecker(GameEventUsage gameEventUsage)
        {
            var type = gameEventUsage.usageType;
            var fieldAttri = Mono.Cecil.FieldAttributes.Private;
            var fieldType = assemblyDefinition.MainModule.ImportReference(typeof(bool));
            var fieldRef = new FieldDefinition($"__{type.Name}__SceneChecked__", fieldAttri, fieldType);
            type.Fields.Add(fieldRef);
            return fieldRef;
        }

        private void InjectMonoPrefixMethod(MethodDefinition method, bool needEnable, MethodDefinition Customize_op_Implicit_, FieldDefinition sceneCheckerField, bool isTask)
        {
            var eventType = method.Parameters[0].ParameterType;

            var ilProcesser = method.Body.GetILProcessor();
            var firstLine = method.Body.Instructions[0];

            var sceneConditionFirstLine = ilProcesser.Create(OpCodes.Ldarg_0);

            var enableBlockFirstLine = ilProcesser.Create(OpCodes.Ldarg_0);

            // if(!mono) unregister return;
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
            var monoIsExist = typeof(UnityEngine.Object).GetMethod("op_Implicit");
            var monoIsExist_Ref = assemblyDefinition.MainModule.ImportReference(monoIsExist);
            if (Customize_op_Implicit_ != null)
            {
                monoIsExist_Ref = assemblyDefinition.MainModule.ImportReference(Customize_op_Implicit_);
            }
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, monoIsExist_Ref));
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Brtrue_S, sceneConditionFirstLine));

            {
                // unregister
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldftn, method));
                var action_CTOR = GetEventActionConstructor(eventType);
                if (isTask)
                {
                    action_CTOR = GetTaskFuncConstructor(eventType);
                }
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Newobj, action_CTOR));
                var unregisterMethod = GetUnregisterEventMethod(eventType);
                if (isTask)
                {
                    unregisterMethod = GetUnregisterTaskMethod(eventType);
                }
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, unregisterMethod));
                if (isTask)
                {
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldnull));
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ret));
                }
                else
                {
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ret));
                }
            }

            // __SceneChecker__ == false    
            ilProcesser.InsertBefore(firstLine, sceneConditionFirstLine);
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldfld, sceneCheckerField));
            if (needEnable)
            {
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Brtrue_S, enableBlockFirstLine));
            }
            else
            {
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Brtrue_S, firstLine));
            }


            // SceneChecker = true
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldc_I4_1));
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Stfld, sceneCheckerField));

            // Scene scene = ((Component)this).get_gameObject().get_scene();
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
            var isSceneObj = typeof(GameEvent.GameEventDriver).GetMethod("IsSceneObj");
            var isSceneObj_Ref = assemblyDefinition.MainModule.ImportReference(isSceneObj);
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, isSceneObj_Ref));

            if (needEnable)
            {
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Brtrue_S, enableBlockFirstLine));
            }
            else
            {
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Brtrue_S, firstLine));
            }
            {
                // unregister
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldftn, method));
                var action_CTOR = GetEventActionConstructor(eventType);
                if (isTask)
                {
                    action_CTOR = GetTaskFuncConstructor(eventType);
                }
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Newobj, action_CTOR));
                var unregisterMethod = GetUnregisterEventMethod(eventType);
                if (isTask)
                {
                    unregisterMethod = GetUnregisterTaskMethod(eventType);
                }
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, unregisterMethod));
                if (isTask)
                {
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldnull));
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ret));
                }
                else
                {
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ret));
                }
            }

            if (!needEnable) return;

            ilProcesser.InsertBefore(firstLine, enableBlockFirstLine);
            var isEnableMethod = typeof(UnityEngine.Behaviour).GetProperty("isActiveAndEnabled").GetMethod;
            var isEnableMethodRef = assemblyDefinition.MainModule.ImportReference(isEnableMethod);
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, isEnableMethodRef));
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Brtrue_S, firstLine));
            if (isTask)
            {
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldnull));
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ret));
            }
            else
            {
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ret));
            }


        }

        private void NewInjectStaticMethod(GameEventUsage gameEventUsage)
        {
            foreach (var kv in gameEventUsage.event_Static_Usage_Cache)
            {
                var eventType = kv.Key;
                var methodList = kv.Value;
                foreach (var method in methodList)
                {
                    var staticWrapper = NewGenerate_StaticMethod_Register_Wrapper(method, false);
                    NewAppendStaticEventToRegisterBridge(staticWrapper);
                }
            }

            foreach (var kv in gameEventUsage.task_Static_Usage_Cache)
            {
                var eventType = kv.Key;
                var methodList = kv.Value;
                foreach (var method in methodList)
                {
                    var staticRegisterWrapper = NewGenerate_StaticMethod_Register_Wrapper(method, true);
                    NewAppendStaticTaskToRegisterBridge(staticRegisterWrapper);
                }
            }
        }

        private MethodDefinition NewGenerate_StaticMethod_Register_Wrapper(MethodDefinition method, bool isTask)
        {
            TypeReference eventType = method.Parameters[0].ParameterType;

            var methodName = $"{method.Name}__Wrapper";
            var methodAttri = Mono.Cecil.MethodAttributes.Public;
            methodAttri |= Mono.Cecil.MethodAttributes.HideBySig;
            methodAttri |= Mono.Cecil.MethodAttributes.Static;

            TypeReference methodRet = assemblyDefinition.MainModule.ImportReference(typeof(void));

            var methodWrapper = new MethodDefinition(methodName, methodAttri, methodRet);

            var ilProcesser = methodWrapper.Body.GetILProcessor();

            if (isTask)
            {
                ilProcesser.Append(ilProcesser.Create(OpCodes.Ldnull));
                ilProcesser.Append(ilProcesser.Create(OpCodes.Ldftn, method));

                var action_CTOR = GetTaskFuncConstructor(eventType);
                ilProcesser.Append(ilProcesser.Create(OpCodes.Newobj, action_CTOR));

                var registerMethod = GetRegisterTaskMethod(eventType);
                ilProcesser.Append(ilProcesser.Create(OpCodes.Call, registerMethod));

                // ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_0));
                // ilProcesser.Append(ilProcesser.Create(OpCodes.Call, method));
            }
            else
            {
                ilProcesser.Append(ilProcesser.Create(OpCodes.Ldnull));
                ilProcesser.Append(ilProcesser.Create(OpCodes.Ldftn, method));

                var action_CTOR = GetEventActionConstructor(eventType);
                ilProcesser.Append(ilProcesser.Create(OpCodes.Newobj, action_CTOR));

                var registerMethod = GetRegisterEventMethod(eventType);
                ilProcesser.Append(ilProcesser.Create(OpCodes.Call, registerMethod));

                // ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_0));
                // ilProcesser.Append(ilProcesser.Create(OpCodes.Call, method));
            }

            ilProcesser.Append(ilProcesser.Create(OpCodes.Ret));

            method.DeclaringType.Methods.Add(methodWrapper);

            return methodWrapper;
        }


        private void NewAppendStaticEventToRegisterBridge(MethodReference staticWrapper)
        {
            var ilProcesser = this.newStaticRegisterMethod.Body.GetILProcessor();
            var count = this.newStaticRegisterMethod.Body.Instructions.Count;
            var lastLine = this.newStaticRegisterMethod.Body.Instructions[count - 1];

            ilProcesser.InsertBefore(lastLine, ilProcesser.Create(OpCodes.Call, staticWrapper));
        }

        private void NewAppendStaticTaskToRegisterBridge(MethodReference staticWrapper)
        {
            var ilProcesser = this.newStaticRegisterMethod.Body.GetILProcessor();
            var count = this.newStaticRegisterMethod.Body.Instructions.Count;
            var lastLine = this.newStaticRegisterMethod.Body.Instructions[count - 1];

            ilProcesser.InsertBefore(lastLine, ilProcesser.Create(OpCodes.Call, staticWrapper));
        }

        private void NewInjectBridge()
        {
            var InjectedNameSpace = GameEventDriver.InjectedNameSpace;
            var InjectedClazz = GameEventDriver.InjectedClazz;

            var injectedFullName = $"{InjectedNameSpace}.{InjectedClazz}";
            var has = assemblyDefinition.MainModule.Types.FirstOrDefault(t => t.FullName == injectedFullName);
            if (has == null)
            {
                var typeAttri = Mono.Cecil.TypeAttributes.Class | Mono.Cecil.TypeAttributes.Public;
                var baseType = assemblyDefinition.MainModule.TypeSystem.Object;

                var injectedTypeDef = new TypeDefinition(InjectedNameSpace, InjectedClazz, typeAttri, baseType);

                var PreserveCtor = typeof(UnityEngine.Scripting.PreserveAttribute).GetConstructors()[0];
                var Preserve = new CustomAttribute(assemblyDefinition.MainModule.ImportReference(PreserveCtor));
                injectedTypeDef.CustomAttributes.Add(Preserve);

                assemblyDefinition.MainModule.Types.Add(injectedTypeDef);

                this.newBridgeType = injectedTypeDef;
            }
            else
            {
                this.injectedBridge = true;
                this.newBridgeType = has;
            }

            this.NewGenerate_iRegisterBridge();
            this.NewGenerate_CTOR();
            // this.Generate_Register();
            // this.Generate_Unregister();
            this.NewGenerate_StaticRegister();
        }

        private MethodDefinition newStaticRegisterMethod;
        private void NewGenerate_StaticRegister()
        {
            // 添加 Static Register
            var staticRegisterName = "StaticRegister";
            var has = this.newBridgeType.Methods.FirstOrDefault(m => m.Name == staticRegisterName);
            if (has != null)
            {
                this.newStaticRegisterMethod = has;
                return;
            }

            var staticRegisterAttri = Mono.Cecil.MethodAttributes.Public;
            staticRegisterAttri |= Mono.Cecil.MethodAttributes.HideBySig;
            staticRegisterAttri |= Mono.Cecil.MethodAttributes.NewSlot;
            staticRegisterAttri |= Mono.Cecil.MethodAttributes.Virtual;
            staticRegisterAttri |= Mono.Cecil.MethodAttributes.Final;

            var staticRegisterRet = assemblyDefinition.MainModule.ImportReference(typeof(void));

            var staticRegisterMethod = new MethodDefinition(staticRegisterName, staticRegisterAttri, staticRegisterRet);

            var ilProcesser = staticRegisterMethod.Body.GetILProcessor();
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ret));

            this.newBridgeType.Methods.Add(staticRegisterMethod);
            this.newStaticRegisterMethod = staticRegisterMethod;

        }

        private TypeDefinition newBridgeType;
        private void NewGenerate_iRegisterBridge()
        {
            if (this.injectedBridge) return;
            // 添加IRegisterBridge接口
            var invokerTypeRef = assemblyDefinition.MainModule.ImportReference(typeof(GameEvent.IRegisterBridge));
            var invoker = new InterfaceImplementation(invokerTypeRef);
            this.newBridgeType.Interfaces.Add(invoker);
        }

        private void NewGenerate_CTOR()
        {
            if (this.injectedBridge) return;
            // 实现构造函数
            var CTOR_Name = ".ctor";

            var CTOR_Attri = Mono.Cecil.MethodAttributes.Public;
            CTOR_Attri |= Mono.Cecil.MethodAttributes.HideBySig;
            CTOR_Attri |= Mono.Cecil.MethodAttributes.SpecialName;
            CTOR_Attri |= Mono.Cecil.MethodAttributes.RTSpecialName;

            var CTOR_Ret = assemblyDefinition.MainModule.ImportReference(typeof(void));

            var CTOR = new MethodDefinition(CTOR_Name, CTOR_Attri, CTOR_Ret);

            {
                var PreserveCtor = typeof(UnityEngine.Scripting.PreserveAttribute).GetConstructors()[0];
                var Preserve = new CustomAttribute(assemblyDefinition.MainModule.ImportReference(PreserveCtor));
                CTOR.CustomAttributes.Add(Preserve);
            }

            var ilProcessor = CTOR.Body.GetILProcessor();
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));
            var obj_ctor = typeof(object).GetConstructor(new Type[] { });
            var obj_ctor_Ref = assemblyDefinition.MainModule.ImportReference(obj_ctor);
            ilProcessor.Append(ilProcessor.Create(OpCodes.Call, obj_ctor_Ref));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));

            this.newBridgeType.Methods.Add(CTOR);
        }

        private void NewInjectRegisterToCTOR(GameEventUsage eventUsage)
        {
            var usageType = eventUsage.usageType;
            foreach (var m in usageType.Methods)
            {
                if (m.IsConstructor == false) continue;
                if (m.IsStatic) continue;
                if (m.Body == null) continue;

                InjectConstructor(m, eventUsage);
            }

        }

        private void InjectConstructor(MethodDefinition constructor, GameEventUsage eventUsage)
        {
            var ilProcesser = constructor.Body.GetILProcessor();
            var firstLine = constructor.Body.Instructions[0];

            foreach (var kv in eventUsage.event_Instance_Usage_Cache)
            {
                foreach (var usage in kv.Value)
                {
                    var eventType = usage.Parameters[0].ParameterType;
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldftn, usage));

                    var action_CTOR = GetEventActionConstructor(eventType);
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Newobj, action_CTOR));

                    var registerMethod = GetRegisterEventMethod(eventType);
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, registerMethod));
                }
            }

            foreach (var kv in eventUsage.event_Mono_Enable_Usage_Cache)
            {
                foreach (var usage in kv.Value)
                {
                    var eventType = usage.Parameters[0].ParameterType;
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldftn, usage));

                    var action_CTOR = GetEventActionConstructor(eventType);
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Newobj, action_CTOR));

                    var registerMethod = GetRegisterEventMethod(eventType);
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, registerMethod));
                }
            }

            foreach (var kv in eventUsage.task_Instance_Usage_Cache)
            {
                foreach (var usage in kv.Value)
                {
                    var eventType = usage.Parameters[0].ParameterType;
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldftn, usage));

                    var func_CTOR = GetTaskFuncConstructor(eventType);
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Newobj, func_CTOR));

                    var registerMethod = GetRegisterTaskMethod(eventType);
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, registerMethod));
                }
            }

            foreach (var kv in eventUsage.task_Mono_Enable_Usage_Cache)
            {
                foreach (var usage in kv.Value)
                {
                    var eventType = usage.Parameters[0].ParameterType;
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldftn, usage));

                    var func_CTOR = GetTaskFuncConstructor(eventType);
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Newobj, func_CTOR));

                    var registerMethod = GetRegisterTaskMethod(eventType);
                    ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, registerMethod));
                }
            }
        }

        private MethodReference GetRegisterEventMethod(TypeReference eventType)
        {
            var registerMethod = typeof(GameEvent.GameEventDriver).GetMethod("RegisterEvent");
            var methodReference = assemblyDefinition.MainModule.ImportReference(registerMethod);

            var genMethod = new GenericInstanceMethod(methodReference);
            genMethod.GenericArguments.Add(eventType);
            return genMethod;
        }

        private MethodReference GetRegisterTaskMethod(TypeReference eventType)
        {
            var registerMethod = typeof(GameEvent.GameEventDriver).GetMethod("RegisterTask");
            var methodReference = assemblyDefinition.MainModule.ImportReference(registerMethod);

            var genMethod = new GenericInstanceMethod(methodReference);
            genMethod.GenericArguments.Add(eventType);
            return genMethod;
        }

        private MethodReference GetUnregisterEventMethod(TypeReference eventType)
        {
            var unregisterMethod = typeof(GameEvent.GameEventDriver).GetMethod("UnregisterEvent");
            var methodReference = assemblyDefinition.MainModule.ImportReference(unregisterMethod);

            var genMethod = new GenericInstanceMethod(methodReference);
            genMethod.GenericArguments.Add(eventType);
            return genMethod;
        }

        private MethodReference GetUnregisterTaskMethod(TypeReference eventType)
        {
            var unregisterMethod = typeof(GameEvent.GameEventDriver).GetMethod("UnregisterTask");
            var methodReference = assemblyDefinition.MainModule.ImportReference(unregisterMethod);

            var genMethod = new GenericInstanceMethod(methodReference);
            genMethod.GenericArguments.Add(eventType);
            return genMethod;
        }

        private MethodReference GetEventActionConstructor(TypeReference eventType)
        {
            var actionType = assemblyDefinition.MainModule.ImportReference(typeof(Action<>));
            var fieldType = new GenericInstanceType(actionType);
            fieldType.GenericArguments.Add(eventType);
            // import  Action<GameEvent>.ctor;
            var original_CTOR = fieldType.Resolve().Methods.First(m => { return m.Name == ".ctor"; });
            var generic_CTOR = new MethodReference(original_CTOR.Name, original_CTOR.ReturnType, fieldType)
            {
                HasThis = original_CTOR.HasThis,
                ExplicitThis = original_CTOR.ExplicitThis,
                CallingConvention = original_CTOR.CallingConvention,
            };
            foreach (var p in original_CTOR.Parameters)
            {
                generic_CTOR.Parameters.Add(new ParameterDefinition(p.ParameterType));
            }
            foreach (var gp in original_CTOR.GenericParameters)
            {
                generic_CTOR.GenericParameters.Add(new GenericParameter(gp.Name, generic_CTOR));
            }
            var action_CTOR = eventType.Module.ImportReference(generic_CTOR);

            return action_CTOR;
        }

        private MethodReference GetTaskFuncConstructor(TypeReference eventType)
        {
            var actionType = assemblyDefinition.MainModule.ImportReference(typeof(Func<,>));
            var fieldType = new GenericInstanceType(actionType);
            fieldType.GenericArguments.Add(eventType);
            fieldType.GenericArguments.Add(assemblyDefinition.MainModule.ImportReference(typeof(Task)));
            // import  Func<GameEvent,Task>.ctor;
            var original_CTOR = fieldType.Resolve().Methods.First(m => { return m.Name == ".ctor"; });
            var generic_CTOR = new MethodReference(original_CTOR.Name, original_CTOR.ReturnType, fieldType)
            {
                HasThis = original_CTOR.HasThis,
                ExplicitThis = original_CTOR.ExplicitThis,
                CallingConvention = original_CTOR.CallingConvention,
            };
            foreach (var p in original_CTOR.Parameters)
            {
                generic_CTOR.Parameters.Add(new ParameterDefinition(p.ParameterType));
            }
            foreach (var gp in original_CTOR.GenericParameters)
            {
                generic_CTOR.GenericParameters.Add(new GenericParameter(gp.Name, generic_CTOR));
            }
            var func_CTOR = eventType.Module.ImportReference(generic_CTOR);

            return func_CTOR;
        }
    }
}