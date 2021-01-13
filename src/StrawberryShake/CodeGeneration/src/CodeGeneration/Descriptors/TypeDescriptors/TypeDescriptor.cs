using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a type, which may be a concrete class or an interface.
    /// </summary>
    public class TypeDescriptor: ITypeDescriptor
    {
        /// <summary>
        /// Gets the .NET type name.
        /// </summary>
        public string Name { get; }

        public string? GraphQlTypeName { get; }

        /// <summary>
        /// Describes whether or not it is a nullable type reference
        /// </summary>
        public bool IsNullable { get; }

        /// <summary>
        /// The properties that result from the requested fields of the operation this ResultType is generated for.
        /// </summary>
        public IReadOnlyList<NamedTypeReferenceDescriptor> Properties { get; }

        /// <summary>
        /// A list of interface names the ResultType implements
        /// </summary>
        public IReadOnlyList<string> Implements { get; }

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

        public TypeDescriptor(
            string name,
            string @namespace,
            IReadOnlyList<string>? implements = null,
            IReadOnlyList<NamedTypeReferenceDescriptor>? properties = null,
            IReadOnlyList<TypeDescriptor>? isImplementedBy = null,
            TypeKind kind = TypeKind.Scalar,
            bool isNullable = false,
            string? graphQlTypeName  = null)
        {
            Name = name;
            Namespace = @namespace;
            GraphQlTypeName = graphQlTypeName;
            Implements = implements ?? new List<string>();
            Properties = properties ?? new List<NamedTypeReferenceDescriptor>();
            IsImplementedBy = isImplementedBy ?? new TypeDescriptor[] { };
            Kind = kind;
            IsNullable = isNullable;
        }
    }
}
