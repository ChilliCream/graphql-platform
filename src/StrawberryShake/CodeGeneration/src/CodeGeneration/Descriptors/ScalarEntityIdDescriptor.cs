namespace StrawberryShake.CodeGeneration.Descriptors
{
    /// <summary>
    /// Represents the entity for which the ID shall be generated or an id field of that entity.
    /// </summary>
    public class ScalarEntityIdDescriptor
    {
        /// <summary>
        /// Creates a new instance of <see cref="ScalarEntityIdDescriptor"/>.
        /// </summary>
        /// <param name="name">
        /// The name of the field or entity.
        /// </param>
        /// <param name="typeName">
        /// The serialization type name of the entity id field, eg. String.
        /// </param>
        /// <param name="serializationType">
        /// The .NET serialization type.
        /// </param>
        public ScalarEntityIdDescriptor(
            string name,
            string typeName,
            RuntimeTypeInfo serializationType)
        {
            Name = name;
            TypeName = typeName;
            SerializationType = serializationType;
        }

        /// <summary>
        /// Gets the name of the field or entity.
        /// </summary>
        /// <value></value>
        public string Name { get; }

        /// <summary>
        /// Gets the GraphQL type name of the entity id field.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Gets the .NET serialization type.
        /// (the way we transport a leaf value.)
        /// </summary>
        public RuntimeTypeInfo SerializationType { get; }
    }
}
