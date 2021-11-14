using HotChocolate.Resolvers;

namespace HotChocolate.Types;

/// <summary>
/// A delegate to determine if a resolver result is of a certain object type.
/// </summary>
public delegate bool IsOfType(
    IResolverContext context,
    object resolverResult);

/// <summary>
/// A delegate to determine if a resolver result is of a certain object type.
/// </summary>
public delegate bool IsOfTypeFallback(
    ObjectType objectType,
    IResolverContext context,
    object resolverResult);
