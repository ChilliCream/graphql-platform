using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class InputModelSerializerDescriptor
        : ICodeDescriptor
    {
        public InputModelSerializerDescriptor(
            string name,
            string @namespace,
            string inputGraphQLTypeName,
            string inputTypeName,
            IReadOnlyList<InputFieldSerializerDescriptor> fieldSerializers,
            IReadOnlyList<ValueSerializerDescriptor> valueSerializers,
            IReadOnlyList<InputTypeSerializerMethodDescriptor> typeSerializerMethods)
        {
            Name = name;
            Namespace = @namespace;
            InputGraphQLTypeName = inputGraphQLTypeName;
            InputTypeName = inputTypeName;
            FieldSerializers = fieldSerializers;
            ValueSerializers = valueSerializers;
            TypeSerializerMethods = typeSerializerMethods;
        }

        /// <summary>
        /// The name of the input model serializer.
        /// </summary>
        /// <value></value>
        public string Name { get; }

        public string Namespace { get; }

        /// <summary>
        /// Gets the GraphQL type name of the input object type.
        /// </summary>
        /// <value></value>
        public string InputGraphQLTypeName { get; }

        /// <summary>
        /// Gets the .NET type name of the input object type.
        /// </summary>
        public string InputTypeName { get; }

        public IReadOnlyList<InputFieldSerializerDescriptor> FieldSerializers { get; }

        /// <summary>
        /// Gets the serializers that the input model serializer needs to
        /// serialize the input model fields.
        /// </summary>
        /// <value></value>
        public IReadOnlyList<ValueSerializerDescriptor> ValueSerializers { get; }

        /// <summary>
        /// Gets the serializer methods that handle the various input type serialization cases of this particular input model.
        /// </summary>
        /// <value></value>
        public IReadOnlyList<InputTypeSerializerMethodDescriptor> TypeSerializerMethods { get; }
    }
}
