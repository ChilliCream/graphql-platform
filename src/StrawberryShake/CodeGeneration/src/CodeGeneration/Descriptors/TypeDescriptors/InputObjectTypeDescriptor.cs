using System;
using System.Collections.Generic;
using HotChocolate;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors
{
    public class InputObjectTypeDescriptor : INamedTypeDescriptor
    {
        public InputObjectTypeDescriptor(
            NameString name,
            RuntimeTypeInfo runtimeType,
            string? documentation)
        {
            Name = NameUtils.GetClassName(name);
            RuntimeType = runtimeType;
            Documentation = documentation;
        }

        /// <summary>
        /// Gets the GraphQL type name.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the type kind.
        /// </summary>
        public TypeKind Kind => TypeKind.InputType;

        /// <summary>
        /// Gets the .NET runtime type of the GraphQL type.
        /// </summary>
        public RuntimeTypeInfo RuntimeType { get; }

        /// <summary>
        /// The documentation of this type
        /// </summary>
        public string? Documentation { get; }

        /// <summary>
        /// The properties that result from the requested fields of the operation this ResultType is
        /// generated for.
        /// </summary>
        public IReadOnlyList<PropertyDescriptor> Properties { get; private set; } =
            Array.Empty<PropertyDescriptor>();

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
