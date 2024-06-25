#nullable enable
namespace HotChocolate.Resolvers;

/// <summary>
/// This delegates represents a pure resolver that is side-effect free and sync.
/// </summary>
/// <param name="context">The resolver context.</param>
/// <returns>
/// Returns the resolver result.
/// </returns>
public delegate object? PureFieldDelegate(IResolverContext context);
