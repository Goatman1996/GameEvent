using System.Collections.Generic;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace GameEvent
{
    internal class ILMethod
    {
        public string evtTypeName => paramDef.ParameterType.FullName;

        public ParameterDefinition paramDef;

        public List<MethodDefinition> methodDef_List;
        public List<MethodDefinition> methodDef_NeedEnable_List;
    }

    internal class ILMethod_Static
    {
        public string evtTypeName => paramDef.ParameterType.FullName;

        public ParameterDefinition paramDef;

        public List<MethodDefinition> methodDef_List;
        public List<MethodDefinition> methodDef_Public_List;

        public void GenerateILCode(AssemblyDefinition assemblyDef)
        {
            methodDef_Public_List.Clear();
            foreach (var method in this.methodDef_List)
            {
                var __Invoke__Name = $"__Static__Invoke__{method.Name}__";

                var __Invoke__Attris = MethodAttributes.Public;
                __Invoke__Attris |= MethodAttributes.HideBySig;
                __Invoke__Attris |= MethodAttributes.Static;

                var __Invoke__Ret = assemblyDef.MainModule.ImportReference(typeof(void));

                var __Invoke__Param_Evt = method.Parameters[0].Resolve();
                __Invoke__Param_Evt.Name = "evt";

                var __Invoke__ = new MethodDefinition(__Invoke__Name, __Invoke__Attris, __Invoke__Ret);
                __Invoke__.Parameters.Add(__Invoke__Param_Evt);

                var __Invoke__IL = __Invoke__.Body.GetILProcessor();

                __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_0));
                __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Call, method));
                __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ret));

                method.DeclaringType.Methods.Add(__Invoke__);

                methodDef_Public_List.Add(__Invoke__);
            }
        }
    }

    internal class ILMethod_Static_Task
    {
        public string evtTypeName => paramDef.ParameterType.FullName;

        public ParameterDefinition paramDef;

        public List<MethodDefinition> methodDef_List;
        public List<MethodDefinition> methodDef_Public_List;

        public void GenerateILCode(AssemblyDefinition assemblyDef)
        {
            methodDef_Public_List.Clear();
            foreach (var method in this.methodDef_List)
            {
                var __Invoke__Name = $"__Static__Invoke__{method.Name}__";

                var __Invoke__Attris = MethodAttributes.Public;
                __Invoke__Attris |= MethodAttributes.HideBySig;
                __Invoke__Attris |= MethodAttributes.Static;

                var __Invoke__Ret = assemblyDef.MainModule.ImportReference(typeof(Task));

                var __Invoke__Param_Evt = method.Parameters[0].Resolve();
                __Invoke__Param_Evt.Name = "evt";

                var __Invoke__ = new MethodDefinition(__Invoke__Name, __Invoke__Attris, __Invoke__Ret);
                __Invoke__.Parameters.Add(__Invoke__Param_Evt);

                var __Invoke__IL = __Invoke__.Body.GetILProcessor();

                __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ldarg_0));
                __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Call, method));
                __Invoke__IL.Append(__Invoke__IL.Create(OpCodes.Ret));

                method.DeclaringType.Methods.Add(__Invoke__);

                methodDef_Public_List.Add(__Invoke__);
            }
        }
    }
}