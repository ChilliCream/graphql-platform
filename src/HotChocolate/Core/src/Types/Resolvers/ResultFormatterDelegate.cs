#nullable enable
namespace HotChocolate.Resolvers;

/// <summary>
/// This delegate allows to format a resolver result after it has completed.
/// </summary>
public delegate object? ResultFormatterDelegate(IPureResolverContext context, object? result);
