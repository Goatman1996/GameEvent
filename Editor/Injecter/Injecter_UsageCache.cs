namespace GameEvent
{
    public partial class Injecter
    {
        public void BuildEventCache(MethodUsageCache cache)
        {
            // 构建 事件 相关的使用 并 缓存
            cache.BuildEventCache(this.assemblyDefinition);
        }

        public void BuildUsageCache(MethodUsageCache cache)
        {
            // 构建 事件 相关的使用 并 缓存
            cache.BuildUsageCache(this.assemblyDefinition);
        }

        public void SetCache(MethodUsageCache cache)
        {
            // 构建 事件 相关的使用 并 缓存
            this.usageCache = cache;
        }
        private MethodUsageCache usageCache;
    }
}