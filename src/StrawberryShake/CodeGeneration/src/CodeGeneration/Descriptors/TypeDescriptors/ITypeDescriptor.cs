namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

public interface ITypeDescriptor : ICodeDescriptor
{
    /// <summary>
    /// Gets the type kind.
    /// </summary>
    TypeKind Kind { get; }
}
