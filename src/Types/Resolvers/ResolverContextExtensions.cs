namespace HotChocolate.Resolvers
{
    public static class ResolverContextExtensions
    {
        public static T Loader<T>(this IResolverContext resolverContext)
        {
            return resolverContext.Loader<T>(typeof(T).FullName);
        }
    }
}
