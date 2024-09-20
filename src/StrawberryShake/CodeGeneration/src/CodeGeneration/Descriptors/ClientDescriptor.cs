using StrawberryShake.CodeGeneration.Descriptors.Operations;
using static StrawberryShake.CodeGeneration.Properties.CodeGenerationResources;

namespace StrawberryShake.CodeGeneration.Descriptors;

/// <summary>
/// Describes a GraphQL client class, that bundles all operations defined in a single class.
/// </summary>
public sealed class ClientDescriptor : ICodeDescriptor
{
    public ClientDescriptor(
        string name,
        string @namespace,
        List<OperationDescriptor> operations)
    {
        RuntimeType = new(name, @namespace);
        Operations = operations;
        Documentation = string.Format(ClientDescriptor_Description, Name);
        InterfaceType = new("I" + name, @namespace);
    }

    /// <summary>
    /// Gets the client name
    /// </summary>
    /// <value></value>
    public string Name => RuntimeType.Name;

    /// <summary>
    /// The name of the client
    /// </summary>
    public RuntimeTypeInfo RuntimeType { get; }

    /// <summary>
    /// The operations that are contained in this client class
    /// </summary>
    public List<OperationDescriptor> Operations { get; }

    /// <summary>
    /// The documentation for this client
    /// </summary>
    public string Documentation { get; }

    /// <summary>
    /// The interface of this client
    /// </summary>
    public RuntimeTypeInfo InterfaceType { get; }
}
