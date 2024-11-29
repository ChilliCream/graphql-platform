using HotChocolate.Resolvers;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// A reference resolver definition.
/// </summary>
public readonly struct ReferenceResolverDefinition
{
    /// <summary>
    /// Initializes a new instance of <see cref="ReferenceResolverDefinition"/>.
    /// </summary>
    /// <param name="resolver">
    /// The field resolver.
    /// </param>
    /// <param name="required">
    /// Specifies the representation paths that are required for this resolver.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="resolver"/> is <c>null</c>.
    /// </exception>
    public ReferenceResolverDefinition(
        FieldResolverDelegate resolver,
        IReadOnlyList<string[]>? required = default)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        Resolver = resolver;
        Required = required ?? Array.Empty<string[]>();
    }

    /// <summary>
    /// The reference resolver.
    /// </summary>
    public FieldResolverDelegate Resolver { get; }

    /// <summary>
    /// The representation paths that are required for this resolver.
    /// </summary>
    public IReadOnlyList<string[]> Required { get; }
}
