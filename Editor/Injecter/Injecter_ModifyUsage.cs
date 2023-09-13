using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace GameEvent
{
    public partial class Injecter
    {
        private List<EventUsageModifier> usageModifierList = new List<EventUsageModifier>();

        private void ModifyUsage(Func<TypeDefinition, EventModifier> modifierProvider)
        {
            this.CollectEventUsageModifier(modifierProvider);

            foreach (var modifier in this.usageModifierList)
            {
                var needInjectCTOR = modifier.Modify();
                if (needInjectCTOR)
                {
                    this.InjectRegisterToCTOR(modifier.declaringType);
                }
            }
        }

        private void CollectEventUsageModifier(Func<TypeDefinition, EventModifier> modifierProvider)
        {
            this.usageModifierList.Clear();

            foreach (var usage in this.usageCache.GetGameEventUsage())
            {
                if (usage.usageType.Module != this.assemblyDefinition.MainModule)
                {
                    continue;
                }
                var modifier = new EventUsageModifier();
                modifier.assemblyDefinition = this.assemblyDefinition;
                modifier.eventUsageCollection = usage;
                modifier.eventModifyProvider = modifierProvider;
                modifier.AppendStaticMethodToRegisterBridge = this.AppendStaticMethodToRegisterBridge;
                this.usageModifierList.Add(modifier);
            }
        }

        private void InjectRegisterToCTOR(TypeDefinition type)
        {
            foreach (var m in type.Methods)
            {
                if (m.IsConstructor == false) continue;
                if (m.IsStatic) continue;
                if (m.Body == null) continue;

                var ilProcesser = m.Body.GetILProcessor();
                var firstLine = m.Body.Instructions[0];
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Ldarg_0));
                var registerMethod = typeof(GameEvent.GameEventDriver).GetMethod("Register", new[] { typeof(object) });
                var methodReference = assemblyDefinition.MainModule.ImportReference(registerMethod);
                ilProcesser.InsertBefore(firstLine, ilProcesser.Create(OpCodes.Call, methodReference));
            }
        }
    }
}