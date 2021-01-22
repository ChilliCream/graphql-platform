using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Types;

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
            IReadOnlyList<NameString>? implements = null,
            IReadOnlyList<PropertyDescriptor>? properties = null,
            IReadOnlyList<NamedTypeDescriptor>? implementedBy = null,
            TypeKind kind = TypeKind.LeafType,
            NameString? graphQLTypeName = null,
            string? serializationType = null)
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

        public string? SerializationType { get; }

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
