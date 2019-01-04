using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public delegate bool IsOfType(
        IResolverContext context,
        object resolverResult);
}
