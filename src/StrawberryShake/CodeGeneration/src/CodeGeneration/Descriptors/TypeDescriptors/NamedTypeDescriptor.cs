using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{


    /// <summary>
    /// Describes a type, which may be a concrete class or an interface.
    /// </summary>
    public class NamedTypeDescriptor : ITypeDescriptor
    {
        public NamedTypeDescriptor(
            NameString name,
            string @namespace,
            bool isInterface,
            IReadOnlyList<NameString>? implements,
            IReadOnlyList<PropertyDescriptor>? properties,
            IReadOnlyList<NamedTypeDescriptor>? implementedBy,
            TypeKind kind,
            NameString? graphQLTypeName,
            string? serializationType,
            bool isEnum,
            string? complexDataTypeParent)
        {
            Name = name;
            Namespace = @namespace;
            GraphQLTypeName = graphQLTypeName;
            Implements = implements ?? new List<NameString>();
            Properties = properties ?? new List<PropertyDescriptor>();
            ImplementedBy = implementedBy ?? new NamedTypeDescriptor[] { };
            Kind = kind;
            IsInterface = isInterface;
            SerializationType = serializationType;
            IsEnum = isEnum;
            ComplexDataTypeParent = complexDataTypeParent;
        }

        /// <summary>
        /// Gets the .NET type name.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// What is the Kind of the described Type?
        /// </summary>
        public TypeKind Kind { get; }

        /// <summary>
        /// Gets the GraphQL type name.
        /// </summary>
        public NameString? GraphQLTypeName { get; }

        public NameString? ComplexDataTypeParent { get; }

        /// <summary>
        /// The runtime type including namespace of the GraphQL scalar type
        /// </summary>
        public string? SerializationType { get; }

        public bool IsEnum { get; }

        /// <summary>
        /// The properties that result from the requested fields of the operation this ResultType is generated for.
        /// </summary>
        public IReadOnlyList<PropertyDescriptor> Properties { get; private set; }

        /// <summary>
        /// A list of interface names the ResultType implements
        /// </summary>
        public IReadOnlyList<NameString> Implements { get; }

        /// <summary>
        /// The name of the namespace the generated type shall reside in
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// States whether or not this type is an interface
        /// </summary>
        public bool IsInterface { get; }

        /// <summary>
        /// A list of types that implement this interface
        /// This list must only contain the most specific, concrete classes (that implement this interface),
        /// but no other interfaces.
        /// </summary>
        public IReadOnlyList<NamedTypeDescriptor> ImplementedBy { get; }

        public void Complete(IReadOnlyList<PropertyDescriptor> properties)
        {
            Properties = properties;
        }
    }
}
