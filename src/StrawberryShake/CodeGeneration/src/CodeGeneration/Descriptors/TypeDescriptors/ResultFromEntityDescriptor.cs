namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

public sealed class ResultFromEntityDescriptor : ComplexTypeDescriptor
{
    public ResultFromEntityDescriptor(
        string name,
        RuntimeTypeInfo runtimeType,
        IReadOnlyList<string> implements,
        IReadOnlyList<DeferredFragmentDescriptor>? deferred,
        string? description)
        : base(name, TypeKind.Entity, runtimeType, implements, deferred, description)
    {
    }
}
