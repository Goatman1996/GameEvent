namespace GameEvent
{
    public interface IRegisterBridge
    {
        public void Register(object target);
        public void Unregister(object target);
    }
}