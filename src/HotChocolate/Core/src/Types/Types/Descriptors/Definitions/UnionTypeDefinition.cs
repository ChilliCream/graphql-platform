using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Defines the properties of a GraphQL union type.
/// </summary>
public class UnionTypeDefinition : TypeDefinitionBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="UnionTypeDefinition"/>.
    /// </summary>
    public UnionTypeDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="UnionTypeDefinition"/>.
    /// </summary>
    public UnionTypeDefinition(
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
    public IList<TypeReference> Types { get; } = new List<TypeReference>();
}
