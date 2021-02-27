namespace StrawberryShake.CodeGeneration
{
    public interface ILeafTypeDescriptor : INamedTypeDescriptor
    {
        /// <summary>
        /// Gets the .NET serialization type.
        /// (the way we transport a leaf value.)
        /// </summary>
        RuntimeTypeInfo SerializationType { get; }
    }
}
