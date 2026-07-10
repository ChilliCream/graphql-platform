using System.Collections.Immutable;

namespace HotChocolate.Resolvers;

/// <summary>
/// This delegate defines the interface of a batch field pipeline that the
/// execution engine invokes to resolve a field result for multiple parent
/// objects in a single invocation.
/// </summary>
/// <param name="contexts">The middleware contexts for all parent objects in the batch.</param>
public delegate ValueTask BatchFieldDelegate(ImmutableArray<IMiddlewareContext> contexts);
