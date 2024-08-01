#nullable enable

namespace HotChocolate.Resolvers.Expressions;

/// <summary>
/// Describes a resolver that is based on a resolver type.
/// </summary>
internal class ResolverDescriptor
{
    /// <summary>
    /// Creates a new instance of <see cref="ResolverType"/>
    /// </summary>
    public ResolverDescriptor(
        Type sourceType,
        FieldMember field,
        Type? resolverType = null)
    {
        SourceType = sourceType
            ?? throw new ArgumentNullException(nameof(sourceType));
        Field = field
            ?? throw new ArgumentNullException(nameof(field));

        ResolverType = resolverType == typeof(object) ? null : resolverType;
    }

    /// <summary>
    /// Gets the resolver type.
    /// If a resolver type is the <see cref="Field"/> belongs to this type.
    /// </summary>
    public Type? ResolverType { get; }

    /// <summary>
    /// Gets the source type aka runtime type of a GraphQL type.
    /// </summary>
    public Type SourceType { get; }

    /// <summary>
    /// Gets the member that shall be compiled to a resolver.
    /// </summary>
    public FieldMember Field { get; }
}
