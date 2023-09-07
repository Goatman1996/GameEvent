using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace GameEvent
{
    // 这是一个使用了 GameEvent 的类
    public class EventUsageModifier
    {
        public AssemblyDefinition assemblyDefinition;
        public GameEventUsage eventUsageCollection;
        public Func<TypeDefinition, EventModifier> eventModifyProvider;

        public TypeDefinition declaringType { get => this.eventUsageCollection.usageType; }
        private bool isMono { get => this.eventUsageCollection.isMono; }
        private MethodReference InstanceMethod;
        private MethodReference MonoEnableMethod;

        public bool Modify()
        {
            this.InjectStaticToEvent();
            var needInjectCTOR = this.InjectInstanceInvokerToUsageType();
            return needInjectCTOR;
        }

        private void InjectStaticToEvent()
        {
            var eventTypeList = this.eventUsageCollection.GetAllStaticEvent();
            foreach (var eventType in eventTypeList)
            {
                var eventModify = this.eventModifyProvider.Invoke(eventType);
                var methodList = this.eventUsageCollection.TryGetEventTypeUsage(eventType, true, false);
                if (methodList == null)
                {
                    throw new Exception($"Static Method({eventType.Name}) Not Found In {this.declaringType.Name}");
                }
                foreach (var method in methodList)
                {
                    var methodWrapper = Generate_StaticMethod_Wrapper(method);
                    eventModify.AppendStaticMethod(methodWrapper);
                }
            }
        }

        private MethodReference Generate_StaticMethod_Wrapper(MethodDefinition method)
        {
            var methodName = $"{method.Name}__Wrapper";
            var methodAttri = MethodAttributes.Public;
            methodAttri |= MethodAttributes.HideBySig;
            methodAttri |= MethodAttributes.Static;
            var methodRet = assemblyDefinition.MainModule.ImportReference(typeof(void));

            var methodWrapper = new MethodDefinition(methodName, methodAttri, methodRet);

            var param = new ParameterDefinition(method.Parameters[0].ParameterType);
            param.Name = method.Parameters[0].Name;
            methodWrapper.Parameters.Add(param);

            var ilProcesser = methodWrapper.Body.GetILProcessor();
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_0));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Call, method));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ret));

            declaringType.Methods.Add(methodWrapper);

            return methodWrapper;
        }

        private bool InjectInstanceInvokerToUsageType()
        {
            var eventTypeList = this.eventUsageCollection.GetAllInstanceEvent();
            bool hasAnyInstanceMethod = false;
            foreach (var eventType in eventTypeList)
            {
                hasAnyInstanceMethod = true;
                this.InjectInstanceInvoker(eventType);
            }

            return hasAnyInstanceMethod;
        }

        private void InjectInstanceInvoker(TypeDefinition eventType)
        {
            var eventModifier = this.eventModifyProvider.Invoke(eventType);
            var iInvoke = this.Inject_Event_iInvoker(eventModifier);

            var ilProcesser = iInvoke.Body.GetILProcessor();

            var monoBlockFirstLine = ilProcesser.Create(OpCodes.Ldarg_0);
            var invokeBlockRet = ilProcesser.Create(OpCodes.Ret);

            if (this.isMono)
            {
                // if (this) { } return;
                ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_0));
                var monoIsExist = typeof(UnityEngine.Object).GetMethod("op_Implicit");
                var monoIsExist_Ref = assemblyDefinition.MainModule.ImportReference(monoIsExist);
                ilProcesser.Append(ilProcesser.Create(OpCodes.Call, monoIsExist_Ref));
                ilProcesser.Append(ilProcesser.Create(OpCodes.Brfalse_S, monoBlockFirstLine));
            }

            var instanceMethodList = this.eventUsageCollection.TryGetEventTypeUsage(eventType, false, false);
            if (instanceMethodList != null)
            {
                foreach (var method in instanceMethodList)
                {
                    ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_0));
                    ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_1));
                    ilProcesser.Append(ilProcesser.Create(OpCodes.Call, method));
                }
            }

            var monoEnableMethodList = this.eventUsageCollection.TryGetEventTypeUsage(eventType, false, true);
            if (monoEnableMethodList != null)
            {
                ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_0));
                var isEnableMethod = typeof(UnityEngine.Behaviour).GetProperty("isActiveAndEnabled").GetMethod;
                var isEnableMethodRef = assemblyDefinition.MainModule.ImportReference(isEnableMethod);
                ilProcesser.Append(ilProcesser.Create(OpCodes.Call, isEnableMethodRef));
                ilProcesser.Append(ilProcesser.Create(OpCodes.Brfalse_S, invokeBlockRet));

                foreach (var method in monoEnableMethodList)
                {
                    ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_0));
                    ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_1));
                    ilProcesser.Append(ilProcesser.Create(OpCodes.Call, method));
                }
            }
            ilProcesser.Append(invokeBlockRet);

            if (this.isMono)
            {
                // if (!this) GameEvent.GameEventDriver.Unregister(this);
                ilProcesser.Append(monoBlockFirstLine);
                var unregisterMethod = typeof(GameEvent.GameEventDriver).GetMethod("Unregister", new[] { typeof(object) });
                var unregisterMethod_Ret = assemblyDefinition.MainModule.ImportReference(unregisterMethod);
                ilProcesser.Append(ilProcesser.Create(OpCodes.Call, unregisterMethod_Ret));
                ilProcesser.Append(ilProcesser.Create(OpCodes.Ret));
            }

        }

        private MethodDefinition Inject_Event_iInvoker(EventModifier eventModifier)
        {
            var iInvoker = new InterfaceImplementation(eventModifier.eventiInvoker);
            this.declaringType.Interfaces.Add(iInvoker);

            var inInterfaceMethod = eventModifier.eventiInvoker_Invoke;

            var methodName = inInterfaceMethod.Name;
            var methodAttri = MethodAttributes.Public;
            methodAttri |= MethodAttributes.HideBySig;
            methodAttri |= MethodAttributes.NewSlot;
            methodAttri |= MethodAttributes.Virtual;
            methodAttri |= MethodAttributes.Final;
            var methodRet = inInterfaceMethod.ReturnType;

            var methodInvoke = new MethodDefinition(methodName, methodAttri, methodRet);
            foreach (var otiginalP in inInterfaceMethod.Parameters)
            {
                var p = new ParameterDefinition(otiginalP.ParameterType);
                p.Name = otiginalP.Name;
                methodInvoke.Parameters.Add(p);
            }

            this.declaringType.Methods.Add(methodInvoke);
            return methodInvoke;
        }
    }
}