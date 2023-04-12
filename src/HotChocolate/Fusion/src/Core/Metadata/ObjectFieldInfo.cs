namespace HotChocolate.Fusion.Metadata;

internal sealed class ObjectFieldInfo
{
    public ObjectFieldInfo(
        string name,
        ObjectFieldFlags flags,
        MemberBindingCollection bindings,
        FieldVariableDefinitionCollection variables,
        ResolverDefinitionCollection resolvers)
    {
        Name = name;
        Flags = flags;
        Bindings = bindings;
        Variables = variables;
        Resolvers = resolvers;
    }

    /// <summary>
    /// Gets the name of the object type this metadata is associated with.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the flags that specify execution behavior for this field.
    /// </summary>
    public ObjectFieldFlags Flags { get; }

    /// <summary>
    /// Gets the subgraph member bindings.
    /// </summary>
    public MemberBindingCollection Bindings { get; }

    public FieldVariableDefinitionCollection Variables { get; }

    public ResolverDefinitionCollection Resolvers { get; }
}
