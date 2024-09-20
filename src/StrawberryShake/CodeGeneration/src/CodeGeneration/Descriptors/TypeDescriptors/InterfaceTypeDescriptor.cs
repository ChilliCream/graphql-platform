namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

public sealed class InterfaceTypeDescriptor : ComplexTypeDescriptor
{
    public InterfaceTypeDescriptor(
        string name,
        TypeKind typeKind,
        RuntimeTypeInfo runtimeType,
        IReadOnlyCollection<ObjectTypeDescriptor> implementedBy,
        IReadOnlyList<string> implements,
        IReadOnlyList<DeferredFragmentDescriptor>? deferred,
        string? description,
        RuntimeTypeInfo? parentRuntimeType = null)
        : base(
            name,
            typeKind,
            runtimeType,
            implements,
            deferred,
            description,
            parentRuntimeType)
    {
        ImplementedBy = implementedBy;
    }

    /// <summary>
    /// A list of types that implement this interface
    /// This list must only contain the most specific, concrete classes (that implement this
    /// interface), but no other interfaces.
    /// </summary>
    public IReadOnlyCollection<ObjectTypeDescriptor> ImplementedBy { get; }
}
