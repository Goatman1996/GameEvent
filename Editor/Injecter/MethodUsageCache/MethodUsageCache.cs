using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace GameEvent
{
    public class MethodUsageCache
    {
        private string iGameEventFullName = typeof(IGameEvent).FullName;
        private string iGameTaskFullName = typeof(IGameTask).FullName;
        private string taskFullPrefixName = typeof(System.Threading.Tasks.Task).FullName;

        private HashSet<TypeDefinition> iGameEventList = new HashSet<TypeDefinition>();
        private HashSet<TypeDefinition> iGameTaskList = new HashSet<TypeDefinition>();

        private Dictionary<TypeDefinition, GameEventUsage> userType_EventUsage_Collection = new Dictionary<TypeDefinition, GameEventUsage>();

        StringBuilder sb = new StringBuilder();
        public string Print()
        {
            sb.Clear();

            sb.AppendLine("iGameEventList");
            foreach (var e in iGameEventList)
            {
                sb.AppendLine(e.FullName);
            }

            sb.AppendLine("iGameTaskList");
            foreach (var e in iGameTaskList)
            {
                sb.AppendLine(e.FullName);
            }

            sb.AppendLine("userType_EventUsage_Collection");
            foreach (var e in userType_EventUsage_Collection)
            {
                sb.AppendLine($"{e.Key.FullName}");
            }

            return sb.ToString();
        }


        public void BuildEventCache(AssemblyDefinition assemblyDefinition)
        {
            foreach (var type in assemblyDefinition.MainModule.Types)
            {
                this.CachingIGameEvent(type);
            }
        }
        public void BuildUsageCache(AssemblyDefinition assemblyDefinition)
        {
            foreach (var type in assemblyDefinition.MainModule.Types)
            {
                this.TryCachingUsage(type);
            }
        }

        private void CachingIGameEvent(TypeDefinition type)
        {
            foreach (var nestedType in type.NestedTypes)
            {
                this.CachingIGameEvent(nestedType);
            }

            if (CheckingIsEvent(type))
            {
                this.iGameEventList.Add(type);
            }
            if (CheckingIsTask(type))
            {
                this.iGameTaskList.Add(type);
            }
        }

        private bool CheckingIsEvent(TypeDefinition type)
        {
            foreach (var iface in type.Interfaces)
            {
                if (iface.InterfaceType.FullName == iGameEventFullName)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckingIsTask(TypeDefinition type)
        {
            foreach (var iface in type.Interfaces)
            {
                if (iface.InterfaceType.FullName == iGameTaskFullName)
                {
                    return true;
                }
            }
            return false;
        }

        private void TryCachingUsage(TypeDefinition type)
        {
            foreach (var nestedType in type.NestedTypes)
            {
                this.TryCachingUsage(nestedType);
            }

            foreach (var method in type.Methods)
            {
                if (this.MethodHasGameEventAttribute(type, method, out bool CallOnlyIfMonoEnable) == false) continue;
                if (this.MethodParamOnlyGameEvent(method))
                {
                    this.CachingGameEventUsage(method, CallOnlyIfMonoEnable);
                }
                if (this.MethodParamOnlyGameTask(method))
                {
                    this.CachingGameTaskUsage(method, CallOnlyIfMonoEnable);
                }
            }
        }

        private bool MethodHasGameEventAttribute(TypeDefinition type, MethodDefinition method, out bool CallOnlyIfMonoEnable)
        {
            CallOnlyIfMonoEnable = false;
            var targetAttriName = typeof(GameEvent.GameEventAttribute).FullName;
            foreach (var attri in method.CustomAttributes)
            {
                if (attri.AttributeType.FullName == targetAttriName)
                {
                    if (this.TypeIsMono(type))
                    {
                        CallOnlyIfMonoEnable = (bool)attri.ConstructorArguments[0].Value;
                    }
                    return true;
                }
            }
            return false;
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

        private bool MethodParamOnlyGameEvent(MethodDefinition method)
        {
            var methodParamCount = method.Parameters.Count;
            if (methodParamCount != 1) return false;

            var onlyParam = method.Parameters[0];
            var paramDef = onlyParam.ParameterType.Resolve();

            return this.CheckingIsEvent(paramDef);
        }

        private bool MethodParamOnlyGameTask(MethodDefinition method)
        {
            var methodParamCount = method.Parameters.Count;
            if (methodParamCount != 1) return false;

            var retType = method.ReturnType;
            if (retType.FullName.StartsWith(taskFullPrefixName) == false) return false;

            var onlyParam = method.Parameters[0];
            var paramDef = onlyParam.ParameterType.Resolve();

            return this.CheckingIsTask(paramDef);
        }

        private GameEventUsage GetOrCreate(TypeDefinition typeDef)
        {
            if (this.userType_EventUsage_Collection.ContainsKey(typeDef) == false)
            {
                this.userType_EventUsage_Collection.Add(typeDef, new GameEventUsage()
                {
                    usageType = typeDef,
                });
            }
            return this.userType_EventUsage_Collection[typeDef];
        }

        private void CachingGameEventUsage(MethodDefinition method, bool CallOnlyIfMonoEnable)
        {
            var usageCollection = this.GetOrCreate(method.DeclaringType);
            usageCollection.AppendEvent(method, CallOnlyIfMonoEnable);
        }

        private void CachingGameTaskUsage(MethodDefinition method, bool CallOnlyIfMonoEnable)
        {
            var usageCollection = this.GetOrCreate(method.DeclaringType);
            usageCollection.AppendTask(method, CallOnlyIfMonoEnable);
        }

        public IEnumerable<TypeDefinition> GetGameEventList()
        {
            return this.iGameEventList;
        }

        public IEnumerable<TypeDefinition> GetGameTaskList()
        {
            return this.iGameTaskList;
        }

        public IEnumerable<GameEventUsage> GetGameEventUsage()
        {
            return this.userType_EventUsage_Collection.Values;
        }
    }
}