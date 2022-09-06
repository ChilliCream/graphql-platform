using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Resolvers;

/// <summary>
/// This delegate defines the interface of a field pipeline that the
/// execution engine invokes to resolve a field result.
/// </summary>
/// <param name="context">The middleware context.</param>
public delegate ValueTask FieldDelegate(IMiddlewareContext context);

/// <summary>
/// This delegates represents a pure resolver that is side-effect free and sync.
/// </summary>
/// <param name="context">The resolver context.</param>
/// <returns>
/// Returns the resolver result.
/// </returns>
public delegate object? PureFieldDelegate(IPureResolverContext context);

/// <summary>
/// This delegate allows to convert/transform a resolver result after it has completed.
/// </summary>
public delegate object? ResultConverterDelegate(IPureResolverContext context, object? result);
