namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// Represents metadata about an object type for the purpose of query planning.
/// </summary>
internal sealed class ObjectTypeMetadata : INamedTypeMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectTypeMetadata"/> class.
    /// </summary>
    /// <param name="name">The name of the object type.</param>
    /// <param name="bindings">The collection of member bindings for the object type.</param>
    /// <param name="variables">The collection of variable definitions for the object type.</param>
    /// <param name="resolvers">The collection of resolver definitions for the object type.</param>
    /// <param name="fields">The collection of fields for the object type.</param>
    public ObjectTypeMetadata(
        string name,
        MemberBindingCollection bindings,
        VariableDefinitionCollection variables,
        ResolverDefinitionCollection resolvers,
        ObjectFieldInfoCollection fields)
    {
        Name = name;
        Bindings = bindings;
        Variables = variables;
        Resolvers = resolvers;
        Fields = fields;
    }

    /// <summary>
    /// Gets the name of the object type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the collection of member bindings for the object type.
    /// </summary>
    public MemberBindingCollection Bindings { get; }

    /// <summary>
    /// Gets the collection of variable definitions for the object type.
    /// </summary>
    public VariableDefinitionCollection Variables { get; }

    /// <summary>
    /// Gets the collection of resolver definitions for the object type.
    /// </summary>
    public ResolverDefinitionCollection Resolvers { get; }

    /// <summary>
    /// Gets the collection of fields for the object type.
    /// </summary>
    public ObjectFieldInfoCollection Fields { get; }

    /// <summary>
    /// Returns the name of the object type as a string.
    /// </summary>
    /// <returns>The name of the object type.</returns>
    public override string ToString() => Name;
}
