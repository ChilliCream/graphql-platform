using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public delegate bool IsOfType(
        IResolverContext context,
        object resolverResult);

    public delegate bool IsOfTypeFallback(
        ObjectType objectType,
        IResolverContext context,
        object resolverResult);
}
