using System;
using System.Threading.Tasks;

namespace GameEvent
{
    public interface IGameEvent
    {

    }

    public interface IGameTask
    {

    }

    internal class GameEvent<T> where T : IGameEvent
    {
        internal static event Action<T> Event;

        internal static void Invoke(T arg)
        {
            Event?.Invoke(arg);
        }
    }

    internal class GameTask<T> where T : IGameTask
    {
        internal static event Func<T, Task> Event;

        internal static async Task InvokeAsync(T arg)
        {
            if (Event == null)
            {
                return;
            }
            foreach (var invoker in Event.GetInvocationList())
            {
                var task = (Task)invoker?.DynamicInvoke(arg);
                if (task != null)
                {
                    await task;
                }
            }
        }
    }
}