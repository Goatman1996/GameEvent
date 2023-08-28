using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameEvent
{
    public interface __Instance_Invoker__
    {
        public bool __Invoke__(IGameEvent evt, bool isActiveAndEnabled = true);
    }

    public interface __Instance_Invoker__Task__
    {
        public bool __Invoke__(List<Task> noAllocList, IGameTask task, bool isActiveAndEnabled = true);
    }

    public interface __Static__Invoker__
    {
        public void __Invoke__(IGameEvent evt);

        public void __Invoke__(List<Task> noAllocList, IGameTask task);
    }
}