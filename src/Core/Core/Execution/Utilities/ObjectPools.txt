namespace HotChocolate.Execution
{
    internal static class ObjectPools
    {
        private const int _maxPooledObjects = 500;

        public static ObjectPool<ResolverContext> ResolverContexts { get; } =
            new ObjectPool<ResolverContext>(_maxPooledObjects);
    }
}
