namespace HotChocolate.Execution
{
    internal static class ObjectPools
    {
        public static ObjectPool<ResolverContext> ResolverTasks { get; } =
            new ObjectPool<ResolverContext>(500);
    }
}
