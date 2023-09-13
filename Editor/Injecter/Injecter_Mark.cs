using System.Linq;

namespace GameEvent
{
    public partial class Injecter
    {
        private bool HasInjected()
        {
            var InjectedNameSpace = GameEventDriver.InjectedNameSpace;
            var InjectedClazz = GameEventDriver.InjectedClazz;

            var injectedFullName = $"{InjectedNameSpace}.{InjectedClazz}";

            var injected = assemblyDefinition.MainModule.Types.Any((t)
                => t.FullName == injectedFullName);


            return injected;
        }
    }
}