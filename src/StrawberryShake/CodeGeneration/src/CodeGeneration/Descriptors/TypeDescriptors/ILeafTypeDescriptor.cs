namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

public interface ILeafTypeDescriptor : INamedTypeDescriptor
{
    /// <summary>
    /// Gets the .NET serialization type.
    /// (the way we transport a leaf value.)
    /// </summary>
    RuntimeTypeInfo SerializationType { get; }
}
