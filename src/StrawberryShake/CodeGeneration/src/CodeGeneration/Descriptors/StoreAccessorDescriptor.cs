namespace StrawberryShake.CodeGeneration.Descriptors;

/// <summary>
/// Describes the dependency injection requirements of a  GraphQL client
/// </summary>
public sealed class StoreAccessorDescriptor : ICodeDescriptor
{
    public StoreAccessorDescriptor(
        string name,
        string @namespace)
    {
        RuntimeType = new(name, @namespace);
    }

    /// <summary>
    /// The name of the client
    /// </summary>
    public string Name => RuntimeType.Name;

    public RuntimeTypeInfo RuntimeType { get; }
}
