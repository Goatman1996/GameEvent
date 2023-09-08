using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace GameEvent
{
    public class EventModifier
    {
        public AssemblyDefinition assemblyDefinition;
        public MethodUsageCache usageCache;
        public Injecter.Logger logger;
        public bool isGameTask;

        // IGameEvent 的 实现类
        public TypeDefinition eventType;

        // eventType 创建 专属调用接口 eventType_Invoker
        public TypeReference eventiInvoker;
        // eventType_Invoker 接口 的方法
        public MethodReference eventiInvoker_Invoke;

        // eventType 的 Register 注册方法
        public MethodReference eventRegister;
        // eventType 的 Unregister 注销方法
        public MethodReference eventUnregister;
        // eventType 的 Static Invoker 方法
        public MethodDefinition eventStaticInvoker;

        // eventType 中 用于存事件的 Action<>
        public FieldDefinition actionField;
        // Action<> 泛型实例 的 Invoke 方法
        public MethodReference actionInvokeMethod;
        // Action<> 泛型实例 的 构造函数
        public MethodReference action_CTOR;

        public void Modify()
        {
            this.logger.AppendLine($"Modify [{this.eventType.Name}]");
            this.Generate_iInvoker();

            this.Generate_Action();

            this.Generate_Register();
            this.Generate_Unregister();
            this.Generate_StaticInvoker();

            this.OverridToString();
        }

        private void Generate_iInvoker()
        {
            var typeNameSpace = "GameEvent";
            var typeName = $"{this.eventType.Name}_Invoker";

            var typeAttri = TypeAttributes.Class;
            typeAttri |= TypeAttributes.Interface;
            typeAttri |= TypeAttributes.Public;
            typeAttri |= TypeAttributes.Abstract;

            var iInvoker = new TypeDefinition(typeNameSpace, typeName, typeAttri);

            {
                var invokerName = "Invoke";
                var invokerAttri = MethodAttributes.Public;
                invokerAttri |= MethodAttributes.HideBySig;
                invokerAttri |= MethodAttributes.NewSlot;
                invokerAttri |= MethodAttributes.Abstract;
                invokerAttri |= MethodAttributes.Virtual;
                var invokerRet = assemblyDefinition.MainModule.ImportReference(typeof(void));

                var invoke = new MethodDefinition(invokerName, invokerAttri, invokerRet);

                var paramDef = new ParameterDefinition(this.eventType);
                paramDef.Name = "evt";
                invoke.Parameters.Add(paramDef);

                iInvoker.Methods.Add(invoke);

                eventiInvoker_Invoke = invoke;
            }

            this.assemblyDefinition.MainModule.Types.Add(iInvoker);
            this.eventiInvoker = iInvoker;
        }

        private void Generate_Action()
        {
            var fieldName = "__action__";
            var fieldAttri = FieldAttributes.Private;
            fieldAttri |= FieldAttributes.Static;

            var actionType = assemblyDefinition.MainModule.ImportReference(typeof(Action<>));
            var fieldType = new GenericInstanceType(actionType);
            fieldType.GenericArguments.Add(eventType);

            this.actionField = new FieldDefinition(fieldName, fieldAttri, fieldType);

            {
                // import  Action<GameEvent>.Invoke;
                var originalInvoke = fieldType.Resolve().Methods.First(m => { return m.Name == "Invoke"; });
                var genericInvoke = new MethodReference(originalInvoke.Name, originalInvoke.ReturnType, fieldType)
                {
                    HasThis = originalInvoke.HasThis,
                    ExplicitThis = originalInvoke.ExplicitThis,
                    CallingConvention = originalInvoke.CallingConvention,
                };
                foreach (var p in originalInvoke.Parameters)
                {
                    genericInvoke.Parameters.Add(new ParameterDefinition(p.ParameterType));
                }
                foreach (var gp in originalInvoke.GenericParameters)
                {
                    genericInvoke.GenericParameters.Add(new GenericParameter(gp.Name, genericInvoke));
                }
                actionInvokeMethod = assemblyDefinition.MainModule.ImportReference(genericInvoke);
            }
            {
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
                this.action_CTOR = assemblyDefinition.MainModule.ImportReference(generic_CTOR);
            }

            this.eventType.Fields.Add(actionField);
        }

        private void Generate_Register()
        {
            var methodName = "__Register__";
            var methodAttri = MethodAttributes.Public;
            methodAttri |= MethodAttributes.HideBySig;
            methodAttri |= MethodAttributes.Static;
            var methodRet = assemblyDefinition.MainModule.ImportReference(typeof(void));
            var methodParam = new ParameterDefinition(assemblyDefinition.MainModule.ImportReference(typeof(object)));
            methodParam.Name = "target";

            var methodRegister = new MethodDefinition(methodName, methodAttri, methodRet);
            methodRegister.Parameters.Add(methodParam);

            var ilProcesser = methodRegister.Body.GetILProcessor();

            var finalRet = ilProcesser.Create(OpCodes.Ret);

            // if (arg_0 is Event_iInvoker)
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_0));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Isinst, this.eventiInvoker));
            // if False => GoTo Final Return
            ilProcesser.Append(ilProcesser.Create(OpCodes.Brfalse_S, finalRet));
            // __action__ += (arg_0 as Event_iInvoker).Invoke;
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ldsfld, this.actionField));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_0));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Isinst, this.eventiInvoker));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Dup));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ldvirtftn, this.eventiInvoker_Invoke));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Newobj, this.action_CTOR));
            var delegateCombineMethod = typeof(System.Delegate).GetMethod("Combine", new Type[] { typeof(Delegate), typeof(Delegate) });
            var delegateCombineMethodRef = assemblyDefinition.MainModule.ImportReference(delegateCombineMethod);
            ilProcesser.Append(ilProcesser.Create(OpCodes.Call, delegateCombineMethodRef));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Castclass, this.actionField.FieldType));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Stsfld, this.actionField));
            ilProcesser.Append(finalRet);

            this.eventType.Methods.Add(methodRegister);

            this.eventRegister = methodRegister;
        }

        private void Generate_Unregister()
        {
            var methodName = "__Unregister__";
            var methodAttri = MethodAttributes.Public;
            methodAttri |= MethodAttributes.HideBySig;
            methodAttri |= MethodAttributes.Static;
            var methodRet = assemblyDefinition.MainModule.ImportReference(typeof(void));
            var methodParam = new ParameterDefinition(assemblyDefinition.MainModule.ImportReference(typeof(object)));
            methodParam.Name = "target";

            var methodUnregister = new MethodDefinition(methodName, methodAttri, methodRet);
            methodUnregister.Parameters.Add(methodParam);

            var ilProcesser = methodUnregister.Body.GetILProcessor();

            var finalRet = ilProcesser.Create(OpCodes.Ret);

            // if (arg_0 is Event_iInvoker)
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_0));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Isinst, this.eventiInvoker));
            // if False => GoTo Final Return
            ilProcesser.Append(ilProcesser.Create(OpCodes.Brfalse_S, finalRet));
            // __action__ -= (arg_0 as Event_iInvoker).Invoke;
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ldsfld, this.actionField));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ldarg_0));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Isinst, this.eventiInvoker));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Dup));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ldvirtftn, this.eventiInvoker_Invoke));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Newobj, this.action_CTOR));
            var delegateRemoveMethod = typeof(System.Delegate).GetMethod("Remove", new Type[] { typeof(Delegate), typeof(Delegate) });
            var delegateRemoveMethodRef = assemblyDefinition.MainModule.ImportReference(delegateRemoveMethod);
            ilProcesser.Append(ilProcesser.Create(OpCodes.Call, delegateRemoveMethodRef));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Castclass, this.actionField.FieldType));
            ilProcesser.Append(ilProcesser.Create(OpCodes.Stsfld, this.actionField));
            ilProcesser.Append(finalRet);

            this.eventType.Methods.Add(methodUnregister);

            this.eventUnregister = methodUnregister;
        }

        private void Generate_StaticInvoker()
        {
            var methodName = "__StaticInvoker__";
            var methodAttri = MethodAttributes.Private;
            methodAttri |= MethodAttributes.HideBySig;
            methodAttri |= MethodAttributes.Static;
            var methodRet = assemblyDefinition.MainModule.ImportReference(typeof(void));
            var methodParam = new ParameterDefinition(this.eventType);
            methodParam.Name = "target";

            var methodStaticInvoker = new MethodDefinition(methodName, methodAttri, methodRet);
            methodStaticInvoker.Parameters.Add(methodParam);

            var ilProcesser = methodStaticInvoker.Body.GetILProcessor();
            ilProcesser.Append(ilProcesser.Create(OpCodes.Ret));

            this.eventType.Methods.Add(methodStaticInvoker);
            this.eventStaticInvoker = methodStaticInvoker;
        }

        private void OverridToString()
        {
            MethodDefinition toStringMethod = null;
            foreach (var method in this.eventType.Methods)
            {
                if (method.Name != "ToString") continue;
                if (method.HasParameters) continue;

                if (method.IsVirtual)
                {
                    toStringMethod = method;
                }
                else
                {
                    throw new Exception($"{this.eventType.FullName} Has Not Virtual ToString");
                }
            }

            if (toStringMethod == null)
            {
                toStringMethod = CreateOverrideToString();
            }

            var isEventingField = assemblyDefinition.MainModule.ImportReference(typeof(GameEventDriver).GetField("isEventing"));


            var ilProcesser = toStringMethod.Body.GetILProcessor();
            var firstLine = toStringMethod.Body.Instructions[0];

            var inBlockRetNull = ilProcesser.Create(OpCodes.Ldnull);
            var inBlockRet = ilProcesser.Create(OpCodes.Ret);

            var inBlockCallArg = ilProcesser.Create(OpCodes.Ldarg_0);
            var inBlockCall = ilProcesser.Create(OpCodes.Callvirt, this.actionInvokeMethod);

            // if (GameEvent.isEventing)
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldsfld, isEventingField));
            // if False => GoTo Original First Line 
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Brfalse_S, firstLine));
            // GameEvent.isEventing = false
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldc_I4_0));
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Stsfld, isEventingField));
            // __StaticInvoker__(this);
            if (eventType.IsValueType)
            {
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldobj, this.eventType));
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, this.eventStaticInvoker));
            }
            else
            {
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, this.eventStaticInvoker));
            }

            // Check __action__?
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldsfld, this.actionField));
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Dup));
            // if __action__ not NULL => GoTo Invoke Line
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Brtrue_S, inBlockCallArg));
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Pop));
            // if __action__ NULL => GoTo This If Block Return Line
            ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Br_S, inBlockRetNull));
            // __action__.Invoke(this);
            if (eventType.IsValueType)
            {
                ilProcesser.InsertBefore(firstLine, inBlockCallArg);
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldobj, this.eventType));
                ilProcesser.InsertBefore(firstLine, inBlockCall);
            }
            else
            {
                ilProcesser.InsertBefore(firstLine, inBlockCallArg);
                ilProcesser.InsertBefore(firstLine, inBlockCall);
            }

            // return null;
            ilProcesser.InsertBefore(firstLine, inBlockRetNull);
            ilProcesser.InsertBefore(firstLine, inBlockRet);
        }

        private MethodDefinition CreateOverrideToString()
        {
            var methodName = "ToString";
            var methodAttri = MethodAttributes.Public;
            methodAttri |= MethodAttributes.HideBySig;
            methodAttri |= MethodAttributes.Virtual;
            var methodRet = assemblyDefinition.MainModule.ImportReference(typeof(string));

            var methodToString = new MethodDefinition(methodName, methodAttri, methodRet);

            var ilProcessor = methodToString.Body.GetILProcessor();
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldnull));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));

            this.eventType.Methods.Add(methodToString);

            return methodToString;
        }

        public void AppendStaticMethod(MethodReference method)
        {
            var ilProcesser = this.eventStaticInvoker.Body.GetILProcessor();
            var count = this.eventStaticInvoker.Body.Instructions.Count;
            var lastLine = this.eventStaticInvoker.Body.Instructions[count - 1];

            if (this.isGameTask)
            {
                var taskListField = typeof(GameEventDriver).GetField("taskList", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var taskListField_Ref = assemblyDefinition.MainModule.ImportReference(taskListField);
                ilProcesser.InsertBefore(lastLine, ilProcesser.Create(OpCodes.Ldsfld, taskListField_Ref));

                ilProcesser.InsertBefore(lastLine, ilProcesser.Create(OpCodes.Ldarg_0));
                ilProcesser.InsertBefore(lastLine, ilProcesser.Create(OpCodes.Call, method));

                var addTaskMethod = typeof(List<Task>).GetMethod("Add", new[] { typeof(Task) });
                var addTaskMethod_Ref = assemblyDefinition.MainModule.ImportReference(addTaskMethod);
                ilProcesser.InsertBefore(lastLine, ilProcesser.Create(OpCodes.Callvirt, addTaskMethod_Ref));
            }
            else
            {
                ilProcesser.InsertBefore(lastLine, ilProcesser.Create(OpCodes.Ldarg_0));
                ilProcesser.InsertBefore(lastLine, ilProcesser.Create(OpCodes.Call, method));
            }
        }
    }
}