using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace GameEvent
{
    public partial class Injecter
    {
        private Dictionary<TypeDefinition, EventModifier> eventModifierList = new Dictionary<TypeDefinition, EventModifier>();

        private List<EventUsageModifier> usageModifierList = new List<EventUsageModifier>();

        private void ModifyGameEvent()
        {
            this.CollectEventModifier();
            foreach (var modifier in this.eventModifierList.Values)
            {
                modifier.Modify();
            }

            this.CollectEventUsageModifier();
            foreach (var modifier in this.usageModifierList)
            {
                var needInjectCTOR = modifier.Modify();
                if (needInjectCTOR)
                {
                    this.InjectRegisterToCTOR(modifier.declaringType);
                }
            }
        }

        private EventModifier GetEventModify(TypeDefinition eventType)
        {
            if (this.eventModifierList.ContainsKey(eventType))
            {
                return this.eventModifierList[eventType];
            }
            return null;
        }

        private void CollectEventModifier()
        {
            this.eventModifierList.Clear();

            foreach (var gameEventType in this.usageCache.GetGameEventList())
            {
                var injecter = new EventModifier();
                injecter.eventType = gameEventType;
                injecter.assemblyDefinition = this.assemblyDefinition;
                injecter.usageCache = this.usageCache;
                injecter.logger = this.logger;

                this.eventModifierList.Add(gameEventType, injecter);
            }
        }

        private void CollectEventUsageModifier()
        {
            this.usageModifierList.Clear();

            foreach (var usage in this.usageCache.GetGameEventUsage())
            {
                var modifier = new EventUsageModifier();
                modifier.assemblyDefinition = this.assemblyDefinition;
                modifier.eventUsageCollection = usage;
                modifier.eventModifyProvider = this.GetEventModify;
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