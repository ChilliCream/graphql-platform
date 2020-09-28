using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    public interface IPagingContext
    {
        IResolverContext ResolverContext { get; }

        object Source { get; }

        bool IncludeTotalCount { get; }
    }
}
