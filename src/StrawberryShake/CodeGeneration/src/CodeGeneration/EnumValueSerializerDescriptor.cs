using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class EnumValueSerializerDescriptor
        : ICodeDescriptor
    {
        public EnumValueSerializerDescriptor(
            string name,
            string @namespace,
            string enumGraphQLTypeName,
            string enumTypeName,
            IReadOnlyList<EnumElementDescriptor> elements)
        {
            Name = name;
            Namespace = @namespace;
            EnumGraphQLTypeName = enumGraphQLTypeName;
            EnumTypeName = enumTypeName;
            Elements = elements;
        }

        /// <summary>
        /// Gets the name of the serializer class.
        /// </summary>
        public string Name { get; }

        public string Namespace { get; }

        /// <summary>
        /// Gets the GraphQL type name of the enum type.
        /// </summary>
        /// <value></value>
        public string EnumGraphQLTypeName { get; }

        /// <summary>
        /// Gets the .NET type name of the enum type.
        /// </summary>
        public string EnumTypeName { get; }

        /// <summary>
        /// Gets the enum type elements.
        /// </summary>
        public IReadOnlyList<EnumElementDescriptor> Elements { get; }
    }
}
