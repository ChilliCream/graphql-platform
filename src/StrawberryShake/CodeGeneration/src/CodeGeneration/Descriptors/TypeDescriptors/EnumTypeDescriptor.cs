using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors
{
    public class EnumTypeDescriptor : ILeafTypeDescriptor
    {
        public EnumTypeDescriptor(
            NameString name,
            RuntimeTypeInfo runtimeType,
            RuntimeTypeInfo? underlyingType,
            IReadOnlyList<EnumValueDescriptor> values,
            string? documentation)
        {
            Name = name;
            RuntimeType = runtimeType;
            SerializationType = new RuntimeTypeInfo(TypeNames.String);
            UnderlyingType = underlyingType;
            Values = values;
        }

        /// <summary>
        /// Gets the GraphQL type name.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the type kind.
        /// </summary>
        public TypeKind Kind => TypeKind.Leaf;

        /// <summary>
        /// Gets the .NET runtime type of the GraphQL type.
        /// </summary>
        public RuntimeTypeInfo RuntimeType { get; }

        /// <summary>
        /// Gets the .NET serialization type.
        /// (the way we transport a leaf value.)
        /// </summary>
        public RuntimeTypeInfo SerializationType { get; }

        /// <summary>
        /// Gets the underlying enum type.
        /// </summary>
        public RuntimeTypeInfo? UnderlyingType { get; }

        /// <summary>
        /// Gets the enum values.
        /// </summary>
        public IReadOnlyList<EnumValueDescriptor> Values { get; }

        /// <summary>
        /// The documentation of this enum
        /// </summary>
        public string? Documentation { get; }
    }
}
