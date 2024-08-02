using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Relay;

public delegate Task<TNode?> NodeResolverDelegate<TNode>(
    IResolverContext context,
    object id);

public delegate Task<TNode?> NodeResolverDelegate<TNode, in TId>(
    IResolverContext context,
    TId id);
