using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a deserialization method for leaf types in a result parser.
    /// </summary>
    public class ResultParserDeserializerMethodDescriptor
        : ICodeDescriptor
    {
        public ResultParserDeserializerMethodDescriptor(
            string name,
            string serializationType,
            string runtimeType,
            IReadOnlyList<TypeClassPropertyDescriptor> runtimeTypeComponents,
            ValueSerializerDescriptor serializer)
        {
            Name = name;
            SerializationType = serializationType;
            RuntimeType = runtimeType;
            RuntimeTypeComponents = runtimeTypeComponents;
            Serializer = serializer;
        }

        /// <summary>
        /// Gets the name of the deserialization method.
        /// </summary>
        public string Name { get; }

        public string SerializationType { get; }

        /// <summary>
        /// Gets the .NET type used by the client runtime.
        /// </summary>
        public string RuntimeType { get; }

        /// <summary>
        /// The type components describing the type structure of the type
        /// deserialized by this deserializer.
        /// </summary>
        public IReadOnlyList<TypeClassPropertyDescriptor> RuntimeTypeComponents { get; }

        /// <summary>
        /// The serializer that can deserialize the leaf type
        /// handled by this deserialization method.
        /// </summary>
        public ValueSerializerDescriptor Serializer { get; }
    }
}
