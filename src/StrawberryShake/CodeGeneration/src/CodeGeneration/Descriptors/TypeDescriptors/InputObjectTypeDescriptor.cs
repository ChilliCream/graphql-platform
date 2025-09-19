namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

public sealed class InputObjectTypeDescriptor : INamedTypeDescriptor
{
    public InputObjectTypeDescriptor(
        string name,
        RuntimeTypeInfo runtimeType,
        bool hasUpload,
        string? documentation)
    {
        Name = name;
        RuntimeType = runtimeType;
        HasUpload = hasUpload;
        Documentation = documentation;
    }

    /// <summary>
    /// Gets the GraphQL type name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type kind.
    /// </summary>
    public TypeKind Kind => TypeKind.Input;

    /// <summary>
    /// Gets the .NET runtime type of the GraphQL type.
    /// </summary>
    public RuntimeTypeInfo RuntimeType { get; }

    /// <summary>
    /// The documentation of this type
    /// </summary>
    public string? Documentation { get; }

    /// <summary>
    /// Defines if the input object or one of its related types has any file uploads
    /// </summary>
    public bool HasUpload { get; }

    /// <summary>
    /// The properties that result from the requested fields of the operation this ResultType is
    /// generated for.
    /// </summary>
    public IReadOnlyList<PropertyDescriptor> Properties { get; private set; } =
        Array.Empty<PropertyDescriptor>();

    public void CompleteProperties(IReadOnlyList<PropertyDescriptor> properties)
    {
        if (Properties.Count > 0)
        {
            throw new InvalidOperationException("Properties are already completed.");
        }

        Properties = properties;
    }
}
