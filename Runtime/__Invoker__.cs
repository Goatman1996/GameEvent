namespace GameEvent
{
    public interface __Instance_Invoker__
    {
        public bool __Invoke__(IGameEvent evt, bool isActiveAndEnabled = true);
    }

    public interface __Static__Invoker__
    {
        public void __Invoke__(IGameEvent evt);
    }
}