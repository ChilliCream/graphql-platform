namespace SkinnyLatte.Types;

/// <summary>
/// This marker interface identifies member of the type system like
/// types, directives, the schema or fields and arguments.
/// </summary>
public abstract class TypeSystemMember
{
    protected TypeSystemMember(string name, IReadOnlyDictionary<string, object?> contextData)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ContextData = contextData ?? throw new ArgumentNullException(nameof(contextData));
    }

    /// <summary>
    /// Gets the GraphQL type system member name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The context data dictionary can be used by middleware components and
    /// resolvers to retrieve data during execution.
    /// </summary>
    public IReadOnlyDictionary<string, object?> ContextData { get; }
}
