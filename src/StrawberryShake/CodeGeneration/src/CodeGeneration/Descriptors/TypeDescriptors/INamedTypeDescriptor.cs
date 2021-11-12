namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

public interface INamedTypeDescriptor : ITypeDescriptor
{
    /// <summary>
    /// Gets the .NET runtime type of the GraphQL type.
    /// </summary>
    RuntimeTypeInfo RuntimeType { get; }
}
