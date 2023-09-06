namespace GameEvent
{
    public partial class Injecter
    {
        private MethodUsageCache usageCache;

        private void BuildUsageCache()
        {
            this.usageCache = new MethodUsageCache();
            this.usageCache.assemblyDefinition = this.assemblyDefinition;
            this.usageCache.BuildCache();
        }
    }
}