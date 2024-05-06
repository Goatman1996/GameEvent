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

    public class GameEvent<T> where T : IGameEvent
    {
        public static event Action<T> Event;

        internal static void Invoke(T arg)
        {
            Event?.Invoke(arg);
        }
    }

    public class GameTask<T> where T : IGameTask
    {
        public static event Func<T, Task> Event;

        internal static async Task InvokeAsync(T arg)
        {
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