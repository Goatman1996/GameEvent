using System;

namespace GameEvent
{
    [AttributeUsage(AttributeTargets.Method)]
    public class GameEventAttribute : Attribute
    {
        /// <summary>
        /// 如果是Mono，则判断是否是Enable来发起调用
        /// </summary>
        public bool CallOnlyIfMonoEnable;

        public GameEventAttribute(bool CallOnlyIfMonoEnable = false)
        {
            this.CallOnlyIfMonoEnable = CallOnlyIfMonoEnable;
        }
    }
}