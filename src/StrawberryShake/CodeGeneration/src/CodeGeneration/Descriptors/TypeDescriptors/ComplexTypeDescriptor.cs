namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

public abstract class ComplexTypeDescriptor : INamedTypeDescriptor
{
    protected ComplexTypeDescriptor(
        string name,
        TypeKind typeKind,
        RuntimeTypeInfo runtimeType,
        IReadOnlyList<string> implements,
        IReadOnlyList<DeferredFragmentDescriptor>? deferred,
        string? description,
        RuntimeTypeInfo? parentRuntimeType = null)
    {
        Name = name;
        Kind = typeKind;
        RuntimeType = runtimeType;
        Implements = implements;
        Deferred = deferred ?? Array.Empty<DeferredFragmentDescriptor>();
        Description = description;
        ParentRuntimeType = parentRuntimeType;
    }

    /// <summary>
    /// Gets the GraphQL type name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type kind.
    /// </summary>
    public TypeKind Kind { get; }

    /// <summary>
    /// Gets the .NET runtime type of the GraphQL type.
    /// </summary>
    public RuntimeTypeInfo RuntimeType { get; }

    /// <summary>
    /// The documentation of this type
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// The properties that result from the requested fields of the operation this ResultType is
    /// generated for.
    /// </summary>
    public IReadOnlyList<PropertyDescriptor> Properties { get; private set; } =
        Array.Empty<PropertyDescriptor>();

    /// <summary>
    /// A list of interface names the type implements
    /// </summary>
    public IReadOnlyList<string> Implements { get; }

    /// <summary>
    /// Gets the deferred fragments of this type.
    /// </summary>
    public IReadOnlyList<DeferredFragmentDescriptor> Deferred { get; }

    /// <summary>
    /// Gets the .NET runtime type of the parent. If there is no parent type, this property is
    /// null
    /// </summary>
    public RuntimeTypeInfo? ParentRuntimeType { get; }

    public void CompleteProperties(IReadOnlyList<PropertyDescriptor> properties)
    {
        if (Properties.Count > 0)
        {
            throw new InvalidOperationException("Properties are already completed.");
        }

        Properties = properties;
    }
}
