using System;
using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class InputObjectTypeDescriptor : INamedTypeDescriptor
    {
        public InputObjectTypeDescriptor(
            NameString name,
            RuntimeTypeInfo runtimeType)
        {
            Name = name;
            RuntimeType = runtimeType;
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
        /// The properties that result from the requested fields of the operation this ResultType is
        /// generated for.
        /// </summary>
        public IReadOnlyList<PropertyDescriptor> Properties { get; private set; } =
            Array.Empty<PropertyDescriptor>();

        public void CompleteProperties(IReadOnlyList<PropertyDescriptor> properties)
        {
            if (Properties is not null)
            {
                throw new InvalidOperationException("Properties are already completed.");
            }

            Properties = properties;
        }
    }
}
