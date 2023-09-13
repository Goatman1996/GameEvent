using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;

namespace GameEvent
{
    public partial class Injecter
    {
        private TypeDefinition bridgeType;
        private MethodDefinition registerMethod;
        private MethodDefinition unregisterMethod;
        private MethodDefinition staticRegisterMethod;
        private bool injectedBridge = false;

        private void InjectBridge()
        {
            var InjectedNameSpace = GameEventDriver.InjectedNameSpace;
            var InjectedClazz = GameEventDriver.InjectedClazz;

            var injectedFullName = $"{InjectedNameSpace}.{InjectedClazz}";
            var has = assemblyDefinition.MainModule.Types.FirstOrDefault(t => t.FullName == injectedFullName);
            if (has == null)
            {
                var typeAttri = TypeAttributes.Class | TypeAttributes.Public;
                var baseType = assemblyDefinition.MainModule.TypeSystem.Object;

                var injectedTypeDef = new TypeDefinition(InjectedNameSpace, InjectedClazz, typeAttri, baseType);

                var PreserveCtor = typeof(UnityEngine.Scripting.PreserveAttribute).GetConstructors()[0];
                var Preserve = new CustomAttribute(assemblyDefinition.MainModule.ImportReference(PreserveCtor));
                injectedTypeDef.CustomAttributes.Add(Preserve);

                assemblyDefinition.MainModule.Types.Add(injectedTypeDef);

                this.bridgeType = injectedTypeDef;
            }
            else
            {
                this.injectedBridge = true;
                this.bridgeType = has;
            }

            this.Generate_iRegisterBridge();
            this.Generate_CTOR();
            this.Generate_Register();
            this.Generate_Unregister();
            this.Generate_StaticRegister();
        }

        private void Generate_iRegisterBridge()
        {
            if (this.injectedBridge) return;
            // 添加IRegisterBridge接口
            var invokerTypeRef = assemblyDefinition.MainModule.ImportReference(typeof(GameEvent.IRegisterBridge));
            var invoker = new InterfaceImplementation(invokerTypeRef);
            this.bridgeType.Interfaces.Add(invoker);
        }

        private void Generate_CTOR()
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

            this.bridgeType.Methods.Add(CTOR);
        }

        private void Generate_Register()
        {
            if (this.injectedBridge) return;
            // 添加 Register
            var registerName = "Register";

            var registerAttri = Mono.Cecil.MethodAttributes.Public;
            registerAttri |= Mono.Cecil.MethodAttributes.HideBySig;
            registerAttri |= Mono.Cecil.MethodAttributes.NewSlot;
            registerAttri |= Mono.Cecil.MethodAttributes.Virtual;
            registerAttri |= Mono.Cecil.MethodAttributes.Final;

            var registerRet = assemblyDefinition.MainModule.ImportReference(typeof(void));

            var paramTypeRef = assemblyDefinition.MainModule.ImportReference(typeof(object));
            var registerParam = new ParameterDefinition(paramTypeRef);
            registerParam.Name = "target";

            var registerMethod = new MethodDefinition(registerName, registerAttri, registerRet);
            registerMethod.Parameters.Add(registerParam);

            {
                var PreserveCtor = typeof(UnityEngine.Scripting.PreserveAttribute).GetConstructors()[0];
                var Preserve = new CustomAttribute(assemblyDefinition.MainModule.ImportReference(PreserveCtor));
                registerMethod.CustomAttributes.Add(Preserve);
            }

            var ilProcesser = registerMethod.Body.GetILProcessor();
            foreach (var eventModifier in this.eventModifierList.Values)
            {
                if (eventModifier.eventType.Module == assemblyDefinition.MainModule)
                {
                    ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_1));
                    ilProcesser.Append(ilProcesser.Create(OpCodes.Call, eventModifier.eventRegister));
                }
            }
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ret));

            this.bridgeType.Methods.Add(registerMethod);

            this.registerMethod = registerMethod;
        }


        private void Generate_Unregister()
        {
            if (this.injectedBridge) return;
            // 添加 Unregister
            var unregisterName = "Unregister";

            var unregisterAttri = Mono.Cecil.MethodAttributes.Public;
            unregisterAttri |= Mono.Cecil.MethodAttributes.HideBySig;
            unregisterAttri |= Mono.Cecil.MethodAttributes.NewSlot;
            unregisterAttri |= Mono.Cecil.MethodAttributes.Virtual;
            unregisterAttri |= Mono.Cecil.MethodAttributes.Final;

            var unregisterRet = assemblyDefinition.MainModule.ImportReference(typeof(void));

            var paramTypeRef = assemblyDefinition.MainModule.ImportReference(typeof(object));
            var unregisterParam = new ParameterDefinition(paramTypeRef);
            unregisterParam.Name = "target";

            var unregisterMethod = new MethodDefinition(unregisterName, unregisterAttri, unregisterRet);
            unregisterMethod.Parameters.Add(unregisterParam);

            {
                var PreserveCtor = typeof(UnityEngine.Scripting.PreserveAttribute).GetConstructors()[0];
                var Preserve = new CustomAttribute(assemblyDefinition.MainModule.ImportReference(PreserveCtor));
                unregisterMethod.CustomAttributes.Add(Preserve);
            }

            var ilProcesser = unregisterMethod.Body.GetILProcessor();
            foreach (var eventModifier in this.eventModifierList.Values)
            {
                if (eventModifier.eventType.Module == assemblyDefinition.MainModule)
                {
                    ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_1));
                    ilProcesser.Append(ilProcesser.Create(OpCodes.Call, eventModifier.eventUnregister));
                }
            }
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ret));

            this.bridgeType.Methods.Add(unregisterMethod);

            this.unregisterMethod = unregisterMethod;
        }

        private void Generate_StaticRegister()
        {
            // 添加 Static Register
            var staticRegisterName = "StaticRegister";
            var has = this.bridgeType.Methods.FirstOrDefault(m => m.Name == staticRegisterName);
            if (has != null)
            {
                this.staticRegisterMethod = has;
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

            this.bridgeType.Methods.Add(staticRegisterMethod);
            this.staticRegisterMethod = staticRegisterMethod;
        }

        private void AppendStaticMethodToRegisterBridge(MethodReference staticMethod, EventModifier targetEventModifier)
        {
            var ilProcesser = this.staticRegisterMethod.Body.GetILProcessor();
            var count = this.staticRegisterMethod.Body.Instructions.Count;
            var lastLine = this.staticRegisterMethod.Body.Instructions[count - 1];

            var refed_Action_CTOR = this.staticRegisterMethod.Module.ImportReference(targetEventModifier.action_CTOR);
            var refed_eventStaticRegister = this.staticRegisterMethod.Module.ImportReference(targetEventModifier.eventStaticRegister);

            // EventModifier.StaticRegister(staticMethod);
            ilProcesser.InsertBefore(lastLine, ilProcesser.Create(OpCodes.Ldnull));
            ilProcesser.InsertBefore(lastLine, ilProcesser.Create(OpCodes.Ldftn, staticMethod));
            ilProcesser.InsertBefore(lastLine, ilProcesser.Create(OpCodes.Newobj, refed_Action_CTOR));
            ilProcesser.InsertBefore(lastLine, ilProcesser.Create(OpCodes.Call, refed_eventStaticRegister));
        }
    }
}