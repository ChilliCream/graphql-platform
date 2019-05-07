namespace HotChocolate.Execution
{
    internal static class ObjectPools
    {
        public static ObjectPool<ResolverContext> ResolverContexts { get; } =
            new ObjectPool<ResolverContext>(500);
    }
}
