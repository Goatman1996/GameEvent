using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace GameEvent
{
    public partial class Injecter
    {
        private Dictionary<TypeDefinition, EventModifier> eventModifierList = new Dictionary<TypeDefinition, EventModifier>();

        private void ModifyGameEvent()
        {
            this.CollectEventModifier();
            foreach (var modifier in this.eventModifierList.Values)
            {
                modifier.Modify();
            }
        }

        private EventModifier GetEventModify(TypeDefinition eventType)
        {
            foreach (var kv in this.eventModifierList)
            {
                var modifierType = kv.Key;
                if (eventType.FullName != modifierType.FullName) continue;
                if (eventType.Module.Name != modifierType.Module.Name) continue;
                return kv.Value;
            }

            return null;
        }

        private void CollectEventModifier()
        {
            this.eventModifierList.Clear();

            foreach (var gameEventType in this.usageCache.GetGameEventList())
            {
                if (gameEventType.Module != this.assemblyDefinition.MainModule)
                {
                    continue;
                }
                var injecter = new EventModifier();
                injecter.eventType = gameEventType;
                injecter.assemblyDefinition = this.assemblyDefinition;
                injecter.usageCache = this.usageCache;
                injecter.logger = this.logger;
                injecter.isGameTask = false;

                this.eventModifierList.Add(gameEventType, injecter);
            }

            foreach (var gameTaskType in this.usageCache.GetGameTaskList())
            {
                if (gameTaskType.Module != this.assemblyDefinition.MainModule)
                {
                    continue;
                }
                var injecter = new EventModifier();
                injecter.eventType = gameTaskType;
                injecter.assemblyDefinition = this.assemblyDefinition;
                injecter.usageCache = this.usageCache;
                injecter.logger = this.logger;
                injecter.isGameTask = true;

                this.eventModifierList.Add(gameTaskType, injecter);
            }
        }
    }
}