using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.Tools.Configuration;

namespace StrawberryShake.CodeGeneration.Descriptors.Operations;

/// <summary>
/// Describes a GraphQL operation
/// </summary>
public abstract class OperationDescriptor : ICodeDescriptor
{
    protected OperationDescriptor(
        string name,
        RuntimeTypeInfo runtimeType,
        ITypeDescriptor resultTypeReference,
        IReadOnlyList<PropertyDescriptor> arguments,
        byte[] body,
        string bodyString,
        string hashAlgorithm,
        string hashValue,
        bool hasUpload,
        RequestStrategy strategy)
    {
        Name = name;
        RuntimeType = runtimeType;
        ResultTypeReference = resultTypeReference;
        Arguments = arguments;
        Body = body;
        BodyString = bodyString;
        HashAlgorithm = hashAlgorithm;
        HashValue = hashValue;
        HasUpload = hasUpload;
        Strategy = strategy;
        InterfaceType = new("I" + runtimeType.Name, runtimeType.Namespace);
    }

    /// <summary>
    /// Gets the operation name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the runtime type name.
    /// </summary>
    public RuntimeTypeInfo RuntimeType { get; }

    /// <summary>
    /// Gets the type the operation returns
    /// </summary>
    public ITypeDescriptor ResultTypeReference { get; }

    /// <summary>
    /// Gets the GraphQL Document.
    /// </summary>
    public byte[] Body { get; }

    /// <summary>
    /// Gets the GraphQL Document as readable string.
    /// </summary>
    public string BodyString { get; }

    /// <summary>
    /// Gets the document hash algorithm.
    /// </summary>
    public string HashAlgorithm { get; }

    /// <summary>
    /// Gets the document hash value.
    /// </summary>
    public string HashValue { get; }

    /// <summary>
    /// Defines if the operation has any file uploads
    /// </summary>
    public bool HasUpload { get; }

    /// <summary>
    /// The arguments the operation takes.
    /// </summary>
    public IReadOnlyList<PropertyDescriptor> Arguments { get; }

    /// <summary>
    /// The request strategy.
    /// </summary>
    public RequestStrategy Strategy { get; }

    /// <summary>
    /// The interface of this operation
    /// </summary>
    public RuntimeTypeInfo InterfaceType { get; }
}
