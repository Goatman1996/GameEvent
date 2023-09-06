using System.Text;

namespace GameEvent
{
    public partial class Injecter
    {
        public class Logger
        {
            private StringBuilder logger = new StringBuilder();

            public void AppendLine(string content)
            {
                logger.AppendLine(content);
            }

            public void Print()
            {
                UnityEngine.Debug.Log(logger);
            }

            public void PrintError()
            {
                UnityEngine.Debug.LogError(logger);
            }
        }

        private Logger logger = new Logger();
    }
}