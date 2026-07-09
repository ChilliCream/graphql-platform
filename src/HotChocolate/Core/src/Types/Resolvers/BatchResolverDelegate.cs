namespace HotChocolate.Resolvers;

/// <summary>
/// This delegate describes the batch resolver interface that the execution
/// engine uses to resolve a field for multiple parent objects in a single
/// invocation.
/// </summary>
/// <param name="contexts">The resolver contexts for all parent objects in the batch.</param>
/// <returns>
/// Returns a list of resolver results, one per context, in the same order.
/// </returns>
public delegate ValueTask<IReadOnlyList<ResolverResult>> BatchResolverDelegate(
    IReadOnlyList<IResolverContext> contexts);
