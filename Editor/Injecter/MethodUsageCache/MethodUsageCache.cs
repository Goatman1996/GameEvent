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

        public AssemblyDefinition assemblyDefinition;

        private HashSet<TypeDefinition> iGameEventList = new HashSet<TypeDefinition>();
        private HashSet<TypeDefinition> iGameTaskList = new HashSet<TypeDefinition>();

        private Dictionary<TypeDefinition, GameEventUsage> userType_EventUsage_Collection;

        public void BuildCache()
        {
            userType_EventUsage_Collection = new Dictionary<TypeDefinition, GameEventUsage>();

            foreach (var type in assemblyDefinition.MainModule.Types)
            {
                this.CachingIGameEvent(type);
            }
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

            foreach (var iface in type.Interfaces)
            {
                if (iface.InterfaceType.FullName == iGameEventFullName)
                {
                    this.iGameEventList.Add(type);
                }
                if (iface.InterfaceType.FullName == iGameTaskFullName)
                {
                    this.iGameTaskList.Add(type);
                }
            }
        }

        private void TryCachingUsage(TypeDefinition type)
        {
            foreach (var nestedType in type.NestedTypes)
            {
                this.TryCachingUsage(nestedType);
            }

            var isClass = type.IsClass;
            var isValueType = type.IsValueType;

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

            return this.iGameEventList.Contains(paramDef);
        }

        private bool MethodParamOnlyGameTask(MethodDefinition method)
        {
            var methodParamCount = method.Parameters.Count;
            if (methodParamCount != 1) return false;

            var retType = method.ReturnType;
            if (retType.FullName.StartsWith(taskFullPrefixName) == false) return false;

            var onlyParam = method.Parameters[0];
            var paramDef = onlyParam.ParameterType.Resolve();

            return this.iGameTaskList.Contains(paramDef);
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

        public IEnumerable<GameEventUsage> GetGameEventUsage()
        {
            return this.userType_EventUsage_Collection.Values;
        }
    }
}