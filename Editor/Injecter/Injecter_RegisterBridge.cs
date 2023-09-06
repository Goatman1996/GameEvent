using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace GameEvent
{
    public partial class Injecter
    {
        private TypeDefinition bridgeType;
        private MethodDefinition registerMethod;
        private MethodDefinition unregisterMethod;

        private void InjectBridge()
        {
            var InjectedNameSpace = GameEventDriver.InjectedNameSpace;
            var InjectedClazz = GameEventDriver.InjectedClazz;
            var typeAttri = TypeAttributes.Class | TypeAttributes.Public;
            var baseType = assemblyDefinition.MainModule.TypeSystem.Object;

            var injectedTypeDef = new TypeDefinition(InjectedNameSpace, InjectedClazz, typeAttri, baseType);

            var PreserveCtor = typeof(UnityEngine.Scripting.PreserveAttribute).GetConstructors()[0];
            var Preserve = new CustomAttribute(assemblyDefinition.MainModule.ImportReference(PreserveCtor));
            injectedTypeDef.CustomAttributes.Add(Preserve);

            assemblyDefinition.MainModule.Types.Add(injectedTypeDef);

            this.bridgeType = injectedTypeDef;

            this.Generate_iRegisterBridge();
            this.Generate_CTOR();
            this.Generate_Register();
            this.Generate_Unregister();
        }

        private void Generate_iRegisterBridge()
        {
            // 添加IRegisterBridge接口
            var invokerTypeRef = assemblyDefinition.MainModule.ImportReference(typeof(GameEvent.IRegisterBridge));
            var invoker = new InterfaceImplementation(invokerTypeRef);
            this.bridgeType.Interfaces.Add(invoker);
        }

        private void Generate_CTOR()
        {
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
                ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_1));
                ilProcesser.Append(ilProcesser.Create(OpCodes.Call, eventModifier.eventRegister));
            }
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ret));

            this.bridgeType.Methods.Add(registerMethod);

            this.registerMethod = registerMethod;
        }

        private void Generate_Unregister()
        {
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
                ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_1));
                ilProcesser.Append(ilProcesser.Create(OpCodes.Call, eventModifier.eventUnregister));
            }
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ret));

            this.bridgeType.Methods.Add(unregisterMethod);

            this.unregisterMethod = unregisterMethod;
        }
    }
}