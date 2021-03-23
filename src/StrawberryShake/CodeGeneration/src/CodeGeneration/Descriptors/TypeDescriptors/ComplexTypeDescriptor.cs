using System;
using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors
{
    public abstract class ComplexTypeDescriptor : INamedTypeDescriptor
    {
        protected ComplexTypeDescriptor(
            NameString name,
            TypeKind typeKind,
            RuntimeTypeInfo runtimeType,
            IReadOnlyList<NameString> implements,
            string? description,
            RuntimeTypeInfo? parentRuntimeType = null)
        {
            Name = name;
            Kind = typeKind;
            RuntimeType = runtimeType;
            Implements = implements;
            Description = description;
            ParentRuntimeType = parentRuntimeType;
        }

        /// <summary>
        /// Gets the GraphQL type name.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the type kind.
        /// </summary>
        public TypeKind Kind { get; }

        /// <summary>
        /// Gets the .NET runtime type of the GraphQL type.
        /// </summary>
        public RuntimeTypeInfo RuntimeType { get; }

        /// <summary>
        /// The documentation of this type
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// The properties that result from the requested fields of the operation this ResultType is
        /// generated for.
        /// </summary>
        public IReadOnlyList<PropertyDescriptor> Properties { get; private set; } =
            Array.Empty<PropertyDescriptor>();

        /// <summary>
        /// A list of interface names the type implements
        /// </summary>
        public IReadOnlyList<NameString> Implements { get; }

        /// <summary>
        /// Gets the .NET runtime type of the parent. If there is no parent type, this property is
        /// null
        /// </summary>
        public RuntimeTypeInfo? ParentRuntimeType { get; }

        public void CompleteProperties(IReadOnlyList<PropertyDescriptor> properties)
        {
            if (Properties.Count > 0)
            {
                throw new InvalidOperationException("Properties are already completed.");
            }

            Properties = properties;
        }
    }
}
