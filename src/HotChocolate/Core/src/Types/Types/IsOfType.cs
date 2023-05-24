using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// A delegate to determine if a resolver result is of a certain object type.
/// </summary>
public delegate bool IsOfType(
    IResolverContext context,
    LogicallyTypedValue resolverResult);

/// <summary>
/// A delegate to determine if a resolver result is of a certain object type.
/// </summary>
public delegate bool IsOfTypeFallback(
    ObjectType objectType,
    IResolverContext context,
    LogicallyTypedValue resolverResult);
