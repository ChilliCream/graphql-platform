namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

public interface ITypeDescriptor : ICodeDescriptor
{
    /// <summary>
    /// Gets the type kind.
    /// </summary>
    public TypeKind Kind { get; }
}
