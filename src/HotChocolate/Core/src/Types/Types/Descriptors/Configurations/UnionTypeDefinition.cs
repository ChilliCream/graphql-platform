using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// Defines the properties of a GraphQL union type.
/// </summary>
public class UnionTypeConfiguration : TypeConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="UnionTypeConfiguration"/>.
    /// </summary>
    public UnionTypeConfiguration() { }

    /// <summary>
    /// Initializes a new instance of <see cref="UnionTypeConfiguration"/>.
    /// </summary>
    public UnionTypeConfiguration(
        string name,
        string? description = null,
        Type? runtimeType = null)
        : base(runtimeType ?? typeof(object))
    {
        Name = name.EnsureGraphQLName();
        Description = description;
    }

    /// <summary>
    /// A delegate to get the concrete object type of a resolver result.
    /// </summary>
    public ResolveAbstractType? ResolveAbstractType { get; set; }

    /// <summary>
    /// The types that make up the union type set.
    /// </summary>
    public IList<TypeReference> Types { get; } = [];
}
