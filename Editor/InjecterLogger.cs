namespace GameEvent
{
    internal class InjecterLogger
    {
        private static InjecterLogger _instance;
        public static InjecterLogger Intance
        {
            get
            {
                if (_instance == null)
                    _instance = new InjecterLogger();
                return _instance;
            }
        }

        internal static void Log(object msg)
        {

        }
    }
}
