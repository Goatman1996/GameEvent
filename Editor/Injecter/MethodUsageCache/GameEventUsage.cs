using System.Collections.Generic;
using Mono.Cecil;
using UsageCollection = System.Collections.Generic.Dictionary<Mono.Cecil.TypeDefinition, System.Collections.Generic.List<Mono.Cecil.MethodDefinition>>;
using System.Linq;

namespace GameEvent
{
    public class GameEventUsage
    {
        public TypeDefinition usageType;

        private bool? _isMono;
        public bool isMono
        {
            get
            {
                if (this._isMono == null)
                {
                    this._isMono = this.TypeIsMono(this.usageType);
                }
                return this._isMono.Value;
            }
        }

        public MethodDefinition Customize_op_Implicit_
        {
            get
            {
                var _op_Implicit = this.usageType.Methods.FirstOrDefault(m => m.Name == "op_Implicit" && m.ReturnType.FullName == "System.Boolean");
                return _op_Implicit;
            }
        }

        private bool TypeIsMono(TypeDefinition type)
        {
            var typeIndex = type;
            while (typeIndex.BaseType != null)
            {
                var baseType = typeIndex.BaseType;
                if (baseType.FullName == "UnityEngine.MonoBehaviour")
                {
                    return true;
                }

                typeIndex = baseType.Resolve();
            }

            return false;
        }

        private UsageCollection event_Instance_Usage_Cache = new UsageCollection();
        private UsageCollection event_Mono_Enable_Usage_Cache = new UsageCollection();
        private UsageCollection event_Static_Usage_Cache = new UsageCollection();

        private UsageCollection GetEventCollection(bool isStatic, bool CallOnlyIfMonoEnable)
        {
            if (isStatic) return event_Static_Usage_Cache;
            else if (CallOnlyIfMonoEnable) return event_Mono_Enable_Usage_Cache;
            else return event_Instance_Usage_Cache;
        }

        public void AppendEvent(MethodDefinition method, bool CallOnlyIfMonoEnable)
        {
            TypeDefinition eventType = method.Parameters[0].ParameterType.Resolve();
            var collection = this.GetEventCollection(method.IsStatic, CallOnlyIfMonoEnable);
            if (collection.ContainsKey(eventType) == false)
            {
                collection.Add(eventType, new List<MethodDefinition>());
            }
            collection[eventType].Add(method);
        }



        private UsageCollection task_Instance_Usage_Cache = new UsageCollection();
        private UsageCollection task_Mono_Enable_Usage_Cache = new UsageCollection();
        private UsageCollection task_Static_Usage_Cache = new UsageCollection();

        private UsageCollection GetTaskCollection(bool isStatic, bool CallOnlyIfMonoEnable)
        {
            if (isStatic) return task_Static_Usage_Cache;
            else if (CallOnlyIfMonoEnable) return task_Mono_Enable_Usage_Cache;
            else return task_Instance_Usage_Cache;
        }

        public void AppendTask(MethodDefinition method, bool CallOnlyIfMonoEnable)
        {
            TypeDefinition taskType = method.Parameters[0].ParameterType.Resolve();
            var collection = this.GetTaskCollection(method.IsStatic, CallOnlyIfMonoEnable);
            if (collection.ContainsKey(taskType) == false)
            {
                collection.Add(taskType, new List<MethodDefinition>());
            }
            collection[taskType].Add(method);
        }
















        public IEnumerable<TypeDefinition> GetAllStaticEventAndTask()
        {
            var ret = new List<TypeDefinition>();
            ret.AddRange(this.event_Static_Usage_Cache.Keys);
            ret.AddRange(this.task_Static_Usage_Cache.Keys);
            return ret;
        }

        public IEnumerable<TypeDefinition> GetAllInstanceEventAndTask()
        {
            var ret = new HashSet<TypeDefinition>();
            foreach (var eventType in this.event_Instance_Usage_Cache.Keys)
            {
                ret.Add(eventType);
            }
            foreach (var eventType in this.event_Mono_Enable_Usage_Cache.Keys)
            {
                if (ret.Contains(eventType)) continue;
                ret.Add(eventType);
            }
            foreach (var eventType in this.task_Instance_Usage_Cache.Keys)
            {
                ret.Add(eventType);
            }
            foreach (var eventType in this.task_Mono_Enable_Usage_Cache.Keys)
            {
                if (ret.Contains(eventType)) continue;
                ret.Add(eventType);
            }
            return ret;
        }

        public IEnumerable<MethodDefinition> TryGetEventTypeUsage(TypeDefinition eventType, bool isStatic, bool CallOnlyIfMonoEnable)
        {
            var ret = new List<MethodDefinition>();

            var eventCollection = this.GetEventCollection(isStatic, CallOnlyIfMonoEnable);
            if (eventCollection.ContainsKey(eventType))
            {
                ret.AddRange(eventCollection[eventType]);
            }
            var taskCollection = this.GetTaskCollection(isStatic, CallOnlyIfMonoEnable);
            if (taskCollection.ContainsKey(eventType))
            {
                ret.AddRange(taskCollection[eventType]);
            }

            if (ret.Count == 0)
            {
                return null;
            }
            else
            {
                return ret;
            }
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        public string Print()
        {
            sb.Clear();

            var typeList = new HashSet<TypeDefinition>();
            typeList.UnionWith(this.GetAllStaticEventAndTask());
            typeList.UnionWith(this.GetAllInstanceEventAndTask());

            foreach (var type in typeList)
            {
                var methodList = new List<MethodDefinition>();
                {
                    var list = this.TryGetEventTypeUsage(type, true, false);
                    if (list != null)
                    {
                        methodList.AddRange(list);
                    }
                }
                {
                    var list = this.TryGetEventTypeUsage(type, false, false);
                    if (list != null)
                    {
                        methodList.AddRange(list);
                    }
                }
                {
                    var list = this.TryGetEventTypeUsage(type, false, true);
                    if (list != null)
                    {
                        methodList.AddRange(list);
                    }
                }
                foreach (var method in methodList)
                {
                    sb.AppendLine($" ===> {method.FullName}");
                }
            }
            return sb.ToString();
        }
    }
}