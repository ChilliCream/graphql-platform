using HotChocolate.Resolvers;

namespace HotChocolate.Types;

/// <summary>
/// Gets the concrete object type of a resolver result.
/// </summary>
public delegate ObjectType ResolveAbstractType(
    IResolverContext context,
    object resolverResult);
