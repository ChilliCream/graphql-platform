using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay;

/// <summary>
/// Resolves nodes from their IDs, returning one result per context in the same order.
/// A null entry represents a node that was not found.
/// </summary>
public delegate Task<IReadOnlyList<TNode?>> BatchNodeResolverDelegate<TNode, in TId>(
    IReadOnlyList<IResolverContext> contexts,
    IReadOnlyList<TId> ids);
