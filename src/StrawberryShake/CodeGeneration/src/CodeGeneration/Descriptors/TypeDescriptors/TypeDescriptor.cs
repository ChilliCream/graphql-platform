using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a type, which may be a concrete class or an interface.
    /// </summary>
    public class TypeDescriptor : ITypeDescriptor
    {
        public TypeDescriptor(
            string name,
            string @namespace,
            IReadOnlyList<NameString>? implements = null,
            IReadOnlyList<TypeMemberDescriptor>? properties = null,
            IReadOnlyList<TypeDescriptor>? isImplementedBy = null,
            TypeKind kind = TypeKind.Scalar,
            bool isNullable = false,
            string? graphQLTypeName = null)
        {
            Name = name;
            Namespace = @namespace;
            GraphQLTypeName = graphQLTypeName;
            Implements = implements ?? new List<NameString>();
            Properties = properties ?? new List<TypeMemberDescriptor>();
            IsImplementedBy = isImplementedBy ?? new TypeDescriptor[] { };
            Kind = kind;
            IsNullable = isNullable;
        }

        /// <summary>
        /// Gets the .NET type name.
        /// </summary>
        public NameString Name { get; }

        public NameString? GraphQLTypeName { get; }

        /// <summary>
        /// Describes whether or not it is a nullable type reference
        /// </summary>
        public bool IsNullable { get; }

        /// <summary>
        /// The properties that result from the requested fields of the operation this ResultType is generated for.
        /// </summary>
        public IReadOnlyList<TypeMemberDescriptor> Properties { get; private set; }

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
        public bool IsInterface => IsImplementedBy.Count > 0;

        /// <summary>
        /// A list of types that implement this interface
        /// This list must only contain the most specific, concrete classes (that implement this interface),
        /// but no other interfaces.
        /// </summary>
        public IReadOnlyList<TypeDescriptor> IsImplementedBy { get; }

        /// <summary>
        /// What is the Kind of the described Type?
        /// </summary>
        public TypeKind Kind { get; }

        public bool IsScalarType => Kind == TypeKind.Scalar;

        public bool IsEntityType => Kind == TypeKind.EntityType;

        public void Complete(IReadOnlyList<TypeMemberDescriptor> properties)
        {
            Properties = properties;
        }
    }
}
